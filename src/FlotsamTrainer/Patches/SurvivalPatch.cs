using HarmonyLib;
using PajamaLlama.Fltsm;

namespace FlotsamTrainer.Patches
{
    /// <summary>
    /// 饥饿/口渴的死亡判定。DietVital.IsCauseOfDeath 返回 true 时漂流者会死，
    /// 这里强行返回 false，等于"不会饿死/渴死"，但不影响正常吃饭喝水流程。
    /// </summary>
    [HarmonyPatch(typeof(DietVital), "IsCauseOfDeath")]
    internal static class SurvivalPatch
    {
        [HarmonyPostfix]
        private static void Postfix(ref bool __result)
        {
            if (Trainer.Immortal)
            {
                __result = false;
            }
        }
    }
}
