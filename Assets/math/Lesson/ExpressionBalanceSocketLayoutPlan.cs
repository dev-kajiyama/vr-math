using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VrMath.Lesson
{
    public readonly struct ExpressionBalanceSocketLayoutPlan
    {
        public ExpressionBalanceSocketLayoutPlan(
            XRSocketInteractor variableSocket,
            IReadOnlyList<XRSocketInteractor> leftWeightSockets,
            IReadOnlyList<XRSocketInteractor> rightWeightSockets)
        {
            VariableSocket = variableSocket;
            LeftWeightSockets = leftWeightSockets;
            RightWeightSockets = rightWeightSockets;
        }

        public XRSocketInteractor VariableSocket { get; }
        public IReadOnlyList<XRSocketInteractor> LeftWeightSockets { get; }
        public IReadOnlyList<XRSocketInteractor> RightWeightSockets { get; }
        public int RequiredWeightCount => (LeftWeightSockets?.Count ?? 0) + (RightWeightSockets?.Count ?? 0);
        public bool IsValid => VariableSocket != null && LeftWeightSockets != null && RightWeightSockets != null;
    }
}
