using System.Collections.Generic;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VrMath.Interaction;

namespace VrMath.Lesson
{
    public sealed class ExpressionBalanceSocketLayoutPlanner
    {
        public bool TryPlan(
            ExpressionBalanceProblem problem,
            IReadOnlyList<XRSocketInteractor> leftSockets,
            IReadOnlyList<XRSocketInteractor> rightSockets,
            out ExpressionBalanceSocketLayoutPlan plan,
            out string error)
        {
            plan = default;
            error = "";

            var usableLeftSockets = leftSockets?.Where(socket => socket != null).ToList() ?? new List<XRSocketInteractor>();
            var usableRightSockets = rightSockets?.Where(socket => socket != null).ToList() ?? new List<XRSocketInteractor>();
            var leftTermCount = 1 + problem.Offset;

            if (usableLeftSockets.Count < leftTermCount)
            {
                error = $"left sockets are insufficient. required={leftTermCount}, actual={usableLeftSockets.Count}";
                return false;
            }

            if (usableRightSockets.Count < problem.RightTotal)
            {
                error = $"right sockets are insufficient. required={problem.RightTotal}, actual={usableRightSockets.Count}";
                return false;
            }

            var leftStartIndex = usableLeftSockets.Count - leftTermCount;
            var variableSocket = GetSocketByVisualIndex(usableLeftSockets, leftStartIndex);
            var leftWeightSockets = new List<XRSocketInteractor>();
            for (var i = 0; i < problem.Offset; i++)
            {
                leftWeightSockets.Add(GetSocketByVisualIndex(usableLeftSockets, leftStartIndex + 1 + i));
            }

            var rightWeightSockets = new List<XRSocketInteractor>();
            for (var i = 0; i < problem.RightTotal; i++)
            {
                rightWeightSockets.Add(GetSocketByVisualIndex(usableRightSockets, i));
            }

            if (variableSocket == null || leftWeightSockets.Any(socket => socket == null) || rightWeightSockets.Any(socket => socket == null))
            {
                error = "one or more planned sockets could not be resolved";
                return false;
            }

            plan = new ExpressionBalanceSocketLayoutPlan(variableSocket, leftWeightSockets, rightWeightSockets);
            return true;
        }

        private static XRSocketInteractor GetSocketByVisualIndex(IReadOnlyList<XRSocketInteractor> sockets, int index)
        {
            foreach (var socket in sockets)
            {
                var slot = socket != null ? socket.GetComponent<BalanceBoardSocketSlot>() : null;
                if (slot != null && slot.Index == index)
                {
                    return socket;
                }
            }

            return index >= 0 && index < sockets.Count ? sockets[index] : null;
        }
    }
}
