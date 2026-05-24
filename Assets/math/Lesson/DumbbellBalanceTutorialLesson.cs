using TMPro;
using System.Collections;
using System.Collections.Generic;
using JusticeScale.Scripts;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VrMath.Core;
using VrMath.Interaction;
using VrMath.Rendering;

namespace VrMath.Lesson
{
    /// <summary>
    /// ダンベルだけで均衡を作るチュートリアル問題を、シーン上の天びんへ配置します。
    /// 固定側の数に対応するダンベルを複製し、指定された皿のソケットへ正式に差し込みます。
    /// </summary>
    public sealed class DumbbellBalanceTutorialLesson : MonoBehaviour
    {
        [SerializeField, Tooltip("最初からダンベルを置いておく辺です。")]
        private BalanceSide fixedSide = BalanceSide.Left;

        [SerializeField, Min(1), Tooltip("最初から置いておくダンベルの重さです。")]
        private int fixedWeight = 3;

        [SerializeField, Tooltip("左辺の固定ダンベルを入れるソケットです。")]
        private XRSocketInteractor leftFixedSocket;

        [SerializeField, Tooltip("右辺の固定ダンベルを入れるソケットです。")]
        private XRSocketInteractor rightFixedSocket;

        [SerializeField, Tooltip("問題式を表示するカードです。")]
        private EquationLessonCardDisplay lessonCardDisplay;

        [SerializeField, Tooltip("天びんの傾きを管理するコントローラーです。")]
        private ScaleController scaleController;

        [SerializeField, Tooltip("重さ 1 から順に、ラック上の元ダンベルを登録します。未設定なら Dumbbell1, Dumbbell2... を探します。")]
        private XRGrabInteractable[] dumbbellSourcesByWeight = new XRGrabInteractable[10];

        [SerializeField, Min(0.01f), Tooltip("このチュートリアル中に、何kg差で最大傾きにするか。")]
        private float tutorialMaxWeightDifference = 3f;

        private GameObject spawnedFixedDumbbell;
        private readonly List<XRSocketInteractor> unknownSideSockets = new();
        private readonly List<int> placedWeights = new();
        private string lastPlacedExpression = "?";
        private int lastPlacedTotal = -1;
        private bool showingSuccess;
        private DumbbellBalanceTutorialProblem currentProblem;

        private void Start()
        {
            SetupProblem();
        }

        /// <summary>
        /// 現在の設定値で、式表示と固定側ダンベルのソケット装填を行います。
        /// </summary>
        public void SetupProblem()
        {
            currentProblem = new DumbbellBalanceTutorialProblem(fixedSide, fixedWeight);
            RefreshUnknownSideSockets(currentProblem.UnknownSide);
            ShowProblem(currentProblem);
            StartCoroutine(SpawnFixedDumbbell(currentProblem));
        }

        private void Update()
        {
            if (lessonCardDisplay == null || unknownSideSockets.Count == 0)
            {
                return;
            }

            var placedExpression = BuildPlacedExpression();
            var isBalanced = currentProblem.IsBalanced(lastPlacedTotal);

            if (isBalanced)
            {
                if (!showingSuccess)
                {
                    showingSuccess = true;
                    lastPlacedExpression = placedExpression;
                    lessonCardDisplay.ShowDumbbellTutorialCorrect(currentProblem.FixedSide, currentProblem.FixedWeight);
                    RefreshScaleImmediately();
                }

                return;
            }

            if (showingSuccess || placedExpression != lastPlacedExpression)
            {
                showingSuccess = false;
                lastPlacedExpression = placedExpression;
                lessonCardDisplay.ShowDumbbellTutorialProgress(currentProblem.FixedSide, currentProblem.FixedWeight, placedExpression);
                RefreshScaleImmediately();
            }
        }

        private void ShowProblem(DumbbellBalanceTutorialProblem problem)
        {
            if (lessonCardDisplay == null)
            {
                lessonCardDisplay = FindFirstObjectByType<EquationLessonCardDisplay>();
            }

            if (lessonCardDisplay == null)
            {
                Debug.LogError("問題式を表示するカードが見つかりません。", this);
                return;
            }

            lessonCardDisplay.ShowDumbbellTutorial(problem.FixedSide, problem.FixedWeight);
            lastPlacedExpression = "?";
            lastPlacedTotal = 0;
            showingSuccess = false;
        }

        private IEnumerator SpawnFixedDumbbell(DumbbellBalanceTutorialProblem problem)
        {
            var targetSocket = ResolveSocket(problem.FixedSide);
            var source = ResolveDumbbellSource(problem.FixedWeight);

            if (targetSocket == null || source == null)
            {
                yield break;
            }

            if (spawnedFixedDumbbell != null)
            {
                Destroy(spawnedFixedDumbbell);
            }

            spawnedFixedDumbbell = Instantiate(source.gameObject, targetSocket.transform);
            spawnedFixedDumbbell.name = $"TutorialFixedDumbbell{problem.FixedWeight}";
            spawnedFixedDumbbell.transform.localPosition = Vector3.zero;
            spawnedFixedDumbbell.transform.localRotation = Quaternion.identity;
            spawnedFixedDumbbell.transform.localScale = Vector3.one;

            if (spawnedFixedDumbbell.TryGetComponent(out TMP_Text _))
            {
                Debug.LogWarning("固定ダンベルに TextMeshPro が直接付いています。想定外の元オブジェクトを複製していないか確認してください。", this);
            }

            if (spawnedFixedDumbbell.TryGetComponent(out WeightedDumbbell weightedDumbbell))
            {
                weightedDumbbell.enabled = true;
                weightedDumbbell.Weight = problem.FixedWeight;
            }

            if (spawnedFixedDumbbell.TryGetComponent(out Rigidbody targetRigidbody))
            {
                targetRigidbody.useGravity = false;
                targetRigidbody.isKinematic = true;
            }

            if (spawnedFixedDumbbell.TryGetComponent(out XRGrabInteractable grabInteractable))
            {
                grabInteractable.enabled = true;
            }

            if (!spawnedFixedDumbbell.TryGetComponent(out SocketInitialSelection socketInitialSelection))
            {
                socketInitialSelection = spawnedFixedDumbbell.AddComponent<SocketInitialSelection>();
            }

            socketInitialSelection.enabled = true;

            yield return null;

            if (!targetSocket.hasSelection && spawnedFixedDumbbell.TryGetComponent(out XRGrabInteractable fixedInteractable))
            {
                targetSocket.StartManualInteraction((IXRSelectInteractable)fixedInteractable);
            }

            RefreshScaleImmediately();
        }

        private void RefreshScaleImmediately()
        {
            if (scaleController == null)
            {
                scaleController = FindFirstObjectByType<ScaleController>();
            }

            if (scaleController == null)
            {
                Debug.LogError("天びんの ScaleController が見つかりません。", this);
                return;
            }

            scaleController.maxWeightDifference = tutorialMaxWeightDifference;
            scaleController.RefreshBalance(true);
        }

        private XRSocketInteractor ResolveSocket(BalanceSide side)
        {
            var socket = side == BalanceSide.Left ? leftFixedSocket : rightFixedSocket;
            if (socket != null)
            {
                return socket;
            }

            var socketName = side == BalanceSide.Left ? "LeftScaleSocket01" : "RightScaleSocket01";
            var socketObject = GameObject.Find(socketName);
            if (socketObject != null && socketObject.TryGetComponent(out XRSocketInteractor foundSocket))
            {
                return foundSocket;
            }

            Debug.LogError($"{socketName} が見つかりません。", this);
            return null;
        }

        private void RefreshUnknownSideSockets(BalanceSide unknownSide)
        {
            unknownSideSockets.Clear();

            var scaleName = unknownSide == BalanceSide.Left ? "LeftScale" : "RightScale";
            var scaleObject = GameObject.Find(scaleName);
            if (scaleObject == null)
            {
                Debug.LogError($"{scaleName} が見つかりません。", this);
                return;
            }

            scaleObject.GetComponentsInChildren(true, unknownSideSockets);
        }

        private string BuildPlacedExpression()
        {
            placedWeights.Clear();

            foreach (var socket in unknownSideSockets)
            {
                if (socket == null || !socket.enabled || !socket.hasSelection)
                {
                    continue;
                }

                foreach (var interactable in socket.interactablesSelected)
                {
                    var selectedTransform = interactable.transform;
                    var weightValue = ReadWeight(selectedTransform);
                    if (weightValue > 0)
                    {
                        placedWeights.Add(weightValue);
                    }
                }
            }

            if (placedWeights.Count == 0)
            {
                lastPlacedTotal = 0;
                return "?";
            }

            lastPlacedTotal = 0;
            var expression = placedWeights[0].ToString();
            lastPlacedTotal += placedWeights[0];
            for (var i = 1; i < placedWeights.Count; i++)
            {
                expression += $" + {placedWeights[i]}";
                lastPlacedTotal += placedWeights[i];
            }

            return expression;
        }

        private static int ReadWeight(Transform selectedTransform)
        {
            var weightedDumbbell = selectedTransform.GetComponentInParent<WeightedDumbbell>();
            if (weightedDumbbell != null)
            {
                return Mathf.RoundToInt(weightedDumbbell.Weight);
            }

            var selectedRigidbody = selectedTransform.GetComponentInParent<Rigidbody>();
            return selectedRigidbody != null ? Mathf.RoundToInt(selectedRigidbody.mass) : 0;
        }

        private XRGrabInteractable ResolveDumbbellSource(int weight)
        {
            var index = weight - 1;
            if (index >= 0 && index < dumbbellSourcesByWeight.Length && dumbbellSourcesByWeight[index] != null)
            {
                var registeredSource = dumbbellSourcesByWeight[index];
                var registeredWeight = registeredSource.GetComponent<WeightedDumbbell>();
                if (registeredWeight != null && Mathf.RoundToInt(registeredWeight.Weight) == weight)
                {
                    return registeredSource;
                }

                Debug.LogWarning($"登録済みダンベルの重さが {weight} と一致しません。シーン内から重さで探し直します。", this);
            }

            foreach (var weightedDumbbell in Resources.FindObjectsOfTypeAll<WeightedDumbbell>())
            {
                if (weightedDumbbell == null || weightedDumbbell.gameObject == spawnedFixedDumbbell)
                {
                    continue;
                }

                if (!weightedDumbbell.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (Mathf.RoundToInt(weightedDumbbell.Weight) != weight)
                {
                    continue;
                }

                if (weightedDumbbell.TryGetComponent(out XRGrabInteractable source))
                {
                    return source;
                }
            }

            Debug.LogError($"重さ {weight} のダンベルが見つかりません。", this);
            return null;
        }
    }
}
