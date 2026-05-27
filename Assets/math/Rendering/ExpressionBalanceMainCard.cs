using UnityEngine;

namespace VrMath.Rendering
{
    /// <summary>
    /// 式バランスゲームが操作対象にするメインカードを明示するマーカーです。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EquationLessonCardDisplay))]
    public sealed class ExpressionBalanceMainCard : MonoBehaviour
    {
        private EquationLessonCardDisplay display;

        public EquationLessonCardDisplay Display
        {
            get
            {
                if (display == null)
                {
                    display = GetComponent<EquationLessonCardDisplay>();
                }

                return display;
            }
        }
    }
}
