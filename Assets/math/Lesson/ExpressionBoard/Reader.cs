using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VrMath.Core;

namespace VrMath.Lesson
{
    /// <summary>
    /// 平均台のソケットに実際に入っている x 箱と重りを読み取り、式表示や傾き計算用の値へ変換します。
    /// </summary>
    public sealed class ExpressionBalanceBoardReader
    {
        // 毎フレーム呼ばれるため、一時リストは使い回して GC を抑える。
        private readonly List<int> unknownWeights = new();
        private readonly List<int> leftWeights = new();
        private readonly List<int> rightWeights = new();

        /// <summary>
        /// 3 = ? ステージで、固定辺ではない側に置かれた重りを式文字列と合計値として読みます。
        /// </summary>
        public ExpressionBalanceUnknownSideState ReadUnknownSide(
            BalanceSide unknownSide,
            IReadOnlyList<XRSocketInteractor> leftSockets,
            IReadOnlyList<XRSocketInteractor> rightSockets)
        {
            // 今回読む側のソケットだけを対象にする。前回値は残さない。
            unknownWeights.Clear();
            var sockets = unknownSide == BalanceSide.Left ? leftSockets : rightSockets;
            ReadWeights(sockets, unknownWeights);

            // 何も置かれていない時は、カードには ? を表示する。
            if (unknownWeights.Count == 0)
            {
                return new ExpressionBalanceUnknownSideState("?", 0);
            }

            // 見た目の式が安定するよう、読み取った値を昇順にしてから "1 + 1 + 2" 形式へ整える。
            unknownWeights.Sort();
            return new ExpressionBalanceUnknownSideState(
                string.Join(" + ", unknownWeights.Select(value => value.ToString())),
                unknownWeights.Sum());
        }

        /// <summary>
        /// x + a = b ステージで、左右のソケット状態から現在表示すべき式と正解状態を読みます。
        /// </summary>
        public ExpressionBalanceBoardState ReadVariableEquationState(
            IReadOnlyList<XRSocketInteractor> leftSockets,
            IReadOnlyList<XRSocketInteractor> rightSockets,
            int variableValue)
        {
            // 左右それぞれについて、x 箱の有無と重り合計を独立して読む。
            var leftHasVariable = ReadVariableSide(leftSockets, leftWeights);
            var rightHasVariable = ReadVariableSide(rightSockets, rightWeights);
            var leftWeightTotal = leftWeights.Sum();
            var rightWeightTotal = rightWeights.Sum();

            // 傾き計算では x 箱を正解値の重さとして扱う。
            var leftTotal = (leftHasVariable ? variableValue : 0) + leftWeightTotal;
            var rightTotal = rightWeightTotal;

            // カード表示では、今ボード上に残っている項だけを式として組み立てる。
            var leftExpression = BuildVariableExpression(leftHasVariable, leftWeightTotal);
            var rightExpression = rightWeightTotal > 0 ? rightWeightTotal.ToString() : "?";
            var solvedValue = rightWeightTotal;

            // このレッスンでは「左に x だけ、右に正の重りだけ」が答えまで変形できた状態。
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

        /// <summary>
        /// 指定した辺に載っている重りと x 箱の合計重量を返します。
        /// </summary>
        public float ReadSideTotal(
            BalanceSide side,
            IReadOnlyList<XRSocketInteractor> leftSockets,
            IReadOnlyList<XRSocketInteractor> rightSockets,
            int variableValue)
        {
            // 傾き更新用なので、表示文字列ではなく数値だけを読む。
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
            // WeightedDumbbell だけを拾う。x 箱やその他のオブジェクトは 3 = ? では無視する。
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
            // 1 辺ぶんの読み取り結果を初期化してから、ソケット選択物を順に確認する。
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

                    // 重りなら値を足し、x 箱判定には進まない。
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

                    // 重りではない選択物のうち、名前から x 箱らしいものだけを変数として扱う。
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
            // 左辺に x と重りが残っている場合は "x + 2" のように表示する。
            if (hasVariable && weightTotal > 0)
            {
                return $"x + {weightTotal}";
            }

            // x だけなら "x"、重りだけなら数値、何もなければ ? にする。
            if (hasVariable)
            {
                return "x";
            }

            return weightTotal > 0 ? weightTotal.ToString() : "?";
        }

        private static float ReadInteractableWeight(Transform interactableTransform, int variableValue)
        {
            // 傾き計算用。重りは実値、x 箱は正解値、その他は 0 として扱う。
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
            // 子オブジェクトが選択される場合があるので、親階層まで名前を見る。
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
            // 既存シーンで名前が揺れても拾えるよう、代表的な命名を許可する。
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
