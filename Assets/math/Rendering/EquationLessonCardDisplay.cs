using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VrMath.Core;

namespace VrMath.Rendering
{
    /// <summary>
    /// 天びん近くの浮遊カードに、現在の問題、操作、途中式、答えを表示します。
    /// </summary>
    public sealed class EquationLessonCardDisplay : MonoBehaviour
    {
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

        private void Awake()
        {
            AutoAssignTextReferences();
            AutoAssignStatusBackgrounds();
            EnsureNextButton();
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
            var problem = new DumbbellBalanceTutorialProblem(fixedSide, targetWeight);
            Show("", problem.BuildProgressEquation(placedExpression), "", "");
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
        /// Core が作ったカード文言を表示します。
        /// </summary>
        public void Show(LessonCardContent content)
        {
            Show(content.Title, content.Problem, content.Operation, content.Answer);
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

        private void AutoAssignTextReferences()
        {
            if (combinedText != null || titleText != null || problemText != null || operationText != null || answerText != null)
            {
                return;
            }

            var texts = GetComponentsInChildren<TMP_Text>(true);
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
                var nextTransform = transform.Find("NextButton");
                if (nextTransform != null)
                {
                    nextButton = nextTransform.GetComponent<Button>();
                }
            }

            if (nextButton == null)
            {
                nextButton = CreateNextButton();
            }

            nextButton.onClick.RemoveListener(ShowRandomProblem);
            nextButton.onClick.AddListener(ShowRandomProblem);
            nextButton.gameObject.SetActive(false);
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
                nextButton.gameObject.SetActive(isCorrect);
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
