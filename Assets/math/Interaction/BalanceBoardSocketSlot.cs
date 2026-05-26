using UnityEngine;
using VrMath.Core;

namespace VrMath.Interaction
{
    /// <summary>
    /// 平均台ソケットの式ゲーム上の位置情報です。Index は見た目の左から右へ 0,1,2,3... の順で設定します。
    /// </summary>
    public sealed class BalanceBoardSocketSlot : MonoBehaviour
    {
        [SerializeField]
        private BalanceSide side;

        [SerializeField, Min(0)]
        private int index;

        public BalanceSide Side => side;
        public int Index => index;

        public void Initialize(BalanceSide side, int index)
        {
            this.side = side;
            this.index = Mathf.Max(0, index);
        }
    }
}
