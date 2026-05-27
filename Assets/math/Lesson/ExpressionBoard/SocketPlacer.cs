using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VrMath.Lesson
{
    /// <summary>
    /// 計画済みのソケット配置に従って、x 箱と重りを実際に平均台へ置きます。
    /// </summary>
    public sealed class ExpressionBalanceSocketPlacer
    {
        /// <summary>
        /// x 箱と必要個数の重りをソケットへ移動し、XRSocketInteractor に手動選択させます。
        /// </summary>
        public bool TryPlace(
            ExpressionBalanceSocketLayoutPlan plan,
            XRGrabInteractable variableBox,
            IReadOnlyList<WeightedDumbbell> weights,
            out IReadOnlyList<WeightedDumbbell> usedWeights,
            out string error)
        {
            usedWeights = System.Array.Empty<WeightedDumbbell>();
            error = "";

            // Planner が作った計画が成立しているかを最初に確認する。
            if (!plan.IsValid)
            {
                error = "layout plan is invalid";
                return false;
            }

            // x 箱が無いと左辺が作れないため配置は中止する。
            if (variableBox == null)
            {
                error = "variable box is missing";
                return false;
            }

            // 必要な重りが足りない場合、部分配置せずエラーにする。
            if (weights == null || weights.Count < plan.RequiredWeightCount)
            {
                error = $"weights are insufficient. required={plan.RequiredWeightCount}, actual={weights?.Count ?? 0}";
                return false;
            }

            // 先頭から必要数だけ使う。呼び出し側でボードに近い順に並べて渡している。
            var selectedWeights = weights.Take(plan.RequiredWeightCount).ToList();

            // まず x 箱を置き、その後に左右の重りを置く。
            PlaceVariableBoxInSocket(variableBox, plan.VariableSocket);

            for (var i = 0; i < plan.LeftWeightSockets.Count; i++)
            {
                // 左辺の offset 個はすべて 1 の重りとして扱う。
                var weight = selectedWeights[i];
                weight.Weight.Value = 1f;
                PlaceWeightInSocket(weight, plan.LeftWeightSockets[i]);
            }

            for (var i = 0; i < plan.RightWeightSockets.Count; i++)
            {
                // 右辺の rightTotal 個もすべて 1 の重りとして扱う。
                var weight = selectedWeights[plan.LeftWeightSockets.Count + i];
                weight.Weight.Value = 1f;
                PlaceWeightInSocket(weight, plan.RightWeightSockets[i]);
            }

            usedWeights = selectedWeights;
            return true;
        }

        /// <summary>
        /// x 箱を指定ソケットの Attach Transform に合わせて固定します。
        /// </summary>
        private static void PlaceVariableBoxInSocket(XRGrabInteractable variableBox, XRSocketInteractor socket)
        {
            if (variableBox == null || socket == null)
            {
                return;
            }

            var attach = socket.attachTransform != null ? socket.attachTransform : socket.transform;
            variableBox.transform.SetPositionAndRotation(attach.position, attach.rotation);
            ResetMotion(variableBox);

            if (socket.interactionManager == null)
            {
                // シーン配置のソケットは manager 未設定の場合があるため、その場で補完する。
                socket.interactionManager = Object.FindAnyObjectByType<XRInteractionManager>();
            }

            if (socket.interactionManager != null && !socket.hasSelection)
            {
                // 手で置いた時と同じ状態にするため、ソケットに明示的に選択させる。
                socket.StartManualInteraction((IXRSelectInteractable)variableBox);
            }
        }

        /// <summary>
        /// 重りを指定ソケットの Attach Transform に合わせて固定します。
        /// </summary>
        private static void PlaceWeightInSocket(WeightedDumbbell weight, XRSocketInteractor socket)
        {
            if (weight == null || socket == null)
            {
                return;
            }

            var attach = socket.attachTransform != null ? socket.attachTransform : socket.transform;
            weight.transform.SetPositionAndRotation(attach.position, attach.rotation);
            ResetMotion(weight);

            var grab = weight.GetComponent<XRGrabInteractable>();
            if (grab != null && socket.interactionManager == null)
            {
                // 手作業で置かれたソケットでも自動配置が動くように manager を探す。
                socket.interactionManager = Object.FindAnyObjectByType<XRInteractionManager>();
            }

            if (grab != null && socket.interactionManager != null && !socket.hasSelection)
            {
                // 位置を合わせるだけではソケットの hasSelection が立たないため、手動選択も行う。
                socket.StartManualInteraction((IXRSelectInteractable)grab);
            }
        }

        /// <summary>
        /// 配置直前に Rigidbody の速度を消し、ソケット上で暴れないようにします。
        /// </summary>
        private static void ResetMotion(Component component)
        {
            if (!component.TryGetComponent(out Rigidbody rb))
            {
                return;
            }

            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            // ソケット上に置いた直後は物理で落ちないよう固定する。
            rb.isKinematic = true;
        }
    }
}
