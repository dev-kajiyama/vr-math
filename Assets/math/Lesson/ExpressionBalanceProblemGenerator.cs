using System.Collections.Generic;
using UnityEngine;

namespace VrMath.Lesson
{
    public sealed class ExpressionBalanceProblemGenerator
    {
        public ExpressionBalanceProblem Generate(int answer, int offset)
        {
            return new ExpressionBalanceProblem(answer, offset);
        }

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
            var maxLeftOffset = Mathf.Max(0, leftSocketCount - 1);
            var maxRightTotal = Mathf.Max(1, rightSocketCount);
            var normalizedMinOffset = Mathf.Max(1, minOffset);
            var normalizedMaxOffset = Mathf.Min(maxOffset, maxLeftOffset, maxRightTotal - 1);
            var normalizedMinAnswer = Mathf.Max(1, minAnswer);
            var normalizedMaxAnswer = Mathf.Max(normalizedMinAnswer, maxAnswer);
            var candidates = new List<ExpressionBalanceProblem>();

            for (var offset = normalizedMinOffset; offset <= normalizedMaxOffset; offset++)
            {
                var maxAnswerForOffset = Mathf.Min(normalizedMaxAnswer, maxRightTotal - offset);
                for (var answer = normalizedMinAnswer; answer <= maxAnswerForOffset; answer++)
                {
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
                candidates.RemoveAll(candidate =>
                    candidate.Answer == previousProblem.Answer && candidate.Offset == previousProblem.Offset);
            }

            return candidates.Count == 0 ? fallbackProblem : candidates[Random.Range(0, candidates.Count)];
        }

        public string Format(ExpressionBalanceProblem problem)
        {
            return problem.Offset <= 0
                ? $"x = {problem.Answer}"
                : $"x + {problem.Offset} = {problem.RightTotal}";
        }
    }
}
