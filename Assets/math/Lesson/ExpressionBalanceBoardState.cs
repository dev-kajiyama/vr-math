namespace VrMath.Lesson
{
    public readonly struct ExpressionBalanceBoardState
    {
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

        public string Problem { get; }
        public string Operation { get; }
        public string Answer { get; }
        public float LeftTotal { get; }
        public float RightTotal { get; }
        public bool IsSolved { get; }
    }
}
