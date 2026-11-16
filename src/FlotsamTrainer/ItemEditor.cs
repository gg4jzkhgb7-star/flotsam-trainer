using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlotsamTrainer
{
    /// <summary>
    /// 材料编辑器。所有物品定义来自 GameSettings.Instance.ItemSettings.ItemProperties（全量注册表），
    /// 数量统计与增减都走社区仓库（Community.Storages）的原生接口。
    ///
    /// 关键实现细节（均经 IL 反汇编核实）：
    ///   - 移除必须用 Inventory.RemoveItem → SubInventory.TakeItem（真删除）
    ///   - Storage.ReturnItem 只是 PeekItem（只读不删），不能用来删
    ///   - 新增用 Inventory.AddItem（受容量限制），失败时退回 SubInventory.AddItem(item, true) 无视容量
    /// </summary>
    internal static class ItemEditor
    {
        private static ItemProperties[] _all;
        private static bool _loaded;
        private static readonly Dictionary<ItemProperties, int> _countCache = new Dictionary<ItemProperties, int>();
        private static float _nextCacheRefresh;

        internal static ItemProperties[] All
        {
            get
            {
                EnsureLoaded();
                return _all;
            }
        }

        private static float _nextLoadTry;

        private static void EnsureLoaded()
        {
            if (_loaded) return;

            // 主菜单阶段 GameSettings 可能还没就绪，1 秒后重试，避免永久空表
            if (Time.unscaledTime < _nextLoadTry) return;
            _nextLoadTry = Time.unscaledTime + 1f;

            try
            {
                object settings = null;

                // GameSettings.Instance.ItemSettings.ItemProperties
                var gsType = typeof(GameSpeedManager).Assembly.GetType("GameSettings");
                var instanceProp = gsType?.GetProperty("Instance",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                settings = instanceProp?.GetValue(null, null);

                if (settings == null)
                {
                    var gmType = typeof(GameSpeedManager).Assembly.GetType("GameManager");
                    var s = gmType?.GetProperty("Settings",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    settings = s?.GetValue(null, null);
                }

                if (settings == null)
                {
                    // 还没进存档，下一轮再试
                    _all = new ItemProperties[0];
                    return;
                }

                var itemSettingsField = settings.GetType().GetField("ItemSettings");
                var itemSettings = itemSettingsField?.GetValue(settings);
                var arrField = itemSettings?.GetType().GetField("ItemProperties");
                var arr = arrField?.GetValue(itemSettings) as ItemProperties[];

                if (arr == null || arr.Length == 0)
                {
                    _all = new ItemProperties[0];
                    return;
                }

                _all = arr;
                _loaded = true;
                TrainerPlugin.Log?.LogInfo($"材料注册表已加载：{_all.Length} 项");
            }
            catch (Exception e)
            {
                TrainerPlugin.Log?.LogError("加载材料注册表失败: " + e.Message);
                _all = new ItemProperties[0];
            }
        }

        internal static string NameOf(ItemProperties p)
        {
            try
            {
                var name = p.LocalizedName;
                if (!string.IsNullOrEmpty(name)) return name;
            }
            catch { /* 本地化未就绪 */ }

            try { return p.name; } catch { return "?"; }
        }

        private static List<Storage> Storages()
        {
            try
            {
                var community = GameRefs.PlayerCommunity;
                if (community?.Storages == null) return null;
                return community.Storages;
            }
            catch { return null; }
        }

        internal static int CountOf(ItemProperties p, bool useCache = true)
        {
            if (useCache && Time.unscaledTime < _nextCacheRefresh && _countCache.TryGetValue(p, out var cached))
            {
                return cached;
            }

            int total = 0;
            try
            {
                var storages = Storages();
                if (storages != null)
                {
                    foreach (var s in storages)
                    {
                        if (s == null) continue;
                        total += s.ReturnStoredItemCount(p);
                    }
                }
            }
            catch { total = 0; }

            _countCache[p] = total;
            if (useCache) _nextCacheRefresh = Time.unscaledTime + 1f;
            return total;
        }

        /// <summary>
        /// 把某材料的总数设置为 target。返回是否成功，msg 给出说明。
        /// </summary>
        internal static bool SetCount(ItemProperties p, int target, out string msg)
        {
            msg = "";
            if (p == null) { msg = "物品为空"; return false; }
            if (target < 0) target = 0;
            if (target > 9999) target = 9999;

            var storages = Storages();
            if (storages == null || storages.Count == 0)
            {
                msg = "没找到社区仓库（要先进入存档）";
                return false;
            }

            int current = CountOf(p, false);
            int delta = target - current;
            if (delta == 0) { msg = "数量已一致"; return true; }

            // 单次最多处理 2000 个，避免一次点太多导致卡顿；分几次点即可
            const int MaxPerClick = 2000;
            if (delta > MaxPerClick) delta = MaxPerClick;
            if (delta < -MaxPerClick) delta = -MaxPerClick;

            int done = 0;

            if (delta > 0)
            {
                for (int i = 0; i < delta; i++)
                {
                    bool added = false;

                    // 先按容量正常放入，能均衡分散到多个仓库
                    foreach (var s in storages)
                    {
                        if (s == null || s.Inventory == null) continue;
                        try
                        {
                            if (s.Inventory.AddItem(new Item(p), SubInventoryType.Storage))
                            {
                                added = true;
                                break;
                            }
                        }
                        catch { /* 换下一个仓库 */ }
                    }

                    // 所有仓库都满了：退回无视容量写入，避免"点了没反应"
                    if (!added)
                    {
                        foreach (var s in storages)
                        {
                            if (s == null || s.Inventory == null) continue;
                            try
                            {
                                var sub = s.Inventory.ReturnInventory(SubInventoryType.Storage);
                                if (sub != null)
                                {
                                    sub.AddItem(new Item(p), true);
                                    added = true;
                                    break;
                                }
                            }
                            catch { /* 换下一个仓库 */ }
                        }
                    }

                    if (!added) break;
                    done++;
                }
            }
            else
            {
                for (int i = 0; i < -delta; i++)
                {
                    bool removed = false;
                    foreach (var s in storages)
                    {
                        if (s == null || s.Inventory == null) continue;
                        try
                        {
                            if (s.ReturnStoredItemCount(p) <= 0) continue;
                            var taken = s.Inventory.RemoveItem(p, SubInventoryType.Storage);
                            if (taken != null)
                            {
                                removed = true;
                                break;
                            }
                        }
                        catch { /* 换下一个仓库 */ }
                    }

                    if (!removed) break;
                    done++;
                }
            }

            try
            {
                foreach (var s in storages)
                {
                    s?.Inventory?.OnInventoryUpdated();
                }
            }
            catch { /* 忽略 */ }

            _nextCacheRefresh = 0f;
            CountOf(p, false);
            _nextCacheRefresh = 0f;

            int now = CountOf(p, false);

            if (done != Math.Abs(delta))
            {
                msg = $"部分成功：{current} → {now}（目标 {target}）";
                return false;
            }

            msg = $"{current} → {now}";
            return true;
        }
    }
}
