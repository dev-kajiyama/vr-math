using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VrMath.Lesson
{
    public sealed class ExpressionBalanceSocketPlacer
    {
        public bool TryPlace(
            ExpressionBalanceSocketLayoutPlan plan,
            XRGrabInteractable variableBox,
            IReadOnlyList<WeightedDumbbell> weights,
            out IReadOnlyList<WeightedDumbbell> usedWeights,
            out string error)
        {
            usedWeights = System.Array.Empty<WeightedDumbbell>();
            error = "";

            if (!plan.IsValid)
            {
                error = "layout plan is invalid";
                return false;
            }

            if (variableBox == null)
            {
                error = "variable box is missing";
                return false;
            }

            if (weights == null || weights.Count < plan.RequiredWeightCount)
            {
                error = $"weights are insufficient. required={plan.RequiredWeightCount}, actual={weights?.Count ?? 0}";
                return false;
            }

            var selectedWeights = weights.Take(plan.RequiredWeightCount).ToList();
            PlaceVariableBoxInSocket(variableBox, plan.VariableSocket);

            for (var i = 0; i < plan.LeftWeightSockets.Count; i++)
            {
                var weight = selectedWeights[i];
                weight.Weight.Value = 1f;
                PlaceWeightInSocket(weight, plan.LeftWeightSockets[i]);
            }

            for (var i = 0; i < plan.RightWeightSockets.Count; i++)
            {
                var weight = selectedWeights[plan.LeftWeightSockets.Count + i];
                weight.Weight.Value = 1f;
                PlaceWeightInSocket(weight, plan.RightWeightSockets[i]);
            }

            usedWeights = selectedWeights;
            return true;
        }

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
                socket.interactionManager = Object.FindAnyObjectByType<XRInteractionManager>();
            }

            if (socket.interactionManager != null && !socket.hasSelection)
            {
                socket.StartManualInteraction((IXRSelectInteractable)variableBox);
            }
        }

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
                socket.interactionManager = Object.FindAnyObjectByType<XRInteractionManager>();
            }

            if (grab != null && socket.interactionManager != null && !socket.hasSelection)
            {
                socket.StartManualInteraction((IXRSelectInteractable)grab);
            }
        }

        private static void ResetMotion(Component component)
        {
            if (!component.TryGetComponent(out Rigidbody rb))
            {
                return;
            }

            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }
}
