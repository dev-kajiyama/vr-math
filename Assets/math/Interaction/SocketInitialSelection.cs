using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VrMath.Interaction
{
    /// <summary>
    /// 教材開始時点で、式に対応する物体を XRSocketInteractor に正式に差し込んだ状態にします。
    /// 見た目だけの親子付けではなく、ソケットの選択状態を作るため、天びん計算も通常のソケット判定で動きます。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class SocketInitialSelection : MonoBehaviour
    {
        [SerializeField, Tooltip("開始時にこのソケットへ差し込みます。未設定なら親から探します。")]
        private XRSocketInteractor socketInteractor;

        [SerializeField, Tooltip("ソケットへ差し込む対象です。未設定なら同じ GameObject から探します。")]
        private XRGrabInteractable interactable;

        [SerializeField, Tooltip("開始時にソケットの位置と回転へ合わせます。")]
        private bool snapToSocketPose = true;

        [SerializeField, Tooltip("開始時に Rigidbody を kinematic にして、教材用の固定物として扱います。")]
        private bool keepKinematic = true;

        private IEnumerator Start()
        {
            if (!ResolveReferences())
            {
                Debug.LogError("開始時に差し込むソケットまたは対象が見つかりません。", this);
                yield break;
            }

            yield return null;
            SelectIntoSocket();
        }

        private bool ResolveReferences()
        {
            if (interactable == null)
            {
                interactable = GetComponent<XRGrabInteractable>();
            }

            if (socketInteractor == null)
            {
                socketInteractor = GetComponentInParent<XRSocketInteractor>();
            }

            return socketInteractor != null && interactable != null;
        }

        private void SelectIntoSocket()
        {
            if (socketInteractor.hasSelection)
            {
                return;
            }

            if (snapToSocketPose)
            {
                var attachTransform = socketInteractor.attachTransform != null ? socketInteractor.attachTransform : socketInteractor.transform;
                transform.SetPositionAndRotation(attachTransform.position, attachTransform.rotation);
            }

            if (TryGetComponent(out Rigidbody targetRigidbody))
            {
                targetRigidbody.useGravity = false;
                if (keepKinematic)
                {
                    targetRigidbody.isKinematic = true;
                }
            }

            socketInteractor.StartManualInteraction((IXRSelectInteractable)interactable);
        }
    }
}
