using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VrMath.Core;
using VrMath.Interaction;
using VrMath.Rendering;

namespace VrMath.Lesson
{
    /// <summary>
    /// SampleScene 1 に置かれている平均台と分銅を、式のバランスゲームとして自動接続します。
    /// </summary>
    public sealed class SampleSceneExpressionBalanceBootstrap : MonoBehaviour
    {
        private const string BootstrapObjectName = "SampleScene Expression Balance Game";
        private const int FixedVariableAnswer = 1;
        private const int FixedVariableOffset = 2;
        private const string SetDebugPrefix = "[ExpressionBalance:Set]";
        private const string SocketDebugPrefix = "[ExpressionBalance:Socket]";

        private enum LessonStage
        {
            // 最初の問題。左に 3 個の重りを置き、右に同じ重さを置かせる。
            BalanceOnly,

            // Next 後の問題。x + 2 = 3 を平均台上の配置で表現する。
            VariableExpression
        }

        /// <summary>
        /// 現在読み込まれているシーンが対象なら、平均台ゲームのブートストラップを開始します。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapCurrentScene()
        {
            TryBootstrap(SceneManager.GetActiveScene());
        }

        /// <summary>
        /// シーン遷移後にも対象シーンへ自動接続できるよう、sceneLoaded イベントを登録します。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneLoaded()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>
        /// シーン読み込み完了時に、対象シーンならブートストラップを試行します。
        /// </summary>
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryBootstrap(scene);
        }

        /// <summary>
        /// 式バランスゲーム用の平均台とカードがあるシーンに、管理用 GameObject を一度だけ作成します。
        /// </summary>
        private static void TryBootstrap(Scene scene)
        {
            if (!ShouldBootstrapScene(scene))
            {
                return;
            }

            if (GameObject.Find(BootstrapObjectName) != null)
            {
                return;
            }

            new GameObject(BootstrapObjectName).AddComponent<SampleSceneExpressionBalanceBootstrap>();
        }

        /// <summary>
        /// リネーム後のシーンでも、必要な構成があれば式バランスゲームとして起動できるか判定します。
        /// </summary>
        private static bool ShouldBootstrapScene(Scene scene)
        {
            if (!scene.IsValid())
            {
                return false;
            }

            if (scene.name == "SampleScene 1")
            {
                return true;
            }

            var boardObject = GameObject.Find("BalanceBoard_Simple");
            if (boardObject == null)
            {
                return false;
            }

            return FindAnyObjectByType<ExpressionBalanceMainCard>(FindObjectsInactive.Include) != null
                   || FindObjectsByType<EquationLessonCardDisplay>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                       .Any(display => display != null && display.name == "CoachingCardRoot");
        }

        [SerializeField, Min(1), Tooltip("x の正解値です。x + offset = rightSide の x にあたります。")]
        private int answerValue = 1;

        [SerializeField, Min(1), Tooltip("最初の x を使わない均衡問題の目標値です。")]
        private int balanceOnlyTargetWeight = 3;

        [SerializeField, Min(0), Tooltip("x に足されている数です。")]
        private int equationOffset = 2;

        [SerializeField, Min(1), Tooltip("3問目以降にランダム生成する x の最小値です。")]
        private int randomMinAnswer = 1;

        [SerializeField, Min(1), Tooltip("3問目以降にランダム生成する x の最大値です。")]
        private int randomMaxAnswer = 3;

        [SerializeField, Min(1), Tooltip("3問目以降にランダム生成する x に足す数の最小値です。")]
        private int randomMinOffset = 1;

        [SerializeField, Min(1), Tooltip("3問目以降にランダム生成する x に足す数の最大値です。")]
        private int randomMaxOffset = 3;

        [SerializeField, Tooltip("目標値を置く辺です。")]
        private BalanceSide fixedSide = BalanceSide.Left;

        [SerializeField, Tooltip("平均台ルート名です。未設定でも BalanceBoard_Simple を探します。")]
        private string balanceBoardName = "BalanceBoard_Simple";

        [SerializeField, Tooltip("実際に傾ける板オブジェクト名です。")]
        private string boardVisualName = "Board";

        [SerializeField, Min(0.05f), Tooltip("平均台中央から左右を判定し始める距離です。")]
        private float sideDeadZone = 0.15f;

        [SerializeField, Min(0.1f), Tooltip("この半幅の外側にある分銅は集計しません。")]
        private float boardHalfWidth = 3.6f;

        [SerializeField, Range(1f, 25f), Tooltip("最大傾き角度です。")]
        private float maxTiltDegrees = 12f;

        [SerializeField, Min(0.01f), Tooltip("傾きが最大になる左右差です。")]
        private float maxWeightDifference = 3f;

        [SerializeField, Min(0.01f), Tooltip("傾き追従速度です。")]
        private float tiltFollowSpeed = 8f;

        private Transform boardRoot;
        private Transform boardVisual;
        private ExpressionBalanceMainCard mainCardMarker;
        private EquationLessonCardDisplay cardDisplay;
        private readonly List<XRSocketInteractor> leftSockets = new();
        private readonly List<XRSocketInteractor> rightSockets = new();
        private readonly ExpressionBalanceProblemGenerator problemGenerator = new();
        private readonly ExpressionBalanceSocketLayoutPlanner socketLayoutPlanner = new();
        private readonly ExpressionBalanceSocketPlacer socketPlacer = new();
        private readonly ExpressionBalanceBoardReader boardReader = new();
        private readonly ExpressionBalanceBoardClearer boardClearer = new();
        private readonly ExpressionBalanceTiltUpdater tiltUpdater = new();
        private LessonStage lessonStage = LessonStage.BalanceOnly;
        private string lastExpression = "";
        private int lastTotal = -1;
        private LessonStage lastShownStage;
        private Quaternion boardBaseRotation;
        private bool weightInteractablesPrepared;
        private bool initialFixedWeightsPlaced;
        private bool variableExpressionObjectsPlaced;
        private int variableProblemIndex;
        private int lastAdvanceFrame = -1;
        private bool showConfiguredVariableEquation;
        private bool lessonReset;
        private EquationLessonCardDisplay subscribedCardDisplay;
        private readonly HashSet<XRSocketInteractor> socketsWithDebugLogging = new();

        /// <summary>
        /// 起動直後にシーン参照を解決し、最初のカード表示を出します。
        /// </summary>
        private void Awake()
        {
            ResolveSceneReferences();
            ShowInitialCard();
        }

        /// <summary>
        /// 毎フレーム、参照解決、カード表示、平均台の傾きを現在状態へ同期します。
        /// </summary>
        private void Update()
        {
            // Unity シーン上の手作業変更にも追従できるよう、参照解決と配置試行を毎フレーム軽く確認する。
            ResolveSceneReferences();

            if (lessonReset)
            {
                return;
            }

            var unknownTotal = ReadUnknownSide(out var expression);
            UpdateCard(expression, unknownTotal);
            UpdateBoardTilt(unknownTotal);
        }

        /// <summary>
        /// 平均台、板、カード、ソケット、重りなど、式ゲームに必要なシーン要素を解決して準備します。
        /// </summary>
        private void ResolveSceneReferences()
        {
            // 平均台、カード、ソケット、重りの XR 設定をここでまとめて接続する。
            // 配置処理は各ステージにつき一度だけ実行されるようにフラグで止めている。
            if (boardRoot == null)
            {
                var rootObject = GameObject.Find(balanceBoardName);
                boardRoot = rootObject != null ? rootObject.transform : FindObjectByNamePart("BalanceBoard");
            }

            if (boardVisual == null && boardRoot != null)
            {
                var child = FindChildRecursive(boardRoot, boardVisualName);
                boardVisual = child != null ? child : boardRoot;
                boardBaseRotation = boardVisual.localRotation;
            }

            ResolveStaticSockets();

            if (mainCardMarker == null || cardDisplay == null)
            {
                mainCardMarker = FindMainEquationCardMarker();
                cardDisplay = mainCardMarker != null ? mainCardMarker.Display : FindMainEquationCardDisplayFallback();
            }

            DisableNonMainCardRaycasts(cardDisplay);

            if (cardDisplay != null && subscribedCardDisplay != cardDisplay)
            {
                cardDisplay.SetProcessButtonActions(
                    ResetExpressionBalanceLesson,
                    GenerateProblemTextOnly,
                    PlaceCurrentProblemObjectsOnBoard,
                    AdvanceLessonAfterSuccess);
                subscribedCardDisplay = cardDisplay;
            }

            AutoAssignExpressionFootprints();
            PrepareWeightInteractablesForSockets();
            if (!lessonReset)
            {
                TryPlaceInitialFixedWeights();
            }
        }

        /// <summary>
        /// 式表示とボタン入力を受け持つメインカード marker を探します。
        /// </summary>
        private static ExpressionBalanceMainCard FindMainEquationCardMarker()
        {
            var markers = FindObjectsByType<ExpressionBalanceMainCard>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(marker => marker != null && marker.Display != null)
                .ToList();

            if (markers.Count == 0)
            {
                return null;
            }

            var activeMarker = markers.FirstOrDefault(marker => marker.gameObject.activeInHierarchy);
            return activeMarker != null ? activeMarker : markers[0];
        }

        /// <summary>
        /// 古いシーンで marker がまだ無い場合の移行用 fallback です。基本は ExpressionBalanceMainCard を付けます。
        /// </summary>
        private static EquationLessonCardDisplay FindMainEquationCardDisplayFallback()
        {
            var displays = FindObjectsByType<EquationLessonCardDisplay>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(display => display != null && !IsSuccessOnlyCardDisplay(display))
                .ToList();

            if (displays.Count == 0)
            {
                return null;
            }

            var exactCoachingRoot = displays.FirstOrDefault(display => display.name == "CoachingCardRoot" && display.gameObject.activeInHierarchy);
            if (exactCoachingRoot != null)
            {
                return exactCoachingRoot;
            }

            var activeDisplay = displays.FirstOrDefault(display => display.gameObject.activeInHierarchy);
            return activeDisplay != null ? activeDisplay : displays[0];
        }

        private static void DisableNonMainCardRaycasts(EquationLessonCardDisplay mainDisplay)
        {
            // メインカード以外が同じ Canvas 上に残っていると、XR/UI のクリックを奪うことがある。
            // 古いカードや成功表示専用カードは、見えていても入力対象から外す。
            foreach (var display in FindObjectsByType<EquationLessonCardDisplay>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (display == null || display == mainDisplay)
                {
                    continue;
                }

                foreach (var graphic in display.GetComponentsInChildren<Graphic>(true))
                {
                    if (graphic != null)
                    {
                        graphic.raycastTarget = false;
                    }
                }

                foreach (var selectable in display.GetComponentsInChildren<Selectable>(true))
                {
                    if (selectable != null)
                    {
                        selectable.interactable = false;
                    }
                }

                foreach (var raycaster in display.GetComponentsInChildren<BaseRaycaster>(true))
                {
                    if (raycaster != null)
                    {
                        raycaster.enabled = false;
                    }
                }
            }
        }

        private static bool IsSuccessOnlyCardDisplay(EquationLessonCardDisplay display)
        {
            // SuccessCard 配下の display は「成功後の見た目」用なので、問題操作カードとして扱わない。
            return display != null && HasTransformNamed(display.transform, "SuccessCard");
        }

        private static bool HasTransformNamed(Transform transform, string objectName)
        {
            // 子側のコンポーネントから呼ばれても、親階層に目的の名前があるか確認する。
            while (transform != null)
            {
                if (transform.name == objectName)
                {
                    return true;
                }

                transform = transform.parent;
            }

            return false;
        }

        /// <summary>
        /// 最初の 3 = ? チュートリアルカードを表示します。
        /// </summary>
        private void ShowInitialCard()
        {
            if (cardDisplay == null)
            {
                return;
            }

            cardDisplay.ShowDumbbellTutorial(fixedSide, balanceOnlyTargetWeight);
        }

        /// <summary>
        /// 3 = ? ステージで、固定辺の反対側に置かれた重りを読み取り、式文字列と合計値を返します。
        /// </summary>
        private int ReadUnknownSide(out string expression)
        {
            var state = boardReader.ReadUnknownSide(UnknownSide, leftSockets, rightSockets);
            expression = state.Expression;
            return state.Total;
        }

        /// <summary>
        /// 現在のステージとソケット状態に応じて、カードの式表示と成功表示を更新します。
        /// </summary>
        private void UpdateCard(string expression, int unknownTotal)
        {
            if (cardDisplay == null)
            {
                return;
            }

            if (lessonStage == LessonStage.VariableExpression)
            {
                // x の問題では右辺/左辺の実ソケット状態から表示を作る。
                // そのため BalanceOnly 用の unknownTotal は使わない。
                UpdateVariableExpressionCard();
                return;
            }

            if (unknownTotal == lastTotal && expression == lastExpression && lessonStage == lastShownStage)
            {
                return;
            }

            lastTotal = unknownTotal;
            lastExpression = expression;
            lastShownStage = lessonStage;

            UpdateBalanceOnlyCard(expression, unknownTotal);
        }

        /// <summary>
        /// 現在のステージの左右重量を読み、平均台の傾きを更新します。
        /// </summary>
        private void UpdateBoardTilt(int unknownTotal)
        {
            if (boardVisual == null)
            {
                return;
            }

            if (lessonStage == LessonStage.VariableExpression)
            {
                // x も answerValue の重さとして数え、左右の実重量差で傾ける。
                UpdateBoardTilt(
                    boardReader.ReadSideTotal(BalanceSide.Left, leftSockets, rightSockets, answerValue),
                    boardReader.ReadSideTotal(BalanceSide.Right, leftSockets, rightSockets, answerValue));
                return;
            }

            var fixedWeight = answerValue;
            if (lessonStage == LessonStage.BalanceOnly)
            {
                fixedWeight = balanceOnlyTargetWeight;
            }

            var leftWeight = fixedSide == BalanceSide.Left ? fixedWeight : unknownTotal;
            var rightWeight = fixedSide == BalanceSide.Left ? unknownTotal : fixedWeight;
            UpdateBoardTilt(leftWeight, rightWeight);
        }

        /// <summary>
        /// 左右重量差を角度へ変換し、板のローカル回転をなめらかに追従させます。
        /// </summary>
        private void UpdateBoardTilt(float leftWeight, float rightWeight)
        {
            tiltUpdater.Update(
                boardVisual,
                boardBaseRotation,
                leftWeight,
                rightWeight,
                maxWeightDifference,
                maxTiltDegrees,
                tiltFollowSpeed);
        }

        private void ResetBoardTiltImmediate()
        {
            // Clear / Set の直後は補間を待たず、初期角度へ戻して見た目をリセットする。
            tiltUpdater.Reset(boardVisual, boardBaseRotation);
        }

        private BalanceSide UnknownSide => fixedSide == BalanceSide.Left ? BalanceSide.Right : BalanceSide.Left;

        /// <summary>
        /// 外部の Start ボタンから、x = 3 の問題を明示的に開始します。
        /// </summary>
        public void BeginXEqualsThreeProblem()
        {
            BeginVariableEquationProblem(3, 0);
        }

        /// <summary>
        /// 外部ボタンや別 controller から、x + offset = answer + offset の問題を開始します。
        /// offset が 0 の場合は x = answer として表示します。
        /// </summary>
        public void BeginVariableEquationProblem(int answer, int offset)
        {
            lessonReset = false;
            answerValue = Mathf.Max(1, answer);
            equationOffset = Mathf.Max(0, offset);
            variableProblemIndex = Mathf.Max(1, variableProblemIndex);
            lessonStage = LessonStage.VariableExpression;
            variableExpressionObjectsPlaced = false;
            initialFixedWeightsPlaced = true;
            showConfiguredVariableEquation = true;
            lastTotal = -1;
            lastExpression = "";
            lastShownStage = LessonStage.BalanceOnly;

            ResolveSceneReferences();
            StartVariableExpressionStage();

            // x = 3 は初期配置だけで解が読めるため、開始直後は成功表示へ切り替えず問題式を見せる。
            showConfiguredVariableEquation = true;
            lastExpression = "";
            ShowConfiguredVariableEquation();
        }

        /// <summary>
        /// Clear ボタン用。問題表示、平均台上の配置、x 箱、傾きをすべて初期化します。
        /// </summary>
        public void ResetExpressionBalanceLesson()
        {
            ResolveSceneReferences();

            var variableBox = ResolveVariableBox();
            var allWeights = FindObjectsByType<WeightedDumbbell>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(weight => weight != null && (variableBox == null || !IsSameHierarchy(weight.transform, variableBox.transform)))
                .ToList();

            ResetBoardForEquationPlacement(allWeights, variableBox);
            ResetBoardTiltImmediate();

            lessonStage = LessonStage.BalanceOnly;
            lessonReset = true;
            initialFixedWeightsPlaced = true;
            variableExpressionObjectsPlaced = false;
            showConfiguredVariableEquation = false;
            variableProblemIndex = 0;
            lastTotal = -1;
            lastExpression = "";
            lastShownStage = LessonStage.BalanceOnly;

            if (cardDisplay != null)
            {
                cardDisplay.Show("", "", "", "");
            }

            Debug.Log("[ExpressionBalance:Clear] Reset problem, board placements, variable box, weights, and board tilt.");
        }

        /// <summary>
        /// Gen ボタン用。新しい問題文だけを生成し、平均台上の配置は変更しません。
        /// </summary>
        public void GenerateProblemTextOnly()
        {
            lessonReset = false;
            ResolveSceneReferences();

            lessonStage = LessonStage.VariableExpression;
            GenerateRandomVariableEquation();
            variableProblemIndex = Mathf.Max(1, variableProblemIndex + 1);
            variableExpressionObjectsPlaced = false;
            showConfiguredVariableEquation = true;
            lastTotal = -1;
            lastExpression = "";
            lastShownStage = LessonStage.BalanceOnly;

            ShowConfiguredVariableEquation();
            Debug.Log($"[ExpressionBalance:Gen] Generated problem text only: {BuildConfiguredVariableEquationText()}");
        }

        /// <summary>
        /// x 問題の基本式文字列を作成します。
        /// </summary>
        private string BuildEquation(string unknownExpression)
        {
            var expression = string.IsNullOrWhiteSpace(unknownExpression) ? "?" : unknownExpression;
            if (equationOffset <= 0)
            {
                return $"{expression} {GetComparisonSymbol(expression)} {answerValue}";
            }

            var rightSide = answerValue + equationOffset;
            return $"{expression} + {equationOffset} {GetComparisonSymbol(expression)} {rightSide}";
        }

        /// <summary>
        /// 3 = ? 形式の進行表示で使う比較記号を、置かれた重り合計から返します。
        /// </summary>
        private string GetComparisonSymbol(string expression)
        {
            if (expression == "x" || expression == "?")
            {
                return "=";
            }

            var placedSide = lastTotal + equationOffset;
            var rightSide = answerValue + equationOffset;

            if (placedSide < rightSide)
            {
                return "<";
            }

            if (placedSide > rightSide)
            {
                return ">";
            }

            return "=";
        }

        /// <summary>
        /// 3 = ? ステージのカード表示を、未配置、途中、正解の状態に切り替えます。
        /// </summary>
        private void UpdateBalanceOnlyCard(string expression, int unknownTotal)
        {
            // 3 = ? のチュートリアル表示。右側の重り数に応じて <, >, = の表示へ進む。
            if (unknownTotal == 0 && expression == "?")
            {
                cardDisplay.ShowDumbbellTutorial(fixedSide, balanceOnlyTargetWeight);
                return;
            }

            if (unknownTotal == balanceOnlyTargetWeight)
            {
                cardDisplay.ShowDumbbellTutorialCorrect(fixedSide, balanceOnlyTargetWeight, expression);
                return;
            }

            cardDisplay.ShowDumbbellTutorialProgress(fixedSide, balanceOnlyTargetWeight, expression, unknownTotal);
        }

        /// <summary>
        /// x + 2 = 3 ステージのソケット状態を読み、式変形と成功状態をカードへ反映します。
        /// </summary>
        private void UpdateVariableExpressionCard()
        {
            if (showConfiguredVariableEquation)
            {
                ShowConfiguredVariableEquation();
                return;
            }

            // x + 2 = 3 では、プレイヤーが両辺から同じ重りを取ると式表示も変形する。
            // 最終的に「左に x だけ、右に 1 だけ」なら x = 1 として成功扱いにする。
            var state = boardReader.ReadVariableEquationState(leftSockets, rightSockets, answerValue);
            var cardKey = $"{state.Problem}|{state.Operation}|{state.Answer}|{state.IsSolved}";
            if (cardKey == lastExpression && lessonStage == lastShownStage)
            {
                return;
            }

            lastExpression = cardKey;
            lastTotal = Mathf.RoundToInt(state.LeftTotal + state.RightTotal);
            lastShownStage = lessonStage;

            if (state.IsSolved)
            {
                cardDisplay.ShowEquationCorrect(state.Problem, state.Answer);
                return;
            }

            cardDisplay.Show("", state.Problem, state.Operation, state.Answer);
        }

        /// <summary>
        /// 成功ボタン押下時に、現在の問題から次の問題へ進めます。
        /// </summary>
        private void AdvanceLessonAfterSuccess()
        {
            // XR の Poke/Select と UI Button の onClick が同じフレームで二重発火することがある。
            // 1回の成功操作で 2問目固定 x + 2 = 3 を飛ばしてランダム問題へ進まないよう止める。
            if (lastAdvanceFrame == Time.frameCount)
            {
                return;
            }

            lastAdvanceFrame = Time.frameCount;

            if (lessonStage == LessonStage.BalanceOnly)
            {
                AdvanceToFixedVariableExpressionStage();
                return;
            }

            AdvanceToRandomVariableExpressionStage();
        }

        /// <summary>
        /// 2問目として、固定の x + 2 = 3 ステージへ進め、配置とカードを更新します。
        /// </summary>
        private void AdvanceToFixedVariableExpressionStage()
        {
            answerValue = FixedVariableAnswer;
            equationOffset = FixedVariableOffset;
            variableProblemIndex = 1;
            StartVariableExpressionStage();
        }

        /// <summary>
        /// 3問目以降として、4ソケットに収まるランダムな x + a = b ステージへ進めます。
        /// </summary>
        private void AdvanceToRandomVariableExpressionStage()
        {
            if (variableProblemIndex < 1)
            {
                AdvanceToFixedVariableExpressionStage();
                return;
            }

            GenerateRandomVariableEquation();
            variableProblemIndex++;
            StartVariableExpressionStage();
        }

        /// <summary>
        /// 現在の answerValue / equationOffset を使って、x の式ステージを開始します。
        /// </summary>
        private void StartVariableExpressionStage()
        {
            // Next から呼ばれる。新しい式を表示したら、前問の配置が残らないよう同じ式で即配置する。
            lessonStage = LessonStage.VariableExpression;
            variableExpressionObjectsPlaced = false;
            showConfiguredVariableEquation = true;
            lastTotal = -1;
            lastExpression = "";
            lastShownStage = LessonStage.BalanceOnly;

            ShowConfiguredVariableEquation();
            SetCurrentEquationOnBoard();
        }

        /// <summary>
        /// 現在の answerValue / equationOffset から、配置前の問題式を表示します。
        /// </summary>
        private void ShowConfiguredVariableEquation()
        {
            if (cardDisplay == null)
            {
                return;
            }

            var problem = BuildConfiguredVariableEquationText();
            if (lastExpression == problem && lessonStage == lastShownStage)
            {
                return;
            }

            lastExpression = problem;
            lastTotal = -1;
            lastShownStage = lessonStage;
            cardDisplay.Show("", problem, "", "");
        }

        private string BuildConfiguredVariableEquationText()
        {
            // 現在保持している answer / offset を、カード用の文字式へ変換する。
            return problemGenerator.Format(new ExpressionBalanceProblem(answerValue, equationOffset));
        }

        /// <summary>
        /// Clear ボタン用。平均台ソケットから選択物を外し、重りと x 箱を平均台外へ退避します。
        /// </summary>
        private void ClearEquationBoard()
        {
            var variableBox = ResolveVariableBox();
            var allWeights = FindObjectsByType<WeightedDumbbell>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(weight => weight != null && (variableBox == null || !IsSameHierarchy(weight.transform, variableBox.transform)))
                .ToList();

            ResetBoardForEquationPlacement(allWeights, variableBox);
            ResetBoardTiltImmediate();
            variableExpressionObjectsPlaced = false;
            showConfiguredVariableEquation = lessonStage == LessonStage.VariableExpression;
            lastExpression = "";
            UpdateVariableExpressionCard();
        }

        /// <summary>
        /// Gen ボタン用。現在平均台に載っているものから式表示を作ります。
        /// </summary>
        private void GenerateEquationFromBoard()
        {
            showConfiguredVariableEquation = false;
            lastExpression = "";

            if (lessonStage == LessonStage.VariableExpression)
            {
                UpdateVariableExpressionCard();
                return;
            }

            var unknownTotal = ReadUnknownSide(out var expression);
            UpdateBalanceOnlyCard(expression, unknownTotal);
        }

        /// <summary>
        /// Set ボタン用。カードに表示されている問題文を読み、平均台へ x と重りを配置します。
        /// </summary>
        public void PlaceCurrentProblemObjectsOnBoard()
        {
            SetCurrentEquationOnBoard();
        }

        /// <summary>
        /// Set ボタン用。現在の式設定に合わせて、平均台へ x と重りを配置します。
        /// </summary>
        private void SetCurrentEquationOnBoard()
        {
            Debug.Log($"{SetDebugPrefix} Set pressed. stage={lessonStage}, answer={answerValue}, offset={equationOffset}, equation={BuildConfiguredVariableEquationText()}");

            if (TryApplyDisplayedVariableEquation())
            {
                Debug.Log($"{SetDebugPrefix} Applied displayed equation before placement. answer={answerValue}, offset={equationOffset}, equation={BuildConfiguredVariableEquationText()}");
            }
            else if (lessonStage == LessonStage.BalanceOnly)
            {
                Debug.Log($"{SetDebugPrefix} BalanceOnly stage detected. Advancing to fixed variable equation before placement.");
                AdvanceToFixedVariableExpressionStage();
            }

            showConfiguredVariableEquation = false;
            variableExpressionObjectsPlaced = false;
            lastExpression = "";
            ResetBoardTiltImmediate();
            TryPlaceVariableExpressionObjects();
            UpdateVariableExpressionCard();
            ResetBoardTiltImmediate();

            Debug.Log($"{SetDebugPrefix} Set finished. leftSelections={DescribeSocketSelections(leftSockets)}, rightSelections={DescribeSocketSelections(rightSockets)}");
        }

        private bool TryApplyDisplayedVariableEquation()
        {
            // Set ボタンは「今カードに出ている式」を優先する。
            // Gen で作った式を、内部値へ同期してから配置するための読み取り処理。
            if (cardDisplay == null)
            {
                return false;
            }

            var rawText = cardDisplay.CurrentDisplayedText;
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return false;
            }

            var plainText = Regex.Replace(rawText, "<.*?>", "");

            // x + a = b 形式を読む。TMP のタグは上で消している。
            var match = Regex.Match(plainText, @"x\s*\+\s*(\d+)\s*=\s*(\d+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var parsedOffset = int.Parse(match.Groups[1].Value);
                var parsedRightSide = int.Parse(match.Groups[2].Value);
                var parsedAnswer = parsedRightSide - parsedOffset;

                // x の答えが 0 以下になる式は、このレッスンの問題として扱わない。
                if (parsedOffset < 0 || parsedAnswer <= 0)
                {
                    Debug.LogWarning($"{SetDebugPrefix} Displayed equation was ignored because it is invalid: '{plainText}'");
                    return false;
                }

                equationOffset = parsedOffset;
                answerValue = parsedAnswer;
                lessonStage = LessonStage.VariableExpression;
                variableProblemIndex = Mathf.Max(1, variableProblemIndex);
                showConfiguredVariableEquation = false;
                return true;
            }

            // offset がない x = n 形式も Start ボタンなどから来るため読む。
            var directMatch = Regex.Match(plainText, @"\bx\s*=\s*(\d+)\b", RegexOptions.IgnoreCase);
            if (!directMatch.Success)
            {
                return false;
            }

            answerValue = Mathf.Max(1, int.Parse(directMatch.Groups[1].Value));
            equationOffset = 0;
            lessonStage = LessonStage.VariableExpression;
            variableProblemIndex = Mathf.Max(1, variableProblemIndex);
            showConfiguredVariableEquation = false;
            return true;
        }

        /// <summary>
        /// 4つのソケットに収まる範囲で、ランダムな x + a = b の値を決めます。
        /// </summary>
        private void GenerateRandomVariableEquation()
        {
            // 左は「x と a 個の重り」、右は「答え + a 個の重り」を置くため、
            // 各辺のソケット数と実際にある重り数以内に制限する。
            var problem = problemGenerator.GenerateRandom(
                randomMinAnswer,
                randomMaxAnswer,
                randomMinOffset,
                randomMaxOffset,
                leftSockets.Count,
                rightSockets.Count,
                CountAvailableUnitWeights(),
                new ExpressionBalanceProblem(answerValue, equationOffset),
                new ExpressionBalanceProblem(FixedVariableAnswer, FixedVariableOffset));

            answerValue = problem.Answer;
            equationOffset = problem.Offset;
        }

        /// <summary>
        /// x 箱を除いた、式配置に使える重りの数を返します。
        /// </summary>
        private static int CountAvailableUnitWeights()
        {
            var variableBox = ResolveVariableBox();
            return FindObjectsByType<WeightedDumbbell>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Count(weight => weight != null
                                 && (variableBox == null || !IsSameHierarchy(weight.transform, variableBox.transform)));
        }

        /// <summary>
        /// シーン内から、指定文字列を名前に含む Transform を探します。
        /// </summary>
        private static Transform FindObjectByNamePart(string namePart)
        {
            foreach (var transform in FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (transform.name.Contains(namePart))
                {
                    return transform;
                }
            }

            return null;
        }

        /// <summary>
        /// 指定した子名を持つ Transform を、子階層から再帰的に探します。
        /// </summary>
        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root.name == childName)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindChildRecursive(root.GetChild(i), childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// x 箱らしい XRGrabInteractable に、ソケット占有情報を自動付与します。
        /// </summary>
        private static void AutoAssignExpressionFootprints()
        {
            // x 箱をソケット対象として扱えるよう、名前から自動で足跡情報を付ける。
            foreach (var grab in FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!LooksLikeVariableBox(grab.name))
                {
                    continue;
                }

                var footprint = grab.GetComponent<BalanceSocketFootprint>();
                if (footprint == null)
                {
                    footprint = grab.gameObject.AddComponent<BalanceSocketFootprint>();
                }

                footprint.SlotSpan = 1;
            }
        }

        /// <summary>
        /// オブジェクト名から x 箱として扱うべきか判定します。
        /// </summary>
        private static bool LooksLikeVariableBox(string objectName)
        {
            return objectName.Equals("xbox", System.StringComparison.OrdinalIgnoreCase)
                   || objectName.Equals("x box", System.StringComparison.OrdinalIgnoreCase)
                   || objectName.Equals("x_box", System.StringComparison.OrdinalIgnoreCase)
                   || objectName.Contains("xbox", System.StringComparison.OrdinalIgnoreCase)
                   || objectName.Contains("x box", System.StringComparison.OrdinalIgnoreCase)
                   || objectName.Contains("x_box", System.StringComparison.OrdinalIgnoreCase)
                   || objectName.Contains("variable", System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 平均台配下の静的 XRSocketInteractor を左右リストへ振り分け、使用順に並べます。
        /// </summary>
        private void ResolveStaticSockets()
        {
            // シーンに手置きした XRSocketInteractor を左右のソケットリストへ集める。
            // BalanceBoardSocketSlot が付いていれば、その Side/Index を優先する。
            if (boardRoot == null)
            {
                return;
            }

            var leftRecords = new List<(XRSocketInteractor Socket, int Index, float LocalX)>();
            var rightRecords = new List<(XRSocketInteractor Socket, int Index, float LocalX)>();

            foreach (var socket in boardRoot.GetComponentsInChildren<XRSocketInteractor>(true))
            {
                if (!TryResolveStaticSocket(socket, out var side, out var index, out var localX))
                {
                    continue;
                }

                ConfigureStaticSocket(socket);
                RegisterSocketDebugLogging(socket);

                var records = side == BalanceSide.Left ? leftRecords : rightRecords;
                records.Add((socket, index, localX));
            }

            ApplySocketRecords(leftRecords, leftSockets);
            ApplySocketRecords(rightRecords, rightSockets);
        }

        /// <summary>
        /// ソケットが左右どちらの何番目かを、BalanceBoardSocketSlot または位置から解決します。
        /// </summary>
        private bool TryResolveStaticSocket(XRSocketInteractor socket, out BalanceSide side, out int index, out float localX)
        {
            side = BalanceSide.Left;
            index = int.MaxValue;
            localX = 0f;

            if (socket == null || boardRoot == null)
            {
                return false;
            }

            var local = boardRoot.InverseTransformPoint(socket.transform.position);
            localX = local.x;

            var slot = socket.GetComponent<BalanceBoardSocketSlot>();
            if (slot == null)
            {
                slot = socket.GetComponentInParent<BalanceBoardSocketSlot>();
            }

            if (slot != null)
            {
                side = slot.Side;
                index = slot.Index;
                return true;
            }

            if (Mathf.Abs(local.x) < sideDeadZone || Mathf.Abs(local.x) > boardHalfWidth)
            {
                return false;
            }

            side = local.x < 0f ? BalanceSide.Left : BalanceSide.Right;
            return true;
        }

        /// <summary>
        /// 収集したソケット情報を、Index 優先または左から右の座標順で実際のソケットリストへ反映します。
        /// </summary>
        private static void ApplySocketRecords(
            List<(XRSocketInteractor Socket, int Index, float LocalX)> records,
            List<XRSocketInteractor> sockets)
        {
            // Index が設定済みのソケットは Index 順、未設定なら板ローカル X の左から右で並べる。
            // この順番が後続の「1,2,3,4 番ソケット」の基準になる。
            records.Sort((a, b) =>
            {
                var indexComparison = a.Index.CompareTo(b.Index);
                if (indexComparison != 0)
                {
                    return indexComparison;
                }

                return a.LocalX.CompareTo(b.LocalX);
            });

            sockets.Clear();
            foreach (var record in records)
            {
                sockets.Add(record.Socket);
            }
        }

        /// <summary>
        /// 手置きされたソケットを、重りや x 箱を受け取れる XRSocketInteractor として設定します。
        /// </summary>
        private static void ConfigureStaticSocket(XRSocketInteractor socket)
        {
            socket.socketActive = true;
            socket.showInteractableHoverMeshes = false;
            socket.hoverSocketSnapping = true;
            socket.recycleDelayTime = 0.05f;
        }

        /// <summary>
        /// 実際にソケットへ入った瞬間、左右/Index/入った物体をログへ出します。
        /// </summary>
        private void RegisterSocketDebugLogging(XRSocketInteractor socket)
        {
            if (socket == null || !socketsWithDebugLogging.Add(socket))
            {
                return;
            }

            socket.selectEntered.AddListener(args => LogSocketEntered(socket, args.interactableObject));
        }

        private void LogSocketEntered(XRSocketInteractor socket, IXRSelectInteractable interactable)
        {
            // 自動配置と手動配置の両方で、どのソケットに何が入ったか確認しやすくするログ。
            var hasIndex = TryGetSocketVisualIndex(socket, out var side, out var index);
            var sideText = hasIndex ? side.ToString() : "Unknown";
            var indexText = hasIndex ? index.ToString() : "unknown";

            Debug.Log(
                $"{SocketDebugPrefix} Entered. side={sideText}, index={indexText}, object={DescribeSocketInteractable(interactable)}, socket={DescribeSocket(socket)}");
        }

        private bool TryGetSocketVisualIndex(XRSocketInteractor socket, out BalanceSide side, out int index)
        {
            // まず Bootstrap が保持している左右リスト内の位置を優先して、見た目上の番号を決める。
            side = BalanceSide.Left;
            index = -1;

            if (socket == null)
            {
                return false;
            }

            var leftIndex = leftSockets.IndexOf(socket);
            if (leftIndex >= 0)
            {
                side = BalanceSide.Left;
                index = leftIndex;
                return true;
            }

            var rightIndex = rightSockets.IndexOf(socket);
            if (rightIndex >= 0)
            {
                side = BalanceSide.Right;
                index = rightIndex;
                return true;
            }

            return TryResolveStaticSocket(socket, out side, out index, out _) && index != int.MaxValue;
        }

        /// <summary>
        /// 最初の 3 = ? ステージ用に、固定辺へ 1 の重りを指定数だけ初期配置します。
        /// </summary>
        private void TryPlaceInitialFixedWeights()
        {
            // 最初の 3 = ? 用配置。左側の中央寄りソケットに 1 の重りを 3 個置く。
            if (initialFixedWeightsPlaced || boardRoot == null || lessonStage != LessonStage.BalanceOnly)
            {
                return;
            }

            var neededCount = Mathf.CeilToInt(balanceOnlyTargetWeight);
            if (neededCount <= 0)
            {
                initialFixedWeightsPlaced = true;
                return;
            }

            var fixedSockets = fixedSide == BalanceSide.Left ? leftSockets : rightSockets;
            if (fixedSockets.Count < neededCount)
            {
                return;
            }

            var allWeights = FindObjectsByType<WeightedDumbbell>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(weight => weight != null)
                .OrderBy(weight => Vector3.Distance(weight.transform.position, boardRoot.position))
                .ToList();
            var variableBox = ResolveVariableBox();

            // まず平均台上を空にしてから、現在の式に必要なものだけを置く。
            ResetBoardForEquationPlacement(allWeights, variableBox);

            var weights = allWeights
                .Where(weight => Mathf.RoundToInt(weight.Weight.Value) == 1)
                .Take(neededCount)
                .ToList();

            if (weights.Count < neededCount)
            {
                return;
            }

            var socketStartIndex = Mathf.Max(0, (fixedSockets.Count - neededCount) / 2);
            for (var i = 0; i < neededCount; i++)
            {
                PlaceWeightInSocket(weights[i], fixedSockets[socketStartIndex + i]);
            }

            initialFixedWeightsPlaced = true;
        }

        /// <summary>
        /// x + a = b ステージ用に、左辺へ x と a 個の重り、右辺へ b 個の重りを配置します。
        /// </summary>
        private void TryPlaceVariableExpressionObjects()
        {
            if (lessonStage != LessonStage.VariableExpression || variableExpressionObjectsPlaced || boardRoot == null)
            {
                Debug.LogWarning($"{SetDebugPrefix} Abort before placement. stage={lessonStage}, placed={variableExpressionObjectsPlaced}, boardRoot={(boardRoot != null ? boardRoot.name : "null")}, leftSocketCount={leftSockets.Count}, rightSocketCount={rightSockets.Count}");
                return;
            }

            var variableBox = ResolveVariableBox();
            if (variableBox == null)
            {
                Debug.LogWarning($"{SetDebugPrefix} Abort: variable box was not found.");
                return;
            }

            var leftExpressionSockets = GetSocketsLeftToRight(BalanceSide.Left);
            var rightExpressionSockets = GetSocketsLeftToRight(BalanceSide.Right);
            Debug.Log($"{SetDebugPrefix} Left expression socket candidates: {DescribeSockets(leftExpressionSockets)}");
            Debug.Log($"{SetDebugPrefix} Right expression socket candidates: {DescribeSockets(rightExpressionSockets)}");

            var problem = new ExpressionBalanceProblem(answerValue, equationOffset);

            // ここでは「どのソケットに何を置くか」を決めるだけ。実配置は SocketPlacer に任せる。
            if (!socketLayoutPlanner.TryPlan(problem, leftExpressionSockets, rightExpressionSockets, out var plan, out var planError))
            {
                Debug.LogWarning($"{SetDebugPrefix} Abort: {planError}");
                return;
            }

            Debug.Log($"{SetDebugPrefix} Resolved layout. xSocket={DescribeSocket(plan.VariableSocket)}, leftWeightSockets={DescribeSockets(plan.LeftWeightSockets)}, rightWeightSockets={DescribeSockets(plan.RightWeightSockets)}");

            var allWeights = FindObjectsByType<WeightedDumbbell>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(weight => weight != null && !IsSameHierarchy(weight.transform, variableBox.transform))
                .OrderBy(weight => Vector3.Distance(weight.transform.position, boardRoot.position))
                .ToList();

            if (allWeights.Count < plan.RequiredWeightCount)
            {
                Debug.LogWarning($"{SetDebugPrefix} Abort: not enough weights. needed={plan.RequiredWeightCount}, found={allWeights.Count}");
                return;
            }

            // 前ステージの重りやソケット選択が残らないよう、一度まっさらにする。
            ResetBoardForEquationPlacement(allWeights, variableBox);
            Debug.Log($"{SetDebugPrefix} After reset. leftSelections={DescribeSocketSelections(leftSockets)}, rightSelections={DescribeSocketSelections(rightSockets)}");

            var weights = allWeights
                .Take(plan.RequiredWeightCount)
                .ToList();
            Debug.Log($"{SetDebugPrefix} Selected weights for placement: {string.Join(", ", weights.Select(DescribeWeight))}");

            // Planner の結果を使って、x 箱と重りを実際の XRSocketInteractor へ選択させる。
            if (!socketPlacer.TryPlace(plan, variableBox, weights, out var usedWeights, out var placeError))
            {
                Debug.LogWarning($"{SetDebugPrefix} Abort: {placeError}");
                return;
            }

            // 今回の式に不要な重りは、式の読み取りに混ざらないよう板の外へ出す。
            boardClearer.ClearUnusedWeights(allWeights, usedWeights, boardRoot, boardHalfWidth);
            variableExpressionObjectsPlaced = true;
            Debug.Log($"{SetDebugPrefix} Placement complete. leftSelections={DescribeSocketSelections(leftSockets)}, rightSelections={DescribeSocketSelections(rightSockets)}");
        }

        /// <summary>
        /// 式配置の前に、左右ソケットの選択解除、重りの値リセットと退避、x 箱の退避をまとめて行います。
        /// </summary>
        private void ResetBoardForEquationPlacement(IReadOnlyList<WeightedDumbbell> weights, XRGrabInteractable variableBox)
        {
            // 式を切り替える前の共通リセット。
            // 1. 左右ソケットの選択を解除
            // 2. 重りを全部 1 に戻して板の外へ退避
            // 3. x 箱も板の外へ退避
            boardClearer.Clear(leftSockets, rightSockets, weights, variableBox, boardRoot, boardHalfWidth);
        }

        /// <summary>
        /// 指定した辺の先頭 4 ソケットを、式ゲーム上の 1,2,3,4 番ソケットとして返します。
        /// </summary>
        private List<XRSocketInteractor> GetSocketsLeftToRight(BalanceSide side)
        {
            // すでに ResolveStaticSockets で並べた左から右の順番をソケット番号として使う。
            var sockets = side == BalanceSide.Left ? leftSockets : rightSockets;
            return sockets
                .Where(socket => socket != null)
                .Take(4)
                .ToList();
        }

        private static string DescribeSockets(IEnumerable<XRSocketInteractor> sockets)
        {
            // デバッグログ用。null の場合もログ上で分かるよう文字列化する。
            return sockets == null ? "null" : string.Join(" | ", sockets.Select(DescribeSocket));
        }

        private static string DescribeSocket(XRSocketInteractor socket)
        {
            // デバッグログ用。パス、slot 情報、実位置、attach 位置をまとめて出す。
            if (socket == null)
            {
                return "null";
            }

            var slot = socket.GetComponent<BalanceBoardSocketSlot>();
            var slotText = slot != null ? $"slotSide={slot.Side}, slotIndex={slot.Index}" : "slot=null";
            var attach = socket.attachTransform != null ? socket.attachTransform : socket.transform;
            return $"{GetTransformPath(socket.transform)} ({slotText}, pos={socket.transform.position:F3}, attach={attach.position:F3})";
        }

        private static string DescribeSocketSelection(XRSocketInteractor socket)
        {
            // デバッグログ用。ソケットが空か、何を選択中かを文字列化する。
            if (socket == null)
            {
                return "socket=null";
            }

            if (!socket.hasSelection)
            {
                return "empty";
            }

            return string.Join(", ", socket.interactablesSelected.Select(interactable => interactable?.transform != null ? GetTransformPath(interactable.transform) : "null"));
        }

        private static string DescribeSocketInteractable(IXRSelectInteractable interactable)
        {
            // デバッグログ用。選択物を weight / x / other に分類して見やすくする。
            if (interactable?.transform == null)
            {
                return "null";
            }

            var transform = interactable.transform;
            var weight = transform.GetComponentInParent<WeightedDumbbell>();
            if (weight != null)
            {
                return $"{GetTransformPath(weight.transform)}(type=weight, value={weight.Weight.Value}, pos={weight.transform.position:F3})";
            }

            if (LooksLikeVariableTransform(transform))
            {
                return $"{GetTransformPath(transform)}(type=x, pos={transform.position:F3})";
            }

            return $"{GetTransformPath(transform)}(type=other, pos={transform.position:F3})";
        }

        private static string DescribeSocketSelections(IEnumerable<XRSocketInteractor> sockets)
        {
            // デバッグログ用。左右リスト内の各ソケットと、その選択物をまとめて出す。
            if (sockets == null)
            {
                return "null";
            }

            return string.Join(" | ", sockets.Select(socket => $"{DescribeSocket(socket)} => {DescribeSocketSelection(socket)}"));
        }

        private static string DescribeWeight(WeightedDumbbell weight)
        {
            // デバッグログ用。重りの階層パス、値、ワールド位置を出す。
            if (weight == null)
            {
                return "null";
            }

            return $"{GetTransformPath(weight.transform)}(value={weight.Weight.Value}, pos={weight.transform.position:F3})";
        }

        private static string GetTransformPath(Transform transform)
        {
            // デバッグログ用。Hierarchy 上で同名オブジェクトがあっても追いやすいようフルパス化する。
            if (transform == null)
            {
                return "null";
            }

            var names = new Stack<string>();
            var current = transform;
            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", names);
        }

        /// <summary>
        /// シーン内から x 箱を解決し、掴める XR オブジェクトとして準備します。
        /// </summary>
        private static XRGrabInteractable ResolveVariableBox()
        {
            // 名前から x 箱を見つけ、XRGrabInteractable がなければ追加する。
            foreach (var grab in FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (grab != null && LooksLikeVariableBox(grab.name))
                {
                    PrepareVariableInteractable(grab);
                    return grab;
                }
            }

            foreach (var transform in FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (transform == null || !LooksLikeVariableBox(transform.name))
                {
                    continue;
                }

                var grab = transform.GetComponent<XRGrabInteractable>();
                if (grab == null)
                {
                    grab = transform.gameObject.AddComponent<XRGrabInteractable>();
                }

                PrepareVariableInteractable(grab);
                return grab;
            }

            return null;
        }

        /// <summary>
        /// x 箱へ Rigidbody、Collider、XRGrabInteractable、ソケット足跡情報を整えます。
        /// </summary>
        private static void PrepareVariableInteractable(XRGrabInteractable grab)
        {
            // x 箱を「掴める/ソケットできる」状態に整える。
            if (grab == null)
            {
                return;
            }

            var rb = grab.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = grab.gameObject.AddComponent<Rigidbody>();
            }

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.mass = 1f;

            var colliders = grab.GetComponentsInChildren<Collider>(true)
                .Where(collider => collider != null && !collider.isTrigger)
                .ToList();
            if (colliders.Count == 0)
            {
                colliders.Add(grab.gameObject.AddComponent<BoxCollider>());
            }

            grab.enabled = true;
            grab.useDynamicAttach = true;
            grab.matchAttachPosition = true;
            grab.matchAttachRotation = true;
            grab.trackRotation = false;
            grab.colliders.Clear();
            foreach (var collider in colliders)
            {
                grab.colliders.Add(collider);
            }

            var footprint = grab.GetComponent<BalanceSocketFootprint>();
            if (footprint == null)
            {
                footprint = grab.gameObject.AddComponent<BalanceSocketFootprint>();
            }

            footprint.SlotSpan = 1;
        }

        /// <summary>
        /// 指定 Transform とその親階層に、x 箱として扱う名前が含まれているか判定します。
        /// </summary>
        private static bool LooksLikeVariableTransform(Transform transform)
        {
            while (transform != null)
            {
                if (LooksLikeVariableBox(transform.name))
                {
                    return true;
                }

                transform = transform.parent;
            }

            return false;
        }

        /// <summary>
        /// 2 つの Transform が同一、親子、子親の関係にあるか判定します。
        /// </summary>
        private static bool IsSameHierarchy(Transform left, Transform right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            return left == right || left.IsChildOf(right) || right.IsChildOf(left);
        }

        /// <summary>
        /// 重りを指定ソケットの Attach Transform に合わせ、XRSocketInteractor に手動選択させます。
        /// </summary>
        private static void PlaceWeightInSocket(WeightedDumbbell weight, XRSocketInteractor socket)
        {
            // 重りを指定ソケットの Attach Transform へ移動し、SocketInteractor に手動選択させる。
            if (weight == null || socket == null)
            {
                return;
            }

            PrepareWeightInteractable(weight);

            var attach = socket.attachTransform != null ? socket.attachTransform : socket.transform;
            weight.transform.SetPositionAndRotation(attach.position, attach.rotation);

            if (weight.TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            var grab = weight.GetComponent<XRGrabInteractable>();
            if (grab != null && socket.interactionManager == null)
            {
                socket.interactionManager = Object.FindAnyObjectByType<XRInteractionManager>();
            }

            if (grab != null && socket.interactionManager != null && !socket.hasSelection)
            {
                socket.StartManualInteraction((IXRSelectInteractable)grab);
            }
        }

        /// <summary>
        /// シーン上の全重りに XRGrabInteractable と Collider を補い、ソケット対象として準備します。
        /// </summary>
        private void PrepareWeightInteractablesForSockets()
        {
            // シーン上の重りを、掴めてソケットできる XRGrabInteractable として一度だけ整える。
            if (weightInteractablesPrepared)
            {
                return;
            }

            var preparedCount = 0;
            foreach (var weight in FindObjectsByType<WeightedDumbbell>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (weight == null)
                {
                    continue;
                }

                PrepareWeightInteractable(weight);
                preparedCount++;
            }

            weightInteractablesPrepared = preparedCount > 0;
        }

        /// <summary>
        /// 1 つの重りへ Rigidbody、Collider、XRGrabInteractable を整えます。
        /// </summary>
        private static void PrepareWeightInteractable(WeightedDumbbell weight)
        {
            // 重りに Rigidbody / Collider / XRGrabInteractable を補い、ソケット対象に登録する。
            var rb = weight.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = weight.gameObject.AddComponent<Rigidbody>();
            }

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.mass = Mathf.Max(0.01f, weight.Weight.Value);

            var colliders = weight.GetComponentsInChildren<Collider>(true)
                .Where(collider => collider != null && !collider.isTrigger)
                .ToList();
            if (colliders.Count == 0)
            {
                var box = weight.gameObject.AddComponent<BoxCollider>();
                colliders.Add(box);
            }

            var grab = weight.GetComponent<XRGrabInteractable>();
            if (grab == null)
            {
                grab = weight.gameObject.AddComponent<XRGrabInteractable>();
            }

            grab.enabled = true;
            grab.useDynamicAttach = true;
            grab.matchAttachPosition = true;
            grab.matchAttachRotation = true;
            grab.trackRotation = false;
            grab.colliders.Clear();
            foreach (var collider in colliders)
            {
                grab.colliders.Add(collider);
            }
        }
    }
}
