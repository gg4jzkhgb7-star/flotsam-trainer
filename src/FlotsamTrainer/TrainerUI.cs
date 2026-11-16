using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlotsamTrainer
{
    internal static class TrainerUI
    {
        internal static bool Visible;

        private const int WindowId = 0x464C5453; // "FLTS"
        private static Rect _window = new Rect(24f, 24f, 520f, 620f);
        private static Font _font;
        private static Texture2D _bg;

        private static string _search = "";
        private static string _lastResult = "";
        private static float _lastResultTime;
        private static Vector2 _materialScroll;
        private static Vector2 _mainScroll;
        private static readonly Dictionary<ItemProperties, string> _inputBuffers = new Dictionary<ItemProperties, string>();
        private static ItemProperties[] _filtered;
        private static string _filteredFor;

        internal static void TickHotkeys()
        {
            try
            {
                if (Input.GetKeyDown(KeyCode.F2))
                {
                    Trainer.SpeedMultiplier = Mathf.Max(Trainer.MinMultiplier, Trainer.SpeedMultiplier - 0.5f);
                }
                if (Input.GetKeyDown(KeyCode.F3))
                {
                    Trainer.SpeedMultiplier = Mathf.Min(Trainer.MaxMultiplier, Trainer.SpeedMultiplier + 0.5f);
                }
                if (Input.GetKeyDown(KeyCode.F4))
                {
                    Trainer.SpeedEnabled = !Trainer.SpeedEnabled;
                    if (!Trainer.SpeedEnabled) RestoreSpeed();
                }
            }
            catch { /* 输入系统异常时忽略 */ }
        }

        internal static void RestoreSpeed()
        {
            try
            {
                if (Patches.SpeedPatch.Captured && Patches.SpeedPatch.BaseScale > 0.0001f)
                {
                    Time.timeScale = Patches.SpeedPatch.BaseScale;
                }
            }
            catch { /* 忽略 */ }
        }

        private static void EnsureStyle()
        {
            if (_font == null)
            {
                try
                {
                    _font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 15);
                }
                catch { /* 用默认字体 */ }
            }

            if (_bg == null)
            {
                _bg = new Texture2D(1, 1);
                _bg.SetPixel(0, 0, new Color(0.06f, 0.08f, 0.10f, 0.94f));
                _bg.Apply();
            }
        }

        internal static void Draw()
        {
            EnsureStyle();

            float scale = Mathf.Max(0.85f, Screen.height / 1080f);
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));

            if (_font != null) GUI.skin.font = _font;
            GUI.backgroundColor = new Color(0.15f, 0.55f, 0.65f);

            _window = GUILayout.Window(WindowId, _window, DrawWindow, " Flotsam 修改器 v0.6.0 ");

            GUI.matrix = oldMatrix;
            GUI.backgroundColor = Color.white;
        }

        private static void DrawWindow(int id)
        {
            GUI.DrawTexture(new Rect(0, 0, _window.width, _window.height), _bg);

            _mainScroll = GUILayout.BeginScrollView(_mainScroll);

            GUILayout.Space(4);
            GUILayout.Label($"<b>社区</b>：{Trainer.CommunityName}    人口：{Trainer.AgentCount}    仓库：{Trainer.StorageCount}");

            GUILayout.Space(6);
            GUILayout.Label("<b>游戏速度</b>");
            bool speed = GUILayout.Toggle(Trainer.SpeedEnabled, " 启用速度倍率（F4 开关，F2/F3 调整）");
            if (speed != Trainer.SpeedEnabled)
            {
                Trainer.SpeedEnabled = speed;
                if (!speed) RestoreSpeed();
            }
            GUILayout.Label($"倍率：x{Trainer.SpeedMultiplier:0.0}");
            Trainer.SpeedMultiplier = GUILayout.HorizontalSlider(Trainer.SpeedMultiplier, Trainer.MinMultiplier, Trainer.MaxMultiplier);

            GUILayout.Space(6);
            GUILayout.Label("<b>打捞</b>");
            Trainer.WorkSpeedEnabled = GUILayout.Toggle(Trainer.WorkSpeedEnabled, " 打捞 / 搬运动作加速");
            GUILayout.Label($"动作倍率：x{Trainer.WorkSpeedMultiplier:0.0}");
            Trainer.WorkSpeedMultiplier = GUILayout.HorizontalSlider(Trainer.WorkSpeedMultiplier, 1f, Trainer.MaxWorkSpeed);
            Trainer.HaulSplit = GUILayout.Toggle(Trainer.HaulSplit, " 启用打捞配额（关闭 = 完全原版）");

            if (Trainer.HaulSplit)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("配额：", GUILayout.Width(44));
                int mode = GUILayout.Toolbar((int)Trainer.HaulMode, new[] { "智能", "满载", "均分", "固定" });
                Trainer.HaulMode = (HaulMode)mode;
                GUILayout.EndHorizontal();

                if (Trainer.HaulMode == HaulMode.FixedPerAgent)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("每人件数：", GUILayout.Width(70));
                    string countText = GUILayout.TextField(Trainer.FixedPerAgentCount.ToString(), GUILayout.Width(60));
                    int per;
                    if (int.TryParse(countText, out per) && per > 0) Trainer.FixedPerAgentCount = Mathf.Min(per, 99);
                    GUILayout.EndHorizontal();
                }

                GUILayout.Label("<size=11>智能：物品少于一船→多船并行；多于一船→按船满载分批</size>");
            }

            Trainer.NearestBoat = GUILayout.Toggle(Trainer.NearestBoat, " 优先派离目标最近的船");

            GUILayout.Space(4);
            GUILayout.Label("<b>指定物品额外加速</b>");
            ItemSpeed.Enabled = GUILayout.Toggle(ItemSpeed.Enabled, " 只加速指定物品（如金属废料）");

            GUILayout.BeginHorizontal();
            GUILayout.Label("物品关键字：", GUILayout.Width(76));
            string keyword = GUILayout.TextField(ItemSpeed.Keyword ?? "", GUILayout.Width(150));
            if (keyword != ItemSpeed.Keyword)
            {
                ItemSpeed.Keyword = keyword;
                ItemSpeed.ResetCache();
            }
            if (GUILayout.Button("重载", GUILayout.Width(50))) ItemSpeed.ResetCache();
            GUILayout.EndHorizontal();

            GUILayout.Label($"该物品倍率：x{ItemSpeed.Multiplier:0.0}");
            ItemSpeed.Multiplier = ItemSpeed.ClampMultiplier(
                GUILayout.HorizontalSlider(ItemSpeed.Multiplier, 1f, 8f));
            GUILayout.Label($"<size=11>已匹配：{ItemSpeed.LastMatched}</size>");

            GUILayout.Space(6);
            GUILayout.Label("<b>生存 / 研究</b>");
            Trainer.Immortal = GUILayout.Toggle(Trainer.Immortal, " 不会饿死 / 渴死");
            Trainer.ResearchPointsEnabled = GUILayout.Toggle(Trainer.ResearchPointsEnabled, " 无限研究点");
            GUILayout.BeginHorizontal();
            GUILayout.Label("点数：", GUILayout.Width(48));
            string rp = GUILayout.TextField(Trainer.ResearchPointsValue.ToString(), GUILayout.Width(90));
            int parsed;
            if (int.TryParse(rp, out parsed) && parsed > 0) Trainer.ResearchPointsValue = parsed;
            GUILayout.EndHorizontal();
            Trainer.InstantResearch = GUILayout.Toggle(Trainer.InstantResearch, " 研究瞬间完成");

            GUILayout.Space(8);
            DrawMaterialSection();

            GUILayout.Space(8);
            if (GUILayout.Button("恢复默认（关闭全部修改）"))
            {
                Trainer.SpeedEnabled = false;
                Trainer.Immortal = false;
                Trainer.ResearchPointsEnabled = false;
                Trainer.InstantResearch = false;
                RestoreSpeed();
            }

            GUILayout.Label("<size=11>F1 隐藏面板 · 改材料前请先存档</size>");
            GUILayout.EndScrollView();

            GUI.DragWindow(new Rect(0, 0, _window.width, 24));
        }

        private static void DrawMaterialSection()
        {
            GUILayout.Label("<b>材料编辑</b>（输入数量后点「设」，直接改成该总数）");

            GUILayout.BeginHorizontal();
            GUILayout.Label("搜索：", GUILayout.Width(44));
            string newSearch = GUILayout.TextField(_search ?? "", GUILayout.Width(220));
            if (newSearch != _search)
            {
                _search = newSearch;
                _filtered = null;
            }
            if (GUILayout.Button("清空", GUILayout.Width(50))) { _search = ""; _filtered = null; }
            GUILayout.EndHorizontal();

            var items = ItemEditor.All;
            if (items.Length == 0)
            {
                GUILayout.Label("未加载到物品注册表（可能还没进入存档）");
                return;
            }

            if (_filtered == null || _filteredFor != _search)
            {
                _filtered = FilterItems(items, _search);
                _filteredFor = _search;
            }

            GUILayout.Label($"<size=11>共 {_filtered.Length} 项匹配</size>");

            _materialScroll = GUILayout.BeginScrollView(_materialScroll, GUILayout.Height(230));

            int shown = 0;
            foreach (var p in _filtered)
            {
                if (shown++ >= 120) break;
                DrawMaterialRow(p);
            }

            GUILayout.EndScrollView();

            if (!string.IsNullOrEmpty(_lastResult) && Time.unscaledTime - _lastResultTime < 6f)
            {
                GUILayout.Label("<color=#7fd6a5>" + _lastResult + "</color>");
            }
        }

        private static void DrawMaterialRow(ItemProperties p)
        {
            string name = ItemEditor.NameOf(p);
            int count = ItemEditor.CountOf(p);

            GUILayout.BeginHorizontal();
            GUILayout.Label(name, GUILayout.Width(150));
            GUILayout.Label(count.ToString(), GUILayout.Width(60));

            string buf;
            if (!_inputBuffers.TryGetValue(p, out buf))
            {
                buf = count.ToString();
                _inputBuffers[p] = buf;
            }
            string newBuf = GUILayout.TextField(buf, GUILayout.Width(70));
            if (newBuf != buf) _inputBuffers[p] = newBuf;

            if (GUILayout.Button("设", GUILayout.Width(40)))
            {
                int target;
                if (int.TryParse(_inputBuffers[p], out target))
                {
                    string msg;
                    ItemEditor.SetCount(p, target, out msg);
                    _lastResult = $"{name}：{msg}";
                    _lastResultTime = Time.unscaledTime;
                    _inputBuffers[p] = target.ToString();
                }
                else
                {
                    _lastResult = $"{name}：请输入数字";
                    _lastResultTime = Time.unscaledTime;
                }
            }

            GUILayout.EndHorizontal();
        }

        private static ItemProperties[] FilterItems(ItemProperties[] all, string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return all;
            }

            var hit = new List<ItemProperties>();
            string k = keyword.Trim();
            foreach (var p in all)
            {
                if (p == null) continue;

                string localized = ItemEditor.NameOf(p);
                string asset = "";
                try { asset = p.name ?? ""; } catch { /* 忽略 */ }

                bool match = (localized != null && localized.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)
                          || (asset != null && asset.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);

                if (match) hit.Add(p);
            }
            return hit.ToArray();
        }
    }
}
