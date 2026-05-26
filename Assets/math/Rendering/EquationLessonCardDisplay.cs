using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VrMath.Core;
using VrMath.Interaction;

namespace VrMath.Rendering
{
    /// <summary>
    /// 天びん近くの浮遊カードに、現在の問題、操作、途中式、答えを表示します。
    /// </summary>
    public sealed class EquationLessonCardDisplay : MonoBehaviour
    {
        private const string PrimaryCombinedTextName = "Modal Text";

        [SerializeField, Tooltip("1つの TextMeshPro に全内容をまとめて表示したい場合に使います。")]
        private TMP_Text combinedText;

        [SerializeField, Tooltip("カードの見出しに使う TextMeshPro。")]
        private TMP_Text titleText;

        [SerializeField, Tooltip("問題文に使う TextMeshPro。")]
        private TMP_Text problemText;

        [SerializeField, Tooltip("式変形や操作説明に使う TextMeshPro。")]
        private TMP_Text operationText;

        [SerializeField, Tooltip("答えに使う TextMeshPro。")]
        private TMP_Text answerText;

        [Header("カード状態")]
        [SerializeField, Tooltip("式エリアの背景。未設定なら子から Mask Background を探します。")]
        private Image[] statusBackgrounds;

        [SerializeField, Tooltip("通常時の式エリア色。")]
        private Color normalBackgroundColor = Color.black;

        [SerializeField, Tooltip("正解時の式エリア色。")]
        private Color correctBackgroundColor = new(0.05f, 0.45f, 0.22f, 0.95f);

        [SerializeField, Tooltip("正解後に次へ進むボタン。未設定なら自動生成します。")]
        private Button nextButton;

        [SerializeField, Tooltip("平均台上の選択物をクリアするボタン。")]
        private Button clearButton;

        [SerializeField, Tooltip("平均台上の状態から式を生成するボタン。")]
        private Button generateButton;

        [SerializeField, Tooltip("現在の式から平均台上へ配置するボタン。")]
        private Button setButton;

        [SerializeField, Tooltip("次へ進むボタンに表示する文字。")]
        private string nextButtonLabel = "Next";

        [Header("初期表示")]
        [SerializeField, Tooltip("開始時は x の問題を出さず、ダンベル均衡チュートリアルを表示します。")]
        private bool startWithDumbbellTutorial = true;

        [SerializeField, Tooltip("チュートリアルで左皿に固定されている重さ。")]
        private int tutorialTargetWeight = 3;

        [SerializeField, Tooltip("チュートリアルで最初からダンベルを置いておく辺。")]
        private BalanceSide tutorialFixedSide = BalanceSide.Left;

        [SerializeField, Tooltip("レッスン名。")]
        private string title = "";

        [SerializeField, Tooltip("最初に表示する問題。")]
        private string problem = "3 = ?";

        [SerializeField, Tooltip("最初に表示する操作。")]
        private string operation = "";

        [SerializeField, Tooltip("最初に表示する答え。空なら隠します。")]
        private string answer = "";

        [SerializeField, Tooltip("開始時に初期表示を反映します。")]
        private bool applyOnStart = true;

        [SerializeField, Tooltip("x の式問題へ進むときに、ランダムな x + a = b の問題を作ります。")]
        private bool randomizeOnStart = true;

        [SerializeField, Tooltip("答え x の最小値。")]
        private int minAnswer = 2;

        [SerializeField, Tooltip("答え x の最大値。")]
        private int maxAnswer = 9;

        [SerializeField, Tooltip("両辺から引く数 a の最小値。")]
        private int minOffset = 1;

        [SerializeField, Tooltip("両辺から引く数 a の最大値。")]
        private int maxOffset = 5;

        private int currentAnswer;
        private int currentOffset;
        private UnityAction nextButtonAction;
        private UnityAction clearButtonAction;
        private UnityAction generateButtonAction;
        private UnityAction setButtonAction;
        private bool processButtonsEnabled;

        private void Awake()
        {
            AutoAssignTextReferences();
            AutoAssignStatusBackgrounds();
            EnsureNextButton();
            EnsureUiInputAvailable();
            SetCorrectState(false);
        }

        private void Start()
        {
            if (!applyOnStart)
            {
                return;
            }

            if (startWithDumbbellTutorial)
            {
                ShowDumbbellTutorial();
                return;
            }

            if (randomizeOnStart)
            {
                ShowRandomProblem();
                return;
            }

            Show(title, problem, operation, answer);
        }

        /// <summary>
        /// 問題、操作、答えをまとめて表示します。
        /// </summary>
        public void Show(string newTitle, string newProblem, string newOperation, string newAnswer)
        {
            SanitizeTextReferences();
            SetCorrectState(false);

            if (combinedText != null)
            {
                combinedText.text = BuildCombinedText(newTitle, newProblem, newOperation, newAnswer);
                combinedText.gameObject.SetActive(true);
            }

            SetText(titleText, newTitle);
            SetText(problemText, newProblem);
            SetText(operationText, newOperation);
            SetText(answerText, newAnswer);
        }

        /// <summary>
        /// 最初の教材として、ダンベルだけで左右の重さをそろえる表示にします。
        /// </summary>
        public void ShowDumbbellTutorial()
        {
            ShowDumbbellTutorial(tutorialFixedSide, tutorialTargetWeight);
        }

        /// <summary>
        /// 指定した固定辺と重さで、ダンベルだけのチュートリアル表示にします。
        /// </summary>
        public void ShowDumbbellTutorial(BalanceSide fixedSide, int targetWeight)
        {
            var policy = new DumbbellBalanceTutorialPolicy(fixedSide, targetWeight);
            Show(policy.BuildInitialCard());
        }

        /// <summary>
        /// 学習者が置いたダンベルを式へ反映した表示にします。
        /// </summary>
        public void ShowDumbbellTutorialProgress(BalanceSide fixedSide, int targetWeight, string placedExpression)
        {
            ShowDumbbellTutorialProgress(fixedSide, targetWeight, placedExpression, -1);
        }

        /// <summary>
        /// 学習者が置いたダンベルを式へ反映し、未均衡なら不等号で表示します。
        /// </summary>
        public void ShowDumbbellTutorialProgress(BalanceSide fixedSide, int targetWeight, string placedExpression, int placedWeight)
        {
            var problem = new DumbbellBalanceTutorialProblem(fixedSide, targetWeight);
            Show("", problem.BuildProgressEquation(placedExpression, placedWeight), "", "");
        }

        /// <summary>
        /// ダンベル均衡チュートリアルを正解表示にします。
        /// </summary>
        public void ShowDumbbellTutorialCorrect()
        {
            var policy = new DumbbellBalanceTutorialPolicy(tutorialFixedSide, tutorialTargetWeight);
            Show(policy.BuildCorrectCard());
            SetCorrectState(true);
        }

        /// <summary>
        /// 指定した固定辺と重さで、ダンベル均衡チュートリアルの成功表示にします。
        /// </summary>
        public void ShowDumbbellTutorialCorrect(BalanceSide fixedSide, int targetWeight)
        {
            var policy = new DumbbellBalanceTutorialPolicy(fixedSide, targetWeight);
            Show(policy.BuildCorrectCard());
            SetCorrectState(true);
        }

        /// <summary>
        /// 指定した固定辺と重さで、置いた分銅の内訳つきの成功表示にします。
        /// </summary>
        public void ShowDumbbellTutorialCorrect(BalanceSide fixedSide, int targetWeight, string placedExpression)
        {
            var policy = new DumbbellBalanceTutorialPolicy(fixedSide, targetWeight);
            Show(policy.BuildCorrectCard(placedExpression));
            SetCorrectState(true);
        }

        /// <summary>
        /// Core が作ったカード文言を表示します。
        /// </summary>
        public void Show(LessonCardContent content)
        {
            Show(content.Title, content.Problem, content.Operation, content.Answer);
        }

        /// <summary>
        /// 外部のレッスン制御から Next ボタンの遷移先を差し替えます。
        /// </summary>
        public void SetNextButtonAction(UnityAction action)
        {
            nextButtonAction = action;
            EnsureNextButton();
            ConfigureNextButtonInteraction();
        }

        /// <summary>
        /// デバッグしやすいよう、式ゲームの処理を Clear / Gen / Set / Next に分けて接続します。
        /// </summary>
        public void SetProcessButtonActions(UnityAction clearAction, UnityAction generateAction, UnityAction setAction, UnityAction nextAction)
        {
            clearButtonAction = clearAction;
            generateButtonAction = generateAction;
            setButtonAction = setAction;
            nextButtonAction = nextAction;
            processButtonsEnabled = true;

            EnsureNextButton();
            EnsureProcessButtons();
            ConfigureProcessButton(clearButton, clearButtonAction, "Clear");
            ConfigureProcessButton(generateButton, generateButtonAction, "Gen");
            ConfigureProcessButton(setButton, setButtonAction, "Set");
            ConfigureNextButtonInteraction();
            SetProcessButtonsVisible(true);
        }

        /// <summary>
        /// ランダムな x + a = b の問題を作って表示します。
        /// </summary>
        public void ShowRandomProblem()
        {
            currentAnswer = 8;
            currentOffset = 3;
            var rightSide = currentAnswer + currentOffset;

            Show(
                title,
                $"x + {currentOffset} = {rightSide}",
                "",
                "");
        }

        /// <summary>
        /// レッスン開始時の表示に戻します。
        /// </summary>
        public void ShowProblem()
        {
            Show(title, problem, operation, "");
        }

        /// <summary>
        /// 同じ数を両辺から引く段階の表示にします。
        /// </summary>
        public void ShowSubtractStep()
        {
            var rightSide = currentAnswer + currentOffset;
            Show(title, $"x + {currentOffset} = {rightSide}", $"-{currentOffset}     -{currentOffset}", $"x = {currentAnswer}");
        }

        /// <summary>
        /// 正解確認の表示にします。
        /// </summary>
        public void ShowCorrect()
        {
            Show(title, $"x = {currentAnswer}", "せいかい！", $"答えは {currentAnswer}");
            SetCorrectState(true);
        }

        public void ShowEquationCorrect(string solvedEquation, string solvedAnswer)
        {
            Show("", solvedEquation, "せいかい！", solvedAnswer);
            SetCorrectState(true);
        }

        private void AutoAssignTextReferences()
        {
            SanitizeTextReferences();
            var primaryText = FindEquationTextByName(PrimaryCombinedTextName);
            if (primaryText != null)
            {
                combinedText = primaryText;
                titleText = null;
                problemText = null;
                operationText = null;
                answerText = null;
                return;
            }

            if (combinedText != null || titleText != null || problemText != null || operationText != null || answerText != null)
            {
                return;
            }

            var texts = GetComponentsInChildren<TMP_Text>(true)
                .Where(IsEquationText)
                .ToArray();
            if (texts.Length == 1)
            {
                combinedText = texts[0];
                return;
            }

            if (texts.Length > 0) titleText = texts[0];
            if (texts.Length > 1) problemText = texts[1];
            if (texts.Length > 2) operationText = texts[2];
            if (texts.Length > 3) answerText = texts[3];
        }

        private void SanitizeTextReferences()
        {
            if (!IsEquationText(combinedText)) combinedText = null;
            if (!IsEquationText(titleText)) titleText = null;
            if (!IsEquationText(problemText)) problemText = null;
            if (!IsEquationText(operationText)) operationText = null;
            if (!IsEquationText(answerText)) answerText = null;
        }

        private static bool IsEquationText(TMP_Text text)
        {
            return text != null && text.GetComponentInParent<Button>(true) == null;
        }

        private TMP_Text FindEquationTextByName(string objectName)
        {
            return GetComponentsInChildren<TMP_Text>(true)
                .Where(IsEquationText)
                .FirstOrDefault(text => string.Equals(text.name, objectName, System.StringComparison.OrdinalIgnoreCase));
        }

        private void AutoAssignStatusBackgrounds()
        {
            if (statusBackgrounds != null && statusBackgrounds.Length > 0)
            {
                return;
            }

            var images = GetComponentsInChildren<Image>(true);
            var backgrounds = new System.Collections.Generic.List<Image>();
            foreach (var image in images)
            {
                if (image != null && image.name == "Mask Background")
                {
                    backgrounds.Add(image);
                }
            }

            statusBackgrounds = backgrounds.ToArray();
        }

        private void EnsureNextButton()
        {
            if (nextButton == null)
            {
                nextButton = FindExistingNextButton();
            }

            if (nextButton == null)
            {
                nextButton = CreateNextButton();
            }

            ConfigureNextButtonInteraction();

            nextButton.gameObject.SetActive(false);
        }

        private Button FindExistingNextButton()
        {
            var namedNextButton = FindButtonByNames("Next", "NextButton");
            if (namedNextButton != null)
            {
                return namedNextButton;
            }

            var specialButton = FindDirectChildButton("Text Poke Button Special");
            if (specialButton != null)
            {
                return specialButton;
            }

            return null;
        }

        private Button FindDirectChildButton(string childName)
        {
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name != childName)
                {
                    continue;
                }

                var button = child.GetComponent<Button>();
                if (button != null)
                {
                    return button;
                }
            }

            return null;
        }

        private Button FindButtonByNames(params string[] names)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var name in names)
            {
                foreach (var button in buttons)
                {
                    if (button != null && string.Equals(button.name, name, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return button;
                    }
                }
            }

            return FindSceneButtonByNames(names);
        }

        private Button FindSceneButtonByNames(params string[] names)
        {
            var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            return buttons
                .Where(button => button != null && names.Any(name => string.Equals(button.name, name, System.StringComparison.OrdinalIgnoreCase)))
                .OrderBy(button => Vector3.SqrMagnitude(button.transform.position - transform.position))
                .FirstOrDefault();
        }

        private void EnsureProcessButtons()
        {
            if (clearButton == null)
            {
                clearButton = FindButtonByNames("Clear", "ClearButton");
                if (clearButton == null)
                {
                    clearButton = CreateProcessButton("ClearButton", "Clear");
                    LayoutProcessButton(clearButton, 0, "Clear");
                }
            }

            if (generateButton == null)
            {
                generateButton = FindButtonByNames("Gen", "Generate", "GenerateButton");
                if (generateButton == null)
                {
                    generateButton = CreateProcessButton("GenerateButton", "Gen");
                    LayoutProcessButton(generateButton, 1, "Gen");
                }
            }

            if (setButton == null)
            {
                setButton = FindButtonByNames("Set", "SetButton");
                if (setButton == null)
                {
                    setButton = CreateProcessButton("SetButton", "Set");
                    LayoutProcessButton(setButton, 2, "Set");
                }
            }
        }

        private Button CreateProcessButton(string objectName, string labelText)
        {
            var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(transform, false);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.08f, 0.45f, 0.85f, 0.95f);
            image.raycastTarget = true;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = labelText;
            label.fontSize = 34f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            return buttonObject.GetComponent<Button>();
        }

        private void LayoutProcessButton(Button button, int index, string label)
        {
            if (button == null)
            {
                return;
            }

            button.name = index switch
            {
                0 => "ClearButton",
                1 => "GenerateButton",
                2 => "SetButton",
                _ => button.name
            };

            var rectTransform = button.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(-330f + index * 220f, 28f);
            rectTransform.sizeDelta = new Vector2(190f, 66f);

        }

        private void ConfigureProcessButton(Button button, UnityAction action, string label)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            if (action != null)
            {
                button.onClick.AddListener(action);
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.enabled = true;
                image.raycastTarget = true;
            }

            var proxy = button.GetComponent<XRButtonSelectProxy>();
            if (proxy == null)
            {
                proxy = button.gameObject.AddComponent<XRButtonSelectProxy>();
            }

            proxy.SetOverrideAction(action);
            proxy.RefreshCollider();
        }

        private Button CreateNextButton()
        {
            var buttonObject = new GameObject("NextButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(transform, false);

            var rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(1f, 0f);
            rectTransform.anchoredPosition = new Vector2(-40f, 28f);
            rectTransform.sizeDelta = new Vector2(220f, 72f);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.08f, 0.62f, 0.32f, 0.95f);
            image.raycastTarget = true;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonObject.transform, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = nextButtonLabel;
            label.fontSize = 42f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            return buttonObject.GetComponent<Button>();
        }

        private void ConfigureNextButtonInteraction()
        {
            if (nextButton == null)
            {
                return;
            }

            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(InvokeNextButtonAction);

            var image = nextButton.GetComponent<Image>();
            if (image != null)
            {
                image.enabled = true;
                image.raycastTarget = true;
            }

            var proxy = nextButton.GetComponent<XRButtonSelectProxy>();
            if (proxy == null)
            {
                proxy = nextButton.gameObject.AddComponent<XRButtonSelectProxy>();
            }

            proxy.SetOverrideAction(InvokeNextButtonAction);
            proxy.RefreshCollider();
        }

        private void InvokeNextButtonAction()
        {
            if (nextButtonAction != null)
            {
                nextButtonAction.Invoke();
                return;
            }

            ShowRandomProblem();
        }

        private void SetProcessButtonsVisible(bool visible)
        {
            if (clearButton != null)
            {
                clearButton.gameObject.SetActive(visible);
            }

            if (generateButton != null)
            {
                generateButton.gameObject.SetActive(visible);
            }

            if (setButton != null)
            {
                setButton.gameObject.SetActive(visible);
            }

            if (nextButton != null && processButtonsEnabled)
            {
                nextButton.gameObject.SetActive(visible);
            }
        }

        private void EnsureUiInputAvailable()
        {
            EnsureCanvasRaycasters();
            EnsureEventSystem();
        }

        private void EnsureCanvasRaycasters()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            canvas.enabled = true;

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            }
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                foreach (var candidate in FindObjectsByType<EventSystem>(FindObjectsInactive.Include))
                {
                    eventSystem = candidate;
                    break;
                }
            }

            if (eventSystem == null)
            {
                var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(XRUIInputModule));
                eventSystemObject.SetActive(true);
                return;
            }

            if (!eventSystem.gameObject.activeSelf)
            {
                eventSystem.gameObject.SetActive(true);
            }

            eventSystem.enabled = true;

            var hasEnabledInputModule = false;
            foreach (var inputModule in eventSystem.GetComponents<BaseInputModule>())
            {
                if (inputModule == null)
                {
                    continue;
                }

                inputModule.enabled = true;
                hasEnabledInputModule = true;
            }

            if (!hasEnabledInputModule)
            {
                eventSystem.gameObject.AddComponent<XRUIInputModule>();
            }
        }

        private void SetCorrectState(bool isCorrect)
        {
            var color = isCorrect ? correctBackgroundColor : normalBackgroundColor;
            if (statusBackgrounds != null)
            {
                foreach (var background in statusBackgrounds)
                {
                    if (background != null)
                    {
                        background.color = color;
                    }
                }
            }

            if (nextButton != null)
            {
                if (isCorrect || processButtonsEnabled)
                {
                    nextButton.transform.SetAsLastSibling();
                    ConfigureNextButtonInteraction();
                }

                nextButton.gameObject.SetActive(processButtonsEnabled || isCorrect);
            }

            if (processButtonsEnabled)
            {
                SetProcessButtonsVisible(true);
            }
        }

        private static string BuildCombinedText(string newTitle, string newProblem, string newOperation, string newAnswer)
        {
            var text = string.IsNullOrWhiteSpace(newTitle) ? newProblem : $"<b>{newTitle}</b>\n\n{newProblem}";

            if (!string.IsNullOrWhiteSpace(newOperation))
            {
                text += $"\n\n{newOperation}";
            }

            if (!string.IsNullOrWhiteSpace(newAnswer))
            {
                text += $"\n\n<b>{newAnswer}</b>";
            }

            return text;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target == null)
            {
                return;
            }

            target.text = value;
            target.gameObject.SetActive(!string.IsNullOrWhiteSpace(value));
        }
    }
}
