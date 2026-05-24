namespace VrMath.Core
{
    /// <summary>
    /// レッスンカードに表示する文言を、Unity の UI 実装から独立して表します。
    /// Core 側で文言の意図を決め、Rendering 側はこの値を TextMeshPro に反映します。
    /// </summary>
    public readonly struct LessonCardContent
    {
        /// <summary>
        /// カードの見出しです。
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// 問題や現在の状態を表す主文です。
        /// </summary>
        public string Problem { get; }

        /// <summary>
        /// 学習者にしてほしい操作です。
        /// </summary>
        public string Operation { get; }

        /// <summary>
        /// 答えや確認結果です。空文字なら表示しません。
        /// </summary>
        public string Answer { get; }

        /// <summary>
        /// レッスンカードの 4 つの表示欄をまとめます。
        /// </summary>
        public LessonCardContent(string title, string problem, string operation, string answer)
        {
            Title = title ?? "";
            Problem = problem ?? "";
            Operation = operation ?? "";
            Answer = answer ?? "";
        }
    }
}
