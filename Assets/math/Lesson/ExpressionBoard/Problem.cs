namespace VrMath.Lesson
{
    /// <summary>
    /// x + offset = rightTotal で表す、1 問分の式バランス問題です。
    /// </summary>
    public readonly struct ExpressionBalanceProblem
    {
        /// <summary>
        /// 答えとオフセットを、ゲームで扱える最小値へ丸めて保持します。
        /// </summary>
        public ExpressionBalanceProblem(int answer, int offset)
        {
            Answer = System.Math.Max(1, answer);
            Offset = System.Math.Max(0, offset);
        }

        /// <summary>
        /// x に入る正解値です。
        /// </summary>
        public int Answer { get; }

        /// <summary>
        /// x に足されている重りの数です。
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// 右辺に置く必要がある重りの合計数です。
        /// </summary>
        public int RightTotal => Answer + Offset;
    }
}
