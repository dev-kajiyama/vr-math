namespace VrMath.Lesson
{
    /// <summary>
    /// 平均台上の現在状態から作った、カード表示と傾き計算に使う読み取り結果です。
    /// </summary>
    public readonly struct ExpressionBalanceBoardState
    {
        /// <summary>
        /// カード表示文字列、左右重量、正解状態をまとめます。
        /// </summary>
        public ExpressionBalanceBoardState(
            string problem,
            string operation,
            string answer,
            float leftTotal,
            float rightTotal,
            bool isSolved)
        {
            Problem = problem ?? "";
            Operation = operation ?? "";
            Answer = answer ?? "";
            LeftTotal = leftTotal;
            RightTotal = rightTotal;
            IsSolved = isSolved;
        }

        /// <summary>
        /// カード中央に表示する式です。
        /// </summary>
        public string Problem { get; }

        /// <summary>
        /// 正解時などに表示する補助操作文です。
        /// </summary>
        public string Operation { get; }

        /// <summary>
        /// 正解時などに表示する答えの文です。
        /// </summary>
        public string Answer { get; }

        /// <summary>
        /// 左辺として読み取った合計重量です。
        /// </summary>
        public float LeftTotal { get; }

        /// <summary>
        /// 右辺として読み取った合計重量です。
        /// </summary>
        public float RightTotal { get; }

        /// <summary>
        /// x だけが左辺に残り、右辺の値が答えとして確定しているかを表します。
        /// </summary>
        public bool IsSolved { get; }
    }
}
