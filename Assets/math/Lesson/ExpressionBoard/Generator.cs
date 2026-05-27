using System.Collections.Generic;
using UnityEngine;

namespace VrMath.Lesson
{
    /// <summary>
    /// 式バランス問題の値決定と、カード表示用の式文字列生成を担当します。
    /// </summary>
    public sealed class ExpressionBalanceProblemGenerator
    {
        /// <summary>
        /// 指定された answer / offset から、1 問分の問題を作ります。
        /// </summary>
        public ExpressionBalanceProblem Generate(int answer, int offset)
        {
            // 値の丸めは ExpressionBalanceProblem 側に寄せている。
            return new ExpressionBalanceProblem(answer, offset);
        }

        /// <summary>
        /// ソケット数と重り数に収まる範囲で、前問と重複しにくいランダム問題を作ります。
        /// </summary>
        public ExpressionBalanceProblem GenerateRandom(
            int minAnswer,
            int maxAnswer,
            int minOffset,
            int maxOffset,
            int leftSocketCount,
            int rightSocketCount,
            int availableWeightCount,
            ExpressionBalanceProblem previousProblem,
            ExpressionBalanceProblem fallbackProblem)
        {
            // 左辺は x 用に 1 ソケット使うので、offset に使える最大数は leftSocketCount - 1。
            var maxLeftOffset = Mathf.Max(0, leftSocketCount - 1);

            // 右辺には answer + offset 個の重りを置くため、右ソケット数が右辺合計の上限になる。
            var maxRightTotal = Mathf.Max(1, rightSocketCount);

            // ランダム範囲の下限/上限を、実際に配置可能な範囲へ丸める。
            var normalizedMinOffset = Mathf.Max(1, minOffset);
            var normalizedMaxOffset = Mathf.Min(maxOffset, maxLeftOffset, maxRightTotal - 1);
            var normalizedMinAnswer = Mathf.Max(1, minAnswer);
            var normalizedMaxAnswer = Mathf.Max(normalizedMinAnswer, maxAnswer);
            var candidates = new List<ExpressionBalanceProblem>();

            // 配置可能な answer / offset の組み合わせを総当たりで候補化する。
            for (var offset = normalizedMinOffset; offset <= normalizedMaxOffset; offset++)
            {
                // answer + offset が右ソケット数を超えないよう、offset ごとに answer 上限を下げる。
                var maxAnswerForOffset = Mathf.Min(normalizedMaxAnswer, maxRightTotal - offset);
                for (var answer = normalizedMinAnswer; answer <= maxAnswerForOffset; answer++)
                {
                    // 左辺 offset 個 + 右辺 answer + offset 個の重りが必要。
                    var neededWeightCount = answer + offset * 2;
                    if (neededWeightCount > availableWeightCount)
                    {
                        continue;
                    }

                    candidates.Add(new ExpressionBalanceProblem(answer, offset));
                }
            }

            if (candidates.Count > 1)
            {
                // 候補が複数あるなら、直前と同じ問題は避ける。
                candidates.RemoveAll(candidate =>
                    candidate.Answer == previousProblem.Answer && candidate.Offset == previousProblem.Offset);
            }

            // 候補が無い場合は、固定問題など呼び出し元が渡した fallback へ戻す。
            return candidates.Count == 0 ? fallbackProblem : candidates[Random.Range(0, candidates.Count)];
        }

        /// <summary>
        /// 問題をカードに表示する式文字列へ変換します。
        /// </summary>
        public string Format(ExpressionBalanceProblem problem)
        {
            // offset 0 の時だけ、足し算なしの x = answer として見せる。
            return problem.Offset <= 0
                ? $"x = {problem.Answer}"
                : $"x + {problem.Offset} = {problem.RightTotal}";
        }
    }
}
