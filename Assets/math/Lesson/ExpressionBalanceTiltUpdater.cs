using UnityEngine;

namespace VrMath.Lesson
{
    public sealed class ExpressionBalanceTiltUpdater
    {
        public void Update(
            Transform boardVisual,
            Quaternion boardBaseRotation,
            float leftWeight,
            float rightWeight,
            float maxWeightDifference,
            float maxTiltDegrees,
            float followSpeed)
        {
            if (boardVisual == null)
            {
                return;
            }

            var normalizedDifference = Mathf.Clamp((leftWeight - rightWeight) / maxWeightDifference, -1f, 1f);
            var targetTilt = normalizedDifference * maxTiltDegrees;
            var targetRotation = boardBaseRotation * Quaternion.Euler(0f, 0f, targetTilt);

            boardVisual.localRotation = Quaternion.Slerp(
                boardVisual.localRotation,
                targetRotation,
                1f - Mathf.Exp(-followSpeed * Time.deltaTime));
        }

        public void Reset(Transform boardVisual, Quaternion boardBaseRotation)
        {
            if (boardVisual == null)
            {
                return;
            }

            boardVisual.localRotation = boardBaseRotation;
        }
    }
}
