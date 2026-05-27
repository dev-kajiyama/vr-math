namespace VrMath.Lesson
{
    public readonly struct ExpressionBalanceUnknownSideState
    {
        public ExpressionBalanceUnknownSideState(string expression, int total)
        {
            Expression = string.IsNullOrWhiteSpace(expression) ? "?" : expression;
            Total = total;
        }

        public string Expression { get; }
        public int Total { get; }
    }
}
