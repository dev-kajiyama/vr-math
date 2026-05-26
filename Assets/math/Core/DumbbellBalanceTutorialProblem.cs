namespace VrMath.Core
{
    /// <summary>
    /// ダンベルだけで均衡を作るチュートリアル問題を表します。
    /// 片側は固定の重さ、もう片側は学習者が作る未知の重さとして扱います。
    /// </summary>
    public readonly struct DumbbellBalanceTutorialProblem
    {
        /// <summary>
        /// 最初からダンベルを置いておく側です。
        /// </summary>
        public BalanceSide FixedSide { get; }

        /// <summary>
        /// 最初から置くダンベルの重さです。
        /// </summary>
        public int FixedWeight { get; }

        /// <summary>
        /// 学習者がダンベルを置いて作る側です。
        /// </summary>
        public BalanceSide UnknownSide => FixedSide == BalanceSide.Left ? BalanceSide.Right : BalanceSide.Left;

        /// <summary>
        /// 固定側と重さを指定して、チュートリアル問題を作ります。
        /// </summary>
        public DumbbellBalanceTutorialProblem(BalanceSide fixedSide, int fixedWeight)
        {
            if (fixedWeight < 1)
            {
                throw new System.ArgumentOutOfRangeException(nameof(fixedWeight), "固定する重さは 1 以上にしてください。");
            }

            FixedSide = fixedSide;
            FixedWeight = fixedWeight;
        }

        /// <summary>
        /// 未知側を ? で表した式を作ります。
        /// </summary>
        public string BuildQuestionEquation()
        {
            return FixedSide == BalanceSide.Left ? $"{FixedWeight} = ?" : $"? = {FixedWeight}";
        }

        /// <summary>
        /// 均衡できたあとの式を作ります。
        /// </summary>
        public string BuildSolvedEquation()
        {
            return $"{FixedWeight} = {FixedWeight}";
        }

        /// <summary>
        /// 均衡できたあとの式を、学習者が置いた分銅の内訳つきで作ります。
        /// </summary>
        public string BuildSolvedEquation(string unknownExpression)
        {
            var expression = string.IsNullOrWhiteSpace(unknownExpression) ? FixedWeight.ToString() : unknownExpression;
            return FixedSide == BalanceSide.Left ? $"{FixedWeight} = {expression}" : $"{expression} = {FixedWeight}";
        }

        /// <summary>
        /// 学習者が置いたダンベルの式を未知側へ反映した表示を作ります。
        /// まだ置かれていない場合は ? のまま残します。
        /// </summary>
        public string BuildProgressEquation(string unknownExpression)
        {
            return BuildProgressEquation(unknownExpression, -1);
        }

        /// <summary>
        /// 学習者が置いたダンベルの式を未知側へ反映し、未均衡なら不等号で表示します。
        /// </summary>
        public string BuildProgressEquation(string unknownExpression, int unknownWeight)
        {
            var expression = string.IsNullOrWhiteSpace(unknownExpression) ? "?" : unknownExpression;
            var comparison = unknownWeight < 0 ? "=" : GetComparisonSymbol(FixedSide, FixedWeight, unknownWeight);
            return FixedSide == BalanceSide.Left ? $"{FixedWeight} {comparison} {expression}" : $"{expression} {comparison} {FixedWeight}";
        }

        /// <summary>
        /// 学習者が作った重さが固定側の重さと等しいかを判定します。
        /// </summary>
        public bool IsBalanced(int unknownWeight)
        {
            return unknownWeight == FixedWeight;
        }

        private static string GetComparisonSymbol(BalanceSide fixedSide, int fixedWeight, int unknownWeight)
        {
            var leftWeight = fixedSide == BalanceSide.Left ? fixedWeight : unknownWeight;
            var rightWeight = fixedSide == BalanceSide.Left ? unknownWeight : fixedWeight;

            if (leftWeight > rightWeight)
            {
                return ">";
            }

            if (leftWeight < rightWeight)
            {
                return "<";
            }

            return "=";
        }
    }
}
