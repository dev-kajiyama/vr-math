using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VrMath.Core;

namespace VrMath.Lesson
{
    public sealed class ExpressionBalanceBoardReader
    {
        private readonly List<int> unknownWeights = new();
        private readonly List<int> leftWeights = new();
        private readonly List<int> rightWeights = new();

        public ExpressionBalanceUnknownSideState ReadUnknownSide(
            BalanceSide unknownSide,
            IReadOnlyList<XRSocketInteractor> leftSockets,
            IReadOnlyList<XRSocketInteractor> rightSockets)
        {
            unknownWeights.Clear();
            var sockets = unknownSide == BalanceSide.Left ? leftSockets : rightSockets;
            ReadWeights(sockets, unknownWeights);

            if (unknownWeights.Count == 0)
            {
                return new ExpressionBalanceUnknownSideState("?", 0);
            }

            unknownWeights.Sort();
            return new ExpressionBalanceUnknownSideState(
                string.Join(" + ", unknownWeights.Select(value => value.ToString())),
                unknownWeights.Sum());
        }

        public ExpressionBalanceBoardState ReadVariableEquationState(
            IReadOnlyList<XRSocketInteractor> leftSockets,
            IReadOnlyList<XRSocketInteractor> rightSockets,
            int variableValue)
        {
            var leftHasVariable = ReadVariableSide(leftSockets, leftWeights);
            var rightHasVariable = ReadVariableSide(rightSockets, rightWeights);
            var leftWeightTotal = leftWeights.Sum();
            var rightWeightTotal = rightWeights.Sum();
            var leftTotal = (leftHasVariable ? variableValue : 0) + leftWeightTotal;
            var rightTotal = rightWeightTotal;
            var leftExpression = BuildVariableExpression(leftHasVariable, leftWeightTotal);
            var rightExpression = rightWeightTotal > 0 ? rightWeightTotal.ToString() : "?";
            var solvedValue = rightWeightTotal;
            var isSolved = leftHasVariable && !rightHasVariable && leftWeightTotal == 0 && solvedValue > 0;

            if (isSolved)
            {
                return new ExpressionBalanceBoardState(
                    $"x = {solvedValue}",
                    "せいかい！",
                    $"答えは {solvedValue}",
                    solvedValue,
                    rightTotal,
                    true);
            }

            return new ExpressionBalanceBoardState(
                $"{leftExpression} = {rightExpression}",
                "",
                "",
                leftTotal,
                rightTotal,
                false);
        }

        public float ReadSideTotal(
            BalanceSide side,
            IReadOnlyList<XRSocketInteractor> leftSockets,
            IReadOnlyList<XRSocketInteractor> rightSockets,
            int variableValue)
        {
            var sockets = side == BalanceSide.Left ? leftSockets : rightSockets;
            var total = 0f;

            if (sockets == null)
            {
                return total;
            }

            foreach (var socket in sockets)
            {
                if (socket == null || !socket.hasSelection)
                {
                    continue;
                }

                foreach (var interactable in socket.interactablesSelected)
                {
                    total += ReadInteractableWeight(interactable.transform, variableValue);
                }
            }

            return total;
        }

        private static void ReadWeights(IReadOnlyList<XRSocketInteractor> sockets, List<int> weights)
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

                foreach (var interactable in socket.interactablesSelected)
                {
                    var weight = interactable.transform.GetComponentInParent<WeightedDumbbell>();
                    if (weight == null)
                    {
                        continue;
                    }

                    var value = Mathf.RoundToInt(weight.Weight.Value);
                    if (value > 0)
                    {
                        weights.Add(value);
                    }
                }
            }
        }

        private static bool ReadVariableSide(IReadOnlyList<XRSocketInteractor> sockets, List<int> weights)
        {
            weights.Clear();
            var hasVariable = false;

            if (sockets == null)
            {
                return false;
            }

            foreach (var socket in sockets)
            {
                if (socket == null || !socket.hasSelection)
                {
                    continue;
                }

                foreach (var interactable in socket.interactablesSelected)
                {
                    var transform = interactable.transform;
                    var weight = transform.GetComponentInParent<WeightedDumbbell>();
                    if (weight != null)
                    {
                        var value = Mathf.RoundToInt(weight.Weight.Value);
                        if (value > 0)
                        {
                            weights.Add(value);
                        }

                        continue;
                    }

                    if (LooksLikeVariableTransform(transform))
                    {
                        hasVariable = true;
                    }
                }
            }

            weights.Sort();
            return hasVariable;
        }

        private static string BuildVariableExpression(bool hasVariable, int weightTotal)
        {
            if (hasVariable && weightTotal > 0)
            {
                return $"x + {weightTotal}";
            }

            if (hasVariable)
            {
                return "x";
            }

            return weightTotal > 0 ? weightTotal.ToString() : "?";
        }

        private static float ReadInteractableWeight(Transform interactableTransform, int variableValue)
        {
            if (interactableTransform == null)
            {
                return 0f;
            }

            var weight = interactableTransform.GetComponentInParent<WeightedDumbbell>();
            if (weight != null)
            {
                return weight.Weight.Value;
            }

            return LooksLikeVariableBox(interactableTransform.name) ? variableValue : 0f;
        }

        private static bool LooksLikeVariableTransform(Transform transform)
        {
            while (transform != null)
            {
                if (LooksLikeVariableBox(transform.name))
                {
                    return true;
                }

                transform = transform.parent;
            }

            return false;
        }

        private static bool LooksLikeVariableBox(string objectName)
        {
            return objectName.Equals("xbox", System.StringComparison.OrdinalIgnoreCase)
                   || objectName.Equals("x box", System.StringComparison.OrdinalIgnoreCase)
                   || objectName.Equals("x_box", System.StringComparison.OrdinalIgnoreCase)
                   || objectName.Contains("xbox", System.StringComparison.OrdinalIgnoreCase)
                   || objectName.Contains("x box", System.StringComparison.OrdinalIgnoreCase)
                   || objectName.Contains("x_box", System.StringComparison.OrdinalIgnoreCase)
                   || objectName.Contains("variable", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
