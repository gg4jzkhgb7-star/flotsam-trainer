using HarmonyLib;

namespace FlotsamTrainer.Patches
{
    /// <summary>
    /// 记录"这一趟搬的是哪个物品"，供速度补丁判断是否需要额外加速。
    /// TransferItemCoroutine 是运送/打捞单件物品的统一入口，协程创建时就会走到这里。
    /// </summary>
    [HarmonyPatch(typeof(ItemToHaul), "TransferItemCoroutine")]
    internal static class ItemSpeedPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ItemToHaul __instance, Agent agent)
        {
            if (!ItemSpeed.Enabled) return;

            try
            {
                var item = __instance?.Item;
                ItemSpeed.Mark(agent, item?.Properties);
            }
            catch { /* 忽略 */ }
        }
    }
}
