namespace VrMath.Core
{
    /// <summary>
    /// 天びんの左右どちらの辺を表すかを示します。
    /// 式の左辺と右辺、実際の左皿と右皿を対応させるために使います。
    /// </summary>
    public enum BalanceSide
    {
        /// <summary>
        /// 左辺、または左皿です。
        /// </summary>
        Left = 0,

        /// <summary>
        /// 右辺、または右皿です。
        /// </summary>
        Right = 1
    }
}
