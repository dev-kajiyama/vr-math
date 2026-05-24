namespace VrMath.Core
{
    /// <summary>
    /// ダンベルだけで天びんの等しさを体験する、最初のチュートリアル表示を決めます。
    /// この段階では未知数 x を出さず、「左右の重さをそろえる」感覚だけに集中させます。
    /// </summary>
    public sealed class DumbbellBalanceTutorialPolicy
    {
        private readonly DumbbellBalanceTutorialProblem problem;

        /// <summary>
        /// 左右でそろえる目標の重さを受け取って、チュートリアル方針を作ります。
        /// 目標値は 1 以上である必要があります。
        /// </summary>
        public DumbbellBalanceTutorialPolicy(int targetWeight)
            : this(BalanceSide.Left, targetWeight)
        {
        }

        /// <summary>
        /// 固定側と重さを受け取って、チュートリアル方針を作ります。
        /// </summary>
        public DumbbellBalanceTutorialPolicy(BalanceSide fixedSide, int targetWeight)
        {
            problem = new DumbbellBalanceTutorialProblem(fixedSide, targetWeight);
        }

        /// <summary>
        /// 学習者に最初に見せるカード文言を作ります。
        /// 左皿のダンベルは固定済みで、右皿に同じ重さを置くことを促します。
        /// </summary>
        public LessonCardContent BuildInitialCard()
        {
            return new LessonCardContent(
                "",
                problem.BuildQuestionEquation(),
                "",
                "");
        }

        /// <summary>
        /// 均衡できたときの成功表示を作ります。
        /// </summary>
        public LessonCardContent BuildCorrectCard()
        {
            return new LessonCardContent(
                "",
                problem.BuildSolvedEquation(),
                "",
                "");
        }
    }
}
