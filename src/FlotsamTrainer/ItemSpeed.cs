using System;
using System.Collections.Generic;

namespace FlotsamTrainer
{
    /// <summary>
    /// 指定物品的额外加速。
    ///
    /// 原版单件打捞的耗时 = 打捞动画长度 × 动画循环次数 / 动画播放速度，
    /// 与物品本身无关（金属废料慢是因为它一件一件来、件数多）。
    /// 所以"只给金属废料加速"的做法是：
    ///   1. 在 ItemToHaul.TransferItemCoroutine 开始时记录这次搬运的是哪个物品
    ///   2. 该物品命中关键字时，把它这一次的 Animator "Speed Multiplier" 额外放大
    /// 只影响命中物品，其它物品照旧。
    /// </summary>
    internal static class ItemSpeed
    {
        internal static bool Enabled;
        internal static string Keyword = "金属废料";
        internal static float Multiplier = 3f;
        internal static string LastMatched = "-";

        private const float MaxMultiplier = 8f;

        private static readonly Dictionary<Agent, bool> _pending = new Dictionary<Agent, bool>();
        private static readonly Dictionary<ItemProperties, bool> _matchCache = new Dictionary<ItemProperties, bool>();

        /// <summary>搬运协程开始时调用，记下这次是否是目标物品。</summary>
        internal static void Mark(Agent agent, ItemProperties properties)
        {
            if (agent == null) return;

            bool target = false;
            if (Enabled && properties != null)
            {
                target = IsTarget(properties);
            }

            _pending[agent] = target;
        }

        /// <summary>取走并清除本次标记；返回是否要给这次动画额外加速。</summary>
        internal static bool Consume(Agent agent)
        {
            if (agent == null) return false;

            bool target;
            if (!_pending.TryGetValue(agent, out target)) return false;

            _pending.Remove(agent);
            return target;
        }

        internal static bool IsTarget(ItemProperties p)
        {
            if (p == null) return false;

            bool cached;
            if (_matchCache.TryGetValue(p, out cached)) return cached;

            bool hit = false;
            string keyword = Keyword;

            if (!string.IsNullOrEmpty(keyword))
            {
                try
                {
                    string localized = p.LocalizedName;
                    if (!string.IsNullOrEmpty(localized) &&
                        localized.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        hit = true;
                        LastMatched = localized;
                    }

                    if (!hit)
                    {
                        string asset = p.name;
                        if (!string.IsNullOrEmpty(asset) &&
                            asset.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            hit = true;
                            LastMatched = localized ?? asset;
                        }
                    }
                }
                catch { /* 忽略 */ }
            }

            _matchCache[p] = hit;
            return hit;
        }

        internal static void ResetCache()
        {
            _matchCache.Clear();
            _pending.Clear();
        }

        internal static float ClampMultiplier(float value)
        {
            if (value < 1f) return 1f;
            if (value > MaxMultiplier) return MaxMultiplier;
            return value;
        }
    }
}
