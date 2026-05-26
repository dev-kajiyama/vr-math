using UnityEngine;

namespace VrMath.Interaction
{
    /// <summary>
    /// 式ゲームの盤面で、このオブジェクトが何個分のソケット幅を占めるかを表します。
    /// 現在の x 箱は 1 ソケット分として扱います。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BalanceSocketFootprint : MonoBehaviour
    {
        [SerializeField, Min(1), Tooltip("占有するソケット数です。現在の x 箱は 1 にします。")]
        private int slotSpan = 1;

        public int SlotSpan
        {
            get => Mathf.Max(1, slotSpan);
            set => slotSpan = Mathf.Max(1, value);
        }
    }
}
