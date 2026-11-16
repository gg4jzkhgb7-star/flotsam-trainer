using UnityEngine;

namespace FlotsamTrainer
{
    /// <summary>
    /// 修改器的全局状态。所有开关集中在这里，补丁与 UI 都读这些字段。
    /// </summary>
    internal static class Trainer
    {
        // ---- 游戏速度 ----
        public static bool SpeedEnabled;
        public static float SpeedMultiplier = 2f;
        public const float MinMultiplier = 0.5f;
        public const float MaxMultiplier = 10f;

        // ---- 打捞/搬运动作速度 ----
        public static bool WorkSpeedEnabled;
        public static float WorkSpeedMultiplier = 2f;
        public const float MaxWorkSpeed = 5f;

        // ---- 打捞配额 ----
        public static bool HaulSplit = true;
        public static HaulMode HaulMode = HaulMode.Smart;
        public static int FixedPerAgentCount = 6;

        // ---- 优先派最近的船 ----
        public static bool NearestBoat = true;

        // ---- 生存 ----
        public static bool Immortal;

        // ---- 研究 ----
        public static bool ResearchPointsEnabled;
        public static int ResearchPointsValue = 9999;
        public static bool InstantResearch;

        // ---- 运行时信息（只读，给面板显示） ----
        public static string CommunityName = "-";
        public static int AgentCount;
        public static int ResearchPointsNow;
        public static int StorageCount;

        private static float _nextInfoRefresh;

        internal static void Tick()
        {
            ApplySpeed();

            if (Time.unscaledTime >= _nextInfoRefresh)
            {
                _nextInfoRefresh = Time.unscaledTime + 0.5f;
                RefreshInfo();
            }

            if (ResearchPointsEnabled)
            {
                try
                {
                    var community = GameRefs.PlayerCommunity;
                    if (community != null && community.Research != null)
                    {
                        community.Research.ResearchPoints = ResearchPointsValue;
                    }
                }
                catch { /* 场景切换时静默失败 */ }
            }
        }

        /// <summary>
        /// 每帧把 timeScale 重设为「基准值 × 倍率」，这样拖动滑块能立即生效。
        /// 基准值来自 SpeedPatch 捕获的游戏原始设置，暂停时基准为 0，此处保持不动。
        /// </summary>
        private static void ApplySpeed()
        {
            if (!SpeedEnabled) return;

            float baseScale = Patches.SpeedPatch.Captured ? Patches.SpeedPatch.BaseScale : 0f;

            // 补丁没捕获到（例如被内联）时，从 GameplaySettings 直接读原始倍率
            if (baseScale <= 0.0001f)
            {
                float fallback;
                if (GameRefs.TryGetBaseTimeScale(out fallback))
                {
                    baseScale = fallback;
                }
            }

            if (baseScale <= 0.0001f) return;

            float target = Mathf.Clamp(baseScale * SpeedMultiplier, 0f, 40f);
            if (!Mathf.Approximately(Time.timeScale, target))
            {
                Time.timeScale = target;
            }
        }

        private static void RefreshInfo()
        {
            try
            {
                var community = GameRefs.PlayerCommunity;
                if (community == null)
                {
                    CommunityName = "-";
                    AgentCount = 0;
                    ResearchPointsNow = 0;
                    StorageCount = 0;
                    return;
                }

                CommunityName = community.Name ?? "-";
                AgentCount = community.Agents != null ? community.Agents.Count : 0;
                StorageCount = community.Storages != null ? community.Storages.Count : 0;
                ResearchPointsNow = community.Research != null ? community.Research.ResearchPoints : 0;
            }
            catch { /* 忽略 */ }
        }
    }
}
