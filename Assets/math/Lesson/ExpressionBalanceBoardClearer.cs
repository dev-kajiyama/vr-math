using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VrMath.Lesson
{
    public sealed class ExpressionBalanceBoardClearer
    {
        public void Clear(
            IReadOnlyList<XRSocketInteractor> leftSockets,
            IReadOnlyList<XRSocketInteractor> rightSockets,
            IReadOnlyList<WeightedDumbbell> weights,
            XRGrabInteractable variableBox,
            Transform boardRoot,
            float boardHalfWidth)
        {
            ClearSocketSelections(leftSockets);
            ClearSocketSelections(rightSockets);
            ClearWeights(weights, boardRoot, boardHalfWidth, resetWeightValues: true);
            ClearVariableBox(variableBox, boardRoot, boardHalfWidth);
        }

        public void ClearUnusedWeights(
            IReadOnlyList<WeightedDumbbell> allWeights,
            IReadOnlyCollection<WeightedDumbbell> usedWeights,
            Transform boardRoot,
            float boardHalfWidth)
        {
            var unusedWeights = allWeights
                .Where(weight => weight != null && !usedWeights.Contains(weight))
                .ToList();
            ClearWeights(unusedWeights, boardRoot, boardHalfWidth, resetWeightValues: true);
        }

        private static void ClearSocketSelections(IReadOnlyList<XRSocketInteractor> sockets)
        {
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
                    socket.interactionManager = Object.FindAnyObjectByType<XRInteractionManager>();
                }

                if (socket.interactionManager == null)
                {
                    continue;
                }

                var selected = socket.interactablesSelected.ToArray();
                foreach (var interactable in selected)
                {
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
                    weight.Weight.Value = 1f;
                }

                var row = i / 6;
                var column = i % 6;
                var localPosition = new Vector3(boardHalfWidth + 0.85f + column * 0.14f, 0.9f, -0.75f - row * 0.14f);
                weight.transform.SetPositionAndRotation(boardRoot.TransformPoint(localPosition), boardRoot.rotation);

                if (weight.TryGetComponent(out Rigidbody rb))
                {
                    rb.isKinematic = false;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
        }

        private static void ClearVariableBox(XRGrabInteractable variableBox, Transform boardRoot, float boardHalfWidth)
        {
            if (variableBox == null || boardRoot == null)
            {
                return;
            }

            var localPosition = new Vector3(-boardHalfWidth - 0.85f, 0.9f, -0.75f);
            variableBox.transform.SetPositionAndRotation(boardRoot.TransformPoint(localPosition), boardRoot.rotation);

            if (variableBox.TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
