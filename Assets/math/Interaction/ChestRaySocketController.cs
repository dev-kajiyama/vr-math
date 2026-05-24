using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VrMath.Interaction
{
    /// <summary>
    /// 右手のレイが箱を指しているときだけ入力で箱を開き、箱内ソケットに物が入ったら閉じます。
    /// </summary>
    public sealed class ChestRaySocketController : MonoBehaviour
    {
        [Header("箱")]
        [SerializeField, Tooltip("箱全体のルート。Ray がこの Transform または子に当たったときだけ開きます。")]
        private Transform chestRoot;

        [SerializeField, Tooltip("蓋を開閉する Animator。通常は LidPivot / pivot に付けます。")]
        private Animator lidAnimator;

        [SerializeField, Tooltip("開く Animator State 名。")]
        private string openStateName = "Open";

        [SerializeField, Tooltip("閉じる Animator State 名。")]
        private string closeStateName = "Close";

        [Header("右手 Ray 入力")]
        [SerializeField, Tooltip("右手の XRRayInteractor。現在 Ray が当たっている対象を調べます。")]
        private XRRayInteractor rightRayInteractor;

        [SerializeField, Tooltip("箱を開く右手ボタンの Input Action。例: RightHand Interaction/Activate。")]
        private InputActionReference openButtonAction;

        [SerializeField, Tooltip("XRRayInteractor が未設定のときに使う予備の Ray 発射元。")]
        private Transform fallbackRayOrigin;

        [SerializeField, Tooltip("予備 Raycast の最大距離。")]
        private float fallbackRayDistance = 10f;

        [SerializeField, Tooltip("予備 Raycast で当たり判定する Layer。")]
        private LayerMask fallbackRayMask = ~0;

        [Header("箱内ソケット")]
        [SerializeField, Tooltip("箱の中に置く XRSocketInteractor。何かが吸着したら閉じます。")]
        private XRSocketInteractor innerSocket;

        private bool isOpen;

        private void Reset()
        {
            chestRoot = transform;
            lidAnimator = GetComponentInChildren<Animator>();
            innerSocket = GetComponentInChildren<XRSocketInteractor>();
        }

        private void OnEnable()
        {
            if (openButtonAction != null && openButtonAction.action != null)
            {
                openButtonAction.action.performed += OnOpenButtonPerformed;
                openButtonAction.action.Enable();
            }

            if (innerSocket != null)
            {
                innerSocket.selectEntered.AddListener(OnSocketSelectEntered);
            }
        }

        private void OnDisable()
        {
            if (openButtonAction != null && openButtonAction.action != null)
            {
                openButtonAction.action.performed -= OnOpenButtonPerformed;
            }

            if (innerSocket != null)
            {
                innerSocket.selectEntered.RemoveListener(OnSocketSelectEntered);
            }
        }

        /// <summary>
        /// UnityEvent からも呼べる、Ray が箱を指しているときだけ開く操作です。
        /// </summary>
        public void OpenIfRayHitsChest()
        {
            if (IsRayHittingChest())
            {
                Open();
            }
        }

        /// <summary>
        /// 箱を開きます。
        /// </summary>
        public void Open()
        {
            if (isOpen)
            {
                return;
            }

            isOpen = true;
            PlayLidState(openStateName);
        }

        /// <summary>
        /// 箱を閉じます。
        /// </summary>
        public void Close()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            PlayLidState(closeStateName);
        }

        private void OnOpenButtonPerformed(InputAction.CallbackContext context)
        {
            OpenIfRayHitsChest();
        }

        private void OnSocketSelectEntered(SelectEnterEventArgs args)
        {
            Close();
        }

        private bool IsRayHittingChest()
        {
            if (rightRayInteractor != null && rightRayInteractor.TryGetCurrent3DRaycastHit(out var xrHit))
            {
                return IsChestCollider(xrHit.collider);
            }

            if (fallbackRayOrigin == null)
            {
                return false;
            }

            var ray = new Ray(fallbackRayOrigin.position, fallbackRayOrigin.forward);
            if (Physics.Raycast(ray, out var physicsHit, fallbackRayDistance, fallbackRayMask, QueryTriggerInteraction.Ignore))
            {
                return IsChestCollider(physicsHit.collider);
            }

            return false;
        }

        private bool IsChestCollider(Collider hitCollider)
        {
            if (hitCollider == null || chestRoot == null)
            {
                return false;
            }

            var hitTransform = hitCollider.transform;
            return hitTransform == chestRoot || hitTransform.IsChildOf(chestRoot);
        }

        private void PlayLidState(string stateName)
        {
            if (lidAnimator == null)
            {
                Debug.LogWarning("箱の Animator が設定されていないため、蓋を開閉できません。", this);
                return;
            }

            if (!string.IsNullOrWhiteSpace(stateName))
            {
                lidAnimator.Play(stateName, 0, 0f);
            }
        }
    }
}
