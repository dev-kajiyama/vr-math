using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VrMath.Interaction;

namespace VrMath.Lesson
{
    /// <summary>
    /// Gen ボタン専用の薄い入口です。クリックされたら問題文だけを生成します。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class ExpressionBalanceGenerateButton : MonoBehaviour
    {
        [SerializeField, Tooltip("未設定ならシーン内から式バランス controller を探します。")]
        private SampleSceneExpressionBalanceBootstrap lessonController;

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

            button.onClick.RemoveListener(GenerateProblemText);
            button.onClick.AddListener(GenerateProblemText);

            EnsureButtonHitTarget(button);
            EnsureXrSelectProxy();
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(GenerateProblemText);
            }

            if (xrSelectProxy != null)
            {
                xrSelectProxy.SetOverrideAction(null);
            }
        }

        public void GenerateProblemText()
        {
            var controller = ResolveLessonController();
            if (controller == null)
            {
                Debug.LogWarning("[ExpressionBalance:Gen] Could not find SampleSceneExpressionBalanceBootstrap.");
                return;
            }

            controller.GenerateProblemTextOnly();
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

            xrSelectProxy.SetOverrideAction(GenerateProblemText);
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
