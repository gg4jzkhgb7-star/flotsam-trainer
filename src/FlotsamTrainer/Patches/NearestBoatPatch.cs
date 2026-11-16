using HarmonyLib;
using UnityEngine;

namespace FlotsamTrainer.Patches
{
    /// <summary>
    /// 让"船离打捞点更近"的漂流者优先接这个打捞项目。
    ///
    /// 排查结论：游戏里并没有"按船的远近挑船"的代码 ——
    /// Community.TryReturnClosestMooringpointWithAvailableBoat 在整个程序集里 0 处引用（死代码）。
    /// 实际决定派谁去的是 Project.ReturnAgentPriority(Agent)：返回越大越优先。
    /// 所以这里在原有优先级上叠加一个"按船长当前船只到目标点距离"的加分，距离越近加分越多。
    /// </summary>
    [HarmonyPatch(typeof(Project), "ReturnAgentPriority")]
    internal static class NearestBoatPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Project __instance, Agent agent, ref int __result)
        {
            if (!Trainer.NearestBoat) return;

            try
            {
                if (__instance == null || agent == null) return;
                if (__instance.SalvageTarget == null) return;   // 只影响打捞类项目

                var boat = agent.Boat;
                if (boat == null) return;

                Vector3 target;
                if (!TryGetTargetPosition(__instance, out target)) return;

                float distance = Vector3.Distance(boat.transform.position, target);

                // 距离 0 → +2000，距离 100 → +20，基本只影响同项目内部的排序
                int bonus = Mathf.RoundToInt(2000f / (1f + distance));
                if (bonus > 0) __result += bonus;
            }
            catch { /* 忽略 */ }
        }

        private static bool TryGetTargetPosition(Project project, out Vector3 position)
        {
            position = Vector3.zero;

            try
            {
                var target = project.Target;
                if (target != null)
                {
                    position = target.transform.position;
                    return true;
                }
            }
            catch { /* 继续尝试 */ }

            try
            {
                var navigationTarget = project.NavigationTarget;
                if (navigationTarget != null)
                {
                    position = navigationTarget.transform.position;
                    return true;
                }
            }
            catch { /* 继续尝试 */ }

            try
            {
                var buildable = project.TargetBuildable;
                if (buildable != null)
                {
                    position = buildable.transform.position;
                    return true;
                }
            }
            catch { /* 忽略 */ }

            return false;
        }
    }
}
