using HarmonyLib;
using UnityEngine;

namespace FlotsamTrainer.Patches
{
    /// <summary>
    /// 拦截 GameSpeedManager.SetGameSpeedAndTimeScale，记录游戏自己设定的基准 timeScale，
    /// 之后按倍率放大。基准值单独保存，避免多次补丁导致倍率被反复相乘。
    /// </summary>
    [HarmonyPatch(typeof(GameSpeedManager), "SetGameSpeedAndTimeScale")]
    internal static class SpeedPatch
    {
        internal static float BaseScale = 1f;
        internal static bool Captured;

        [HarmonyPostfix]
        private static void Postfix()
        {
            BaseScale = Time.timeScale;
            Captured = true;

            if (Trainer.SpeedEnabled && BaseScale > 0.0001f)
            {
                Time.timeScale = BaseScale * Trainer.SpeedMultiplier;
            }
        }
    }
}
