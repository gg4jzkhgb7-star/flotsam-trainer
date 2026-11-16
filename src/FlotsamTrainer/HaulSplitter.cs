using System.Reflection;
using UnityEngine;

namespace FlotsamTrainer
{
    /// <summary>打捞配额模式。</summary>
    internal enum HaulMode
    {
        /// <summary>智能（默认）：物品少于单船容量时均分（多船并行），超过时按船满载分批，少跑腿</summary>
        Smart = 0,

        /// <summary>满载优先：能装多少装多少，船不空跑（接近原版）</summary>
        FullLoad = 1,

        /// <summary>均分：每人 ceil(总数/人数)，人多时并行，但船可能装不满</summary>
        EvenSplit = 2,

        /// <summary>每人固定件数</summary>
        FixedPerAgent = 3
    }

    /// <summary>
    /// 打捞配额。
    ///
    /// 原版逻辑（IL 已核实）：每个 ProjectAssignment 会不停往自己身上塞物品，
    /// 直到 ReturnRemainingItemToHaulCount >= ReturnItemHaulingCapacity（漂流者背包 / 渔船货舱容量）。
    //
    /// 注意这是一组互相矛盾的诉求，只能靠模式切换：
    ///   - 船装满  ⇔ 物品少时只派一条船（少派船、少跑腿）
    ///   - 多船并行 ⇔ 每条船只装一部分（船装不满）
    /// 默认走"满载优先"，也就是效率优先。
    /// </summary>
    internal static class HaulSplitter
    {
        private static MethodInfo _remaining;
        private static MethodInfo _capacity;

        /// <summary>返回该指派最多能拿几件；0 表示不限制（拿满容量为止）。</summary>
        internal static int ShareFor(ProjectAssignment assignment)
        {
            if (assignment == null) return 0;

            var project = assignment.Project;
            if (project == null) return 0;

            // 只对"打捞类"项目生效，避免影响建造、搬运建材等正常物流
            if (project.SalvageTarget == null) return 0;

            int capacity = HaulingCapacity(assignment);
            if (capacity <= 0) return 0;

            int agents = project.AssignmentLimit;
            if (agents <= 0) agents = 1;

            switch (Trainer.HaulMode)
            {
                case HaulMode.Smart:
                {
                    if (agents <= 1) return 0;

                    int total = SalvageableCount(project);
                    if (total <= 0) return 0;

                    // 物品装不满一条船：均分，让被指派的人都能上（解决"只派一条船"）
                    if (total <= capacity)
                    {
                        int even = Mathf.CeilToInt(total / (float)agents);
                        return Mathf.Clamp(even, 1, capacity);
                    }

                    // 物品超过一条船：按需要的船数满载，避免每条船只装一半、来回多跑
                    int boatsNeeded = Mathf.CeilToInt(total / (float)capacity);
                    int workers = Mathf.Min(agents, boatsNeeded);
                    if (workers < 1) workers = 1;

                    int balanced = Mathf.CeilToInt(total / (float)workers);
                    return Mathf.Clamp(balanced, 1, capacity);
                }

                case HaulMode.FullLoad:
                    return 0;

                case HaulMode.FixedPerAgent:
                {
                    int per = Mathf.Clamp(Trainer.FixedPerAgentCount, 1, capacity);
                    return agents <= 1 ? 0 : per;
                }

                default:
                {
                    if (agents <= 1) return 0;

                    int total = SalvageableCount(project);
                    if (total <= 0) return 0;

                    int share = Mathf.CeilToInt(total / (float)agents);
                    if (share < 1) share = 1;
                    if (share > capacity) share = capacity;

                    // 别人已经分走一部分、且剩余不足一份时，当前这个人直接做完，
                    // 免得为了零头再跑一趟船。
                    int allocated = AllocatedCount(project);
                    if (allocated > 0 && total - allocated <= share) return 0;

                    return share;
                }
            }
        }

        internal static int Remaining(ProjectAssignment assignment)
        {
            try
            {
                if (_remaining == null)
                {
                    _remaining = typeof(ProjectAssignment).GetMethod("ReturnRemainingItemToHaulCount",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }

                if (_remaining != null)
                {
                    return (int)_remaining.Invoke(assignment, null);
                }
            }
            catch { /* 忽略 */ }

            return 0;
        }

        /// <summary>供面板显示：某个指派当前的搬运容量。</summary>
        internal static int CapacityOf(ProjectAssignment assignment) => HaulingCapacity(assignment);

        private static int AllocatedCount(Project project)
        {
            int sum = 0;

            try
            {
                var assignments = project.Assignments;
                if (assignments == null) return 0;

                foreach (var a in assignments)
                {
                    if (a == null) continue;
                    sum += Remaining(a);
                }
            }
            catch { /* 忽略 */ }

            return sum;
        }

        private static int HaulingCapacity(ProjectAssignment assignment)
        {
            try
            {
                if (_capacity == null)
                {
                    _capacity = typeof(ProjectAssignment).GetMethod("ReturnItemHaulingCapacity",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }

                if (_capacity != null)
                {
                    return (int)_capacity.Invoke(assignment, null);
                }
            }
            catch { /* 忽略 */ }

            return 0;
        }

        private static int SalvageableCount(Project project)
        {
            try
            {
                var marker = project.SalvageTarget as Marker;
                var list = marker?.SalvageableItemsInRadius;
                if (list != null && list.Count > 0) return list.Count;
            }
            catch { /* 忽略 */ }

            return 0;
        }
    }
}
