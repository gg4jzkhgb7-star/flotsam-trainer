using HarmonyLib;

namespace FlotsamTrainer.Patches
{
    /// <summary>
    /// 拦截"给指派塞待搬物品"。当该指派已经拿到自己的份额时直接返回 false，
    /// 调用方的循环会自然停止（这正是原版背包塞满时的行为），剩余物品留给其他人。
    /// </summary>
    [HarmonyPatch(typeof(ProjectAssignment), "AddItemToHaul", new[] { typeof(Item), typeof(SubInventoryType) })]
    internal static class HaulSplitPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(ProjectAssignment __instance, ref bool __result)
        {
            if (!Trainer.HaulSplit) return true;

            try
            {
                int share = HaulSplitter.ShareFor(__instance);
                if (share <= 0) return true;

                if (HaulSplitter.Remaining(__instance) >= share)
                {
                    __result = false;
                    return false;
                }
            }
            catch { /* 出错就让原逻辑继续 */ }

            return true;
        }
    }
}
