using UnityEngine;

namespace VrMath.Lesson
{
    /// <summary>
    /// 左右の重量差から平均台の見た目の傾きを更新します。
    /// </summary>
    public sealed class ExpressionBalanceTiltUpdater
    {
        /// <summary>
        /// 左右重量差を最大傾き角へ変換し、板のローカル回転をなめらかに追従させます。
        /// </summary>
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

            // 左が重いほど正、右が重いほど負。最大差を超える分は角度を打ち止めにする。
            var normalizedDifference = Mathf.Clamp((leftWeight - rightWeight) / maxWeightDifference, -1f, 1f);

            // 重量差 -1..1 を、板の最大傾き角度へ変換する。
            var targetTilt = normalizedDifference * maxTiltDegrees;

            // 初期ローカル回転を基準にして、Z 軸回転だけを足す。
            var targetRotation = boardBaseRotation * Quaternion.Euler(0f, 0f, targetTilt);

            // フレームレートに依存しにくい指数補間で、急にカクッと傾かないようにする。
            boardVisual.localRotation = Quaternion.Slerp(
                boardVisual.localRotation,
                targetRotation,
                1f - Mathf.Exp(-followSpeed * Time.deltaTime));
        }

        /// <summary>
        /// 平均台の見た目を、記録しておいた初期ローカル回転へ即座に戻します。
        /// </summary>
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
