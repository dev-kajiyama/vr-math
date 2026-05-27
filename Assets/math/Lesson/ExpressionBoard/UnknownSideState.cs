namespace VrMath.Lesson
{
    /// <summary>
    /// 3 = ? ステージで、未知側に置かれた重りから作った式と合計です。
    /// </summary>
    public readonly struct ExpressionBalanceUnknownSideState
    {
        /// <summary>
        /// 表示用の式と、その合計値を保持します。
        /// </summary>
        public ExpressionBalanceUnknownSideState(string expression, int total)
        {
            Expression = string.IsNullOrWhiteSpace(expression) ? "?" : expression;
            Total = total;
        }

        /// <summary>
        /// 例: "1 + 1 + 1"。何も無い場合は "?" です。
        /// </summary>
        public string Expression { get; }

        /// <summary>
        /// 未知側に置かれている重りの合計値です。
        /// </summary>
        public int Total { get; }
    }
}
