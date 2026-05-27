using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VrMath.Lesson
{
    /// <summary>
    /// 1 問分の式を、どのソケットへ置くか決めた結果です。
    /// </summary>
    public readonly struct ExpressionBalanceSocketLayoutPlan
    {
        /// <summary>
        /// x 箱用ソケット、左辺の重り用ソケット、右辺の重り用ソケットを保持します。
        /// </summary>
        public ExpressionBalanceSocketLayoutPlan(
            XRSocketInteractor variableSocket,
            IReadOnlyList<XRSocketInteractor> leftWeightSockets,
            IReadOnlyList<XRSocketInteractor> rightWeightSockets)
        {
            VariableSocket = variableSocket;
            LeftWeightSockets = leftWeightSockets;
            RightWeightSockets = rightWeightSockets;
        }

        /// <summary>
        /// x 箱を置くソケットです。
        /// </summary>
        public XRSocketInteractor VariableSocket { get; }

        /// <summary>
        /// 左辺の + offset 分の重りを置くソケットです。
        /// </summary>
        public IReadOnlyList<XRSocketInteractor> LeftWeightSockets { get; }

        /// <summary>
        /// 右辺の合計値分の重りを置くソケットです。
        /// </summary>
        public IReadOnlyList<XRSocketInteractor> RightWeightSockets { get; }

        /// <summary>
        /// この配置に必要な重りの個数です。
        /// </summary>
        public int RequiredWeightCount => (LeftWeightSockets?.Count ?? 0) + (RightWeightSockets?.Count ?? 0);

        /// <summary>
        /// 配置に必要なソケット情報がそろっているかを返します。
        /// </summary>
        public bool IsValid => VariableSocket != null && LeftWeightSockets != null && RightWeightSockets != null;
    }
}
