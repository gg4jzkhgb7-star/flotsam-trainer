using HarmonyLib;

namespace FlotsamTrainer.Patches
{
    /// <summary>
    /// 打捞/搬运速度。
    ///
    /// 原版单件物品的耗时不是固定秒数，而是等漂流者的取放动画播完（动画事件驱动）。
    /// TransferItemCoroutine 会把角色属性修正值写进 Animator 的 "Speed Multiplier" 参数，
    /// 所以放大这个参数就是加速整段动画，打捞自然变快，视觉上也同步。
    ///
    /// 这里同时处理两种加速：
    ///   1. 全局倍率（Trainer.WorkSpeedEnabled）
    ///   2. 指定物品倍率（ItemSpeed，例如只加速金属废料）
    /// </summary>
    [HarmonyPatch(typeof(AnimatorHelper), "SetFloat")]
    internal static class WorkSpeedPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Agent agent, string name, ref float value)
        {
            try
            {
                if (name != AnimatorHelper.AGENT_PARAMETERS_SpeedMultiplier) return;
                if (value <= 0.01f) return;

                if (Trainer.WorkSpeedEnabled)
                {
                    value *= Trainer.WorkSpeedMultiplier;
                }

                // 只在本趟搬运开始时生效一次（结束时游戏会把它重置为 1）
                if (ItemSpeed.Enabled && ItemSpeed.Consume(agent))
                {
                    value *= ItemSpeed.Multiplier;
                }
            }
            catch { /* 忽略 */ }
        }
    }

    /// <summary>
    /// 打捞站（Salvager 建筑）的单件打捞时长，同样按倍率缩短。
    /// </summary>
    [HarmonyPatch(typeof(Salvager), "ReturnSalvageTime")]
    internal static class SalvageTimePatch
    {
        [HarmonyPostfix]
        private static void Postfix(Item item, ref float __result)
        {
            if (!Trainer.WorkSpeedEnabled) return;

            try
            {
                if (__result <= 0f) return;

                float factor = Trainer.WorkSpeedMultiplier;

                if (ItemSpeed.Enabled && item != null && ItemSpeed.IsTarget(item.Properties))
                {
                    factor *= ItemSpeed.Multiplier;
                }

                __result /= factor;
            }
            catch { /* 忽略 */ }
        }
    }
}
