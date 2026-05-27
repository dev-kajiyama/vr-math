using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VrMath.Lesson
{
    /// <summary>
    /// 平均台上のソケット選択、重り、x 箱を初期位置へ戻す処理を担当します。
    /// </summary>
    public sealed class ExpressionBalanceBoardClearer
    {
        /// <summary>
        /// 左右ソケットの選択解除、重りの退避、x 箱の退避をまとめて実行します。
        /// </summary>
        public void Clear(
            IReadOnlyList<XRSocketInteractor> leftSockets,
            IReadOnlyList<XRSocketInteractor> rightSockets,
            IReadOnlyList<WeightedDumbbell> weights,
            XRGrabInteractable variableBox,
            Transform boardRoot,
            float boardHalfWidth)
        {
            // 先にソケットの選択を外してから、Transform を動かす。
            // 選択されたまま移動すると、XR 側が次フレームで位置を戻すことがある。
            ClearSocketSelections(leftSockets);
            ClearSocketSelections(rightSockets);

            // 式問題では重りの値は基本 1 に戻してから、板の外へ並べ直す。
            ClearWeights(weights, boardRoot, boardHalfWidth, resetWeightValues: true);

            // x 箱は重りとは反対側に退避させ、見た目にも別物として分かるようにする。
            ClearVariableBox(variableBox, boardRoot, boardHalfWidth);
        }

        /// <summary>
        /// 配置に使った重り以外を板の外へ退避させ、余計な重りが式に混ざらないようにします。
        /// </summary>
        public void ClearUnusedWeights(
            IReadOnlyList<WeightedDumbbell> allWeights,
            IReadOnlyCollection<WeightedDumbbell> usedWeights,
            Transform boardRoot,
            float boardHalfWidth)
        {
            // 今回使った重りはソケット上に残す。使わなかったものだけ片付ける。
            var unusedWeights = allWeights
                .Where(weight => weight != null && !usedWeights.Contains(weight))
                .ToList();
            ClearWeights(unusedWeights, boardRoot, boardHalfWidth, resetWeightValues: true);
        }

        private static void ClearSocketSelections(IReadOnlyList<XRSocketInteractor> sockets)
        {
            // XRSocketInteractor が保持している選択状態を、InteractionManager 経由で正式に解除する。
            if (sockets == null)
            {
                return;
            }

            foreach (var socket in sockets)
            {
                if (socket == null || !socket.hasSelection)
                {
                    continue;
                }

                if (socket.interactionManager == null)
                {
                    // 手作業で置いたソケットは InteractionManager が未設定のことがあるため補完する。
                    socket.interactionManager = Object.FindAnyObjectByType<XRInteractionManager>();
                }

                if (socket.interactionManager == null)
                {
                    continue;
                }

                var selected = socket.interactablesSelected.ToArray();
                foreach (var interactable in selected)
                {
                    // 選択リストを直接いじらず、XR Interaction Toolkit の API で外す。
                    socket.interactionManager.SelectExit(socket, interactable);
                }
            }
        }

        private static void ClearWeights(
            IReadOnlyList<WeightedDumbbell> weights,
            Transform boardRoot,
            float boardHalfWidth,
            bool resetWeightValues)
        {
            // 重りは平均台の右外側へ小さなグリッド状に退避させる。
            if (weights == null || boardRoot == null)
            {
                return;
            }

            for (var i = 0; i < weights.Count; i++)
            {
                var weight = weights[i];
                if (weight == null)
                {
                    continue;
                }

                if (resetWeightValues)
                {
                    // ランダム問題や過去の操作で値が変わっていても、次問では 1 として使う。
                    weight.Weight.Value = 1f;
                }

                // boardRoot のローカル座標で位置を作るので、平均台が移動していても相対位置が保たれる。
                var row = i / 6;
                var column = i % 6;
                var localPosition = new Vector3(boardHalfWidth + 0.85f + column * 0.14f, 0.9f, -0.75f - row * 0.14f);
                weight.transform.SetPositionAndRotation(boardRoot.TransformPoint(localPosition), boardRoot.rotation);

                if (weight.TryGetComponent(out Rigidbody rb))
                {
                    // 退避直後に前の速度で飛ばないよう、物理速度を消す。
                    rb.isKinematic = false;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }

        private static void ClearVariableBox(XRGrabInteractable variableBox, Transform boardRoot, float boardHalfWidth)
        {
            // x 箱は平均台の左外側へ退避させる。
            if (variableBox == null || boardRoot == null)
            {
                return;
            }

            var localPosition = new Vector3(-boardHalfWidth - 0.85f, 0.9f, -0.75f);
            variableBox.transform.SetPositionAndRotation(boardRoot.TransformPoint(localPosition), boardRoot.rotation);

            if (variableBox.TryGetComponent(out Rigidbody rb))
            {
                // プレイヤーが掴んだ直後の速度が残らないようにする。
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
