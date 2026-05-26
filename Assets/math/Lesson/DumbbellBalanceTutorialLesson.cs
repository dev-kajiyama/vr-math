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
    /// 小さい分銅だけで均衡を作るチュートリアル問題を、シーン上の天びんへ配置します。
    /// 固定側には重さ1の分銅を目標数だけ複製し、学習者も同じ分銅を複数置いて均衡を作ります。
    /// </summary>
    public sealed class DumbbellBalanceTutorialLesson : MonoBehaviour
    {
        [SerializeField, Tooltip("最初から分銅を置いておく辺です。")]
        private BalanceSide fixedSide = BalanceSide.Left;

        [SerializeField, Min(1), Tooltip("最初から置いておく分銅の合計数です。重さ1の分銅をこの数だけ置きます。")]
        private int fixedWeight = 3;

        [SerializeField, Tooltip("左辺の固定分銅を入れる最初のソケットです。未設定なら LeftScale 配下を探します。")]
        private XRSocketInteractor leftFixedSocket;

        [SerializeField, Tooltip("右辺の固定分銅を入れる最初のソケットです。未設定なら RightScale 配下を探します。")]
        private XRSocketInteractor rightFixedSocket;

        [SerializeField, Tooltip("問題式を表示するカードです。")]
        private EquationLessonCardDisplay lessonCardDisplay;

        [SerializeField, Tooltip("天びんの傾きを管理するコントローラーです。")]
        private ScaleController scaleController;

        [SerializeField, Tooltip("複製元にする重さ1の小さい分銅です。未設定なら StandardWeight_01 などを探します。")]
        private XRGrabInteractable unitWeightSource;

        [SerializeField, Min(0.01f), Tooltip("このチュートリアル中に、何kg差で最大傾きにするか。")]
        private float tutorialMaxWeightDifference = 3f;

        private readonly List<GameObject> spawnedFixedWeights = new();
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
        /// 現在の設定値で、式表示と固定側分銅のソケット装填を行います。
        /// </summary>
        public void SetupProblem()
        {
            currentProblem = new DumbbellBalanceTutorialProblem(fixedSide, fixedWeight);
            RefreshUnknownSideSockets(currentProblem.UnknownSide);
            ShowProblem(currentProblem);
            StartCoroutine(SpawnFixedWeights(currentProblem));
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
                    lessonCardDisplay.ShowDumbbellTutorialCorrect(currentProblem.FixedSide, currentProblem.FixedWeight, placedExpression);
                    RefreshScaleImmediately();
                }

                return;
            }

            if (showingSuccess || placedExpression != lastPlacedExpression)
            {
                showingSuccess = false;
                lastPlacedExpression = placedExpression;
                lessonCardDisplay.ShowDumbbellTutorialProgress(currentProblem.FixedSide, currentProblem.FixedWeight, placedExpression, lastPlacedTotal);
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

        private IEnumerator SpawnFixedWeights(DumbbellBalanceTutorialProblem problem)
        {
            var targetSockets = ResolveSockets(problem.FixedSide);
            var source = ResolveUnitWeightSource();

            if (targetSockets.Count == 0 || source == null)
            {
                yield break;
            }

            foreach (var fixedWeightObject in spawnedFixedWeights)
            {
                if (fixedWeightObject != null)
                {
                    Destroy(fixedWeightObject);
                }
            }

            spawnedFixedWeights.Clear();

            var spawnCount = Mathf.Min(problem.FixedWeight, targetSockets.Count);
            if (spawnCount < problem.FixedWeight)
            {
                Debug.LogWarning($"固定側ソケットが {targetSockets.Count} 個しかないため、分銅 {problem.FixedWeight} 個のうち {spawnCount} 個だけ置きます。", this);
            }

            for (var i = 0; i < spawnCount; i++)
            {
                var targetSocket = targetSockets[i];
                var spawnedWeight = Instantiate(source.gameObject);
                spawnedWeight.name = $"TutorialFixedWeight_{i + 1:00}";
                spawnedWeight.transform.SetParent(targetSocket.transform, false);
                spawnedWeight.transform.localPosition = Vector3.zero;
                MatchSourceScale(spawnedWeight.transform, source.transform);

                if (spawnedWeight.TryGetComponent(out WeightedDumbbell weightedDumbbell))
                {
                    weightedDumbbell.enabled = true;
                    weightedDumbbell.Weight.Value = 1f;
                }

                if (spawnedWeight.TryGetComponent(out Rigidbody targetRigidbody))
                {
                    targetRigidbody.mass = 1f;
                    targetRigidbody.useGravity = false;
                    targetRigidbody.isKinematic = true;
                }

                if (spawnedWeight.TryGetComponent(out XRGrabInteractable grabInteractable))
                {
                    grabInteractable.enabled = true;
                }

                if (!spawnedWeight.TryGetComponent(out SocketInitialSelection socketInitialSelection))
                {
                    socketInitialSelection = spawnedWeight.AddComponent<SocketInitialSelection>();
                }

                socketInitialSelection.enabled = true;
                spawnedFixedWeights.Add(spawnedWeight);
            }

            yield return null;

            for (var i = 0; i < spawnedFixedWeights.Count; i++)
            {
                var targetSocket = targetSockets[i];
                var spawnedWeight = spawnedFixedWeights[i];
                if (!targetSocket.hasSelection && spawnedWeight.TryGetComponent(out XRGrabInteractable fixedInteractable))
                {
                    targetSocket.StartManualInteraction((IXRSelectInteractable)fixedInteractable);
                    MatchSourceScale(spawnedWeight.transform, source.transform);
                }
            }

            yield return null;

            foreach (var fixedWeightObject in spawnedFixedWeights)
            {
                if (fixedWeightObject != null)
                {
                    MatchSourceScale(fixedWeightObject.transform, source.transform);
                }
            }

            RefreshScaleImmediately();
        }

        private static void MatchSourceScale(Transform target, Transform source)
        {
            SetWorldScale(target, source.lossyScale);
        }

        private static void SetWorldScale(Transform target, Vector3 worldScale)
        {
            var parent = target.parent;
            if (parent == null)
            {
                target.localScale = worldScale;
                return;
            }

            var parentScale = parent.lossyScale;
            target.localScale = new Vector3(
                SafeDivide(worldScale.x, parentScale.x),
                SafeDivide(worldScale.y, parentScale.y),
                SafeDivide(worldScale.z, parentScale.z));
        }

        private static float SafeDivide(float value, float divisor)
        {
            return Mathf.Abs(divisor) > Mathf.Epsilon ? value / divisor : value;
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

        private List<XRSocketInteractor> ResolveSockets(BalanceSide side)
        {
            var sockets = new List<XRSocketInteractor>();
            var socket = side == BalanceSide.Left ? leftFixedSocket : rightFixedSocket;
            if (socket != null)
            {
                sockets.Add(socket);
            }

            var scaleName = side == BalanceSide.Left ? "LeftScale" : "RightScale";
            var scaleObject = GameObject.Find(scaleName);
            if (scaleObject != null)
            {
                foreach (var foundSocket in scaleObject.GetComponentsInChildren<XRSocketInteractor>(true))
                {
                    if (!sockets.Contains(foundSocket))
                    {
                        sockets.Add(foundSocket);
                    }
                }
            }

            sockets.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            if (sockets.Count == 0)
            {
                Debug.LogError($"{scaleName} のソケットが見つかりません。", this);
            }

            return sockets;
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
                return Mathf.RoundToInt(weightedDumbbell.Weight.Value);
            }

            var selectedRigidbody = selectedTransform.GetComponentInParent<Rigidbody>();
            return selectedRigidbody != null ? Mathf.RoundToInt(selectedRigidbody.mass) : 0;
        }

        private XRGrabInteractable ResolveUnitWeightSource()
        {
            if (unitWeightSource != null)
            {
                var registeredWeight = unitWeightSource.GetComponent<WeightedDumbbell>();
                if (registeredWeight != null && Mathf.RoundToInt(registeredWeight.Weight.Value) == 1)
                {
                    return unitWeightSource;
                }

                Debug.LogWarning("登録済み分銅の重さが 1 と一致しません。シーン内から探し直します。", this);
            }

            foreach (var weightedDumbbell in Resources.FindObjectsOfTypeAll<WeightedDumbbell>())
            {
                if (weightedDumbbell == null || spawnedFixedWeights.Contains(weightedDumbbell.gameObject))
                {
                    continue;
                }

                if (!weightedDumbbell.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (Mathf.RoundToInt(weightedDumbbell.Weight.Value) != 1)
                {
                    continue;
                }

                if (weightedDumbbell.name.StartsWith("StandardWeight_") && weightedDumbbell.TryGetComponent(out XRGrabInteractable source))
                {
                    return source;
                }
            }

            Debug.LogError("複製元にできる重さ1の小さい分銅が見つかりません。", this);
            return null;
        }
    }
}
