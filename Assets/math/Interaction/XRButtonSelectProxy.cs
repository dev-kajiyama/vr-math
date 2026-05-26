using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VrMath.Interaction
{
    /// <summary>
    /// World Space UI の Button を XR select でも押せるようにする小さな橋渡しです。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class XRButtonSelectProxy : MonoBehaviour
    {
        private Button button;
        private BoxCollider boxCollider;
        private XRSimpleInteractable interactable;
        private UnityAction overrideAction;

        private void Awake()
        {
            button = GetComponent<Button>();
            boxCollider = GetComponent<BoxCollider>();
            interactable = GetComponent<XRSimpleInteractable>();
            RefreshCollider();
        }

        private void OnEnable()
        {
            if (interactable == null)
            {
                interactable = GetComponent<XRSimpleInteractable>();
            }

            interactable.selectEntered.AddListener(OnSelectEntered);
            RefreshCollider();
        }

        private void OnDisable()
        {
            if (interactable != null)
            {
                interactable.selectEntered.RemoveListener(OnSelectEntered);
            }
        }

        public void RefreshCollider()
        {
            if (boxCollider == null)
            {
                boxCollider = GetComponent<BoxCollider>();
            }

            var rectTransform = transform as RectTransform;
            var size = rectTransform != null ? rectTransform.rect.size : new Vector2(100f, 40f);

            boxCollider.isTrigger = false;
            boxCollider.center = Vector3.zero;
            boxCollider.size = new Vector3(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y), 20f);

            if (interactable == null)
            {
                interactable = GetComponent<XRSimpleInteractable>();
            }

            if (interactable != null && !interactable.colliders.Contains(boxCollider))
            {
                interactable.colliders.Add(boxCollider);
            }
        }

        public void SetOverrideAction(UnityAction action)
        {
            overrideAction = action;
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (button != null && button.isActiveAndEnabled && button.interactable)
            {
                if (overrideAction != null)
                {
                    overrideAction.Invoke();
                    return;
                }

                button.onClick.Invoke();
            }
        }
    }
}
