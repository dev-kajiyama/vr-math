using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VrMath.Interaction;

namespace VrMath.Lesson
{
    /// <summary>
    /// Start ボタン専用の薄い入口です。クリックされたら x = 3 の式バランス問題を開始します。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class ExpressionBalanceStartButton : MonoBehaviour
    {
        [SerializeField, Tooltip("未設定ならシーン内から式バランス controller を探します。")]
        private SampleSceneExpressionBalanceBootstrap lessonController;

        [SerializeField, Min(1), Tooltip("Start 時に始める x の値です。")]
        private int answerValue = 3;

        [SerializeField, Min(0), Tooltip("x に足す数です。0 なら x = answer として始めます。")]
        private int equationOffset = 0;

        [SerializeField, Tooltip("Start 後にこのボタンを非表示にします。")]
        private bool hideAfterStart = true;

        private Button button;
        private XRButtonSelectProxy xrSelectProxy;

        private void Awake()
        {
            button = GetComponent<Button>();
            EnsureButtonHitTarget(button);
            EnsureXrSelectProxy();
        }

        private void OnEnable()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            button.onClick.RemoveListener(StartLesson);
            button.onClick.AddListener(StartLesson);

            EnsureButtonHitTarget(button);
            EnsureXrSelectProxy();
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(StartLesson);
            }

            if (xrSelectProxy != null)
            {
                xrSelectProxy.SetOverrideAction(null);
            }
        }

        public void StartLesson()
        {
            var controller = ResolveLessonController();
            if (controller == null)
            {
                Debug.LogWarning("[ExpressionBalance:Start] Could not find SampleSceneExpressionBalanceBootstrap.");
                return;
            }

            controller.BeginVariableEquationProblem(answerValue, equationOffset);

            if (hideAfterStart)
            {
                gameObject.SetActive(false);
            }
        }

        private SampleSceneExpressionBalanceBootstrap ResolveLessonController()
        {
            if (lessonController != null)
            {
                return lessonController;
            }

            lessonController = FindAnyObjectByType<SampleSceneExpressionBalanceBootstrap>(FindObjectsInactive.Include);
            if (lessonController != null)
            {
                return lessonController;
            }

            var controllerObject = new GameObject("SampleScene Expression Balance Game");
            lessonController = controllerObject.AddComponent<SampleSceneExpressionBalanceBootstrap>();
            return lessonController;
        }

        private void EnsureXrSelectProxy()
        {
            xrSelectProxy = GetComponent<XRButtonSelectProxy>();
            if (xrSelectProxy == null)
            {
                xrSelectProxy = gameObject.AddComponent<XRButtonSelectProxy>();
            }

            xrSelectProxy.SetOverrideAction(StartLesson);
            xrSelectProxy.RefreshCollider();
        }

        private static void EnsureButtonHitTarget(Button targetButton)
        {
            if (targetButton == null)
            {
                return;
            }

            targetButton.enabled = true;
            targetButton.interactable = true;

            var targetGraphic = targetButton.targetGraphic;
            if (targetGraphic == null)
            {
                targetGraphic = targetButton.GetComponent<Graphic>();
            }

            if (targetGraphic == null)
            {
                targetGraphic = targetButton.GetComponentsInChildren<Graphic>(true)
                    .FirstOrDefault(graphic => graphic is Image);
            }

            if (targetGraphic != null)
            {
                targetGraphic.enabled = true;
                targetGraphic.raycastTarget = true;
                targetButton.targetGraphic = targetGraphic;
            }

            foreach (var text in targetButton.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                text.raycastTarget = false;
            }
        }
    }
}
