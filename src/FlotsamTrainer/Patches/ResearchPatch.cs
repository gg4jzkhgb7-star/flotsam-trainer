using HarmonyLib;

namespace FlotsamTrainer.Patches
{
    /// <summary>
    /// 研究进度直接顶到上限。CurrentProgress 是"已投入点数"，CurrentCost 是所需点数，
    /// 把前者改成后者等于瞬间完成，但仍走游戏原本的完成流程。
    /// </summary>
    [HarmonyPatch(typeof(CommunityResearch), "get_CurrentProgress")]
    internal static class ResearchPatch
    {
        [HarmonyPostfix]
        private static void Postfix(CommunityResearch __instance, ref int __result)
        {
            if (!Trainer.InstantResearch) return;

            try
            {
                if (__instance.CurrentResearch == null) return;
                __result = __instance.CurrentCost;
            }
            catch { /* 忽略 */ }
        }
    }
}
