namespace VrMath.Lesson
{
    public readonly struct ExpressionBalanceProblem
    {
        public ExpressionBalanceProblem(int answer, int offset)
        {
            Answer = System.Math.Max(1, answer);
            Offset = System.Math.Max(0, offset);
        }

        public int Answer { get; }
        public int Offset { get; }
        public int RightTotal => Answer + Offset;
    }
}
