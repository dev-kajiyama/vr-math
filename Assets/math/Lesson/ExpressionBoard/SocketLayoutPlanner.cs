using System.Collections.Generic;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VrMath.Interaction;

namespace VrMath.Lesson
{
    /// <summary>
    /// 生成済みの問題を、平均台上のどのソケットへ割り当てるか決めます。
    /// </summary>
    public sealed class ExpressionBalanceSocketLayoutPlanner
    {
        /// <summary>
        /// 左辺に x と offset 個の重り、右辺に rightTotal 個の重りが入る配置を計画します。
        /// </summary>
        public bool TryPlan(
            ExpressionBalanceProblem problem,
            IReadOnlyList<XRSocketInteractor> leftSockets,
            IReadOnlyList<XRSocketInteractor> rightSockets,
            out ExpressionBalanceSocketLayoutPlan plan,
            out string error)
        {
            plan = default;
            error = "";

            // null ソケットは候補から落とし、以降の配置計算をシンプルにする。
            var usableLeftSockets = leftSockets?.Where(socket => socket != null).ToList() ?? new List<XRSocketInteractor>();
            var usableRightSockets = rightSockets?.Where(socket => socket != null).ToList() ?? new List<XRSocketInteractor>();

            // 左辺には必ず x 箱が 1 つあり、その後ろに offset 個の重りを置く。
            var leftTermCount = 1 + problem.Offset;

            // 左辺の必要枠が足りなければ、配置前に理由つきで止める。
            if (usableLeftSockets.Count < leftTermCount)
            {
                error = $"left sockets are insufficient. required={leftTermCount}, actual={usableLeftSockets.Count}";
                return false;
            }

            // 右辺には RightTotal 個の 1 重りを置くので、その分のソケットが必要。
            if (usableRightSockets.Count < problem.RightTotal)
            {
                error = $"right sockets are insufficient. required={problem.RightTotal}, actual={usableRightSockets.Count}";
                return false;
            }

            // 左辺は右寄せで置く。例: 4 ソケットなら [_][x][1][1] のように見せる。
            var leftStartIndex = usableLeftSockets.Count - leftTermCount;
            var variableSocket = GetSocketByVisualIndex(usableLeftSockets, leftStartIndex);
            var leftWeightSockets = new List<XRSocketInteractor>();
            for (var i = 0; i < problem.Offset; i++)
            {
                // x の直後に offset 個の重りを並べる。
                leftWeightSockets.Add(GetSocketByVisualIndex(usableLeftSockets, leftStartIndex + 1 + i));
            }

            // 右辺は左から順に RightTotal 個の重りを並べる。
            var rightWeightSockets = new List<XRSocketInteractor>();
            for (var i = 0; i < problem.RightTotal; i++)
            {
                rightWeightSockets.Add(GetSocketByVisualIndex(usableRightSockets, i));
            }

            if (variableSocket == null || leftWeightSockets.Any(socket => socket == null) || rightWeightSockets.Any(socket => socket == null))
            {
                // Index 指定や候補リストに矛盾があるとここに来る。
                error = "one or more planned sockets could not be resolved";
                return false;
            }

            plan = new ExpressionBalanceSocketLayoutPlan(variableSocket, leftWeightSockets, rightWeightSockets);
            return true;
        }

        /// <summary>
        /// BalanceBoardSocketSlot の Index を優先し、無ければリスト順でソケットを取得します。
        /// </summary>
        private static XRSocketInteractor GetSocketByVisualIndex(IReadOnlyList<XRSocketInteractor> sockets, int index)
        {
            // 手動で付けた BalanceBoardSocketSlot.Index があれば、見た目上の番号として優先する。
            foreach (var socket in sockets)
            {
                var slot = socket != null ? socket.GetComponent<BalanceBoardSocketSlot>() : null;
                if (slot != null && slot.Index == index)
                {
                    return socket;
                }
            }

            // Index コンポーネントが無い場合は、Bootstrap が並べたリスト順を番号として使う。
            return index >= 0 && index < sockets.Count ? sockets[index] : null;
        }
    }
}
