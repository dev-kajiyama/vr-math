using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace VrMath.Interaction
{
    /// <summary>
    /// XRSocketInteractor が hover / select しているかを、ゲーム中に見える半透明マーカーで表示します。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRSocketInteractor))]
    public sealed class SocketHoverVisualIndicator : MonoBehaviour
    {
        private const string MarkerName = "Socket Hover Visual";

        [SerializeField, Tooltip("何も入っていない時も薄く表示します。")]
        private bool showIdleMarker;

        [SerializeField, Tooltip("Collider 半径に対する表示マーカーの倍率です。")]
        private float radiusMultiplier = 1f;

        [SerializeField, Tooltip("重りが socket 判定に入った時の色です。")]
        private Color hoverColor = new(0.1f, 0.55f, 1f, 0.35f);

        [SerializeField, Tooltip("重りが socket に吸着している時の色です。")]
        private Color selectedColor = new(0.15f, 1f, 0.45f, 0.5f);

        [SerializeField, Tooltip("何も入っていない時の色です。showIdleMarker が有効な時だけ使います。")]
        private Color idleColor = new(0.1f, 0.55f, 1f, 0.08f);

        private XRSocketInteractor socket;
        private Renderer markerRenderer;
        private Material markerMaterial;

        private void Awake()
        {
            socket = GetComponent<XRSocketInteractor>();
            EnsureMarker();
            UpdateVisual();
        }

        private void LateUpdate()
        {
            UpdateVisual();
        }

        private void OnDestroy()
        {
            if (markerMaterial != null)
            {
                Destroy(markerMaterial);
            }
        }

        private void EnsureMarker()
        {
            var existing = transform.Find(MarkerName);
            var marker = existing != null ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = MarkerName;
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localRotation = Quaternion.identity;

            if (marker.TryGetComponent(out Collider markerCollider))
            {
                Destroy(markerCollider);
            }

            markerRenderer = marker.GetComponent<Renderer>();
            markerMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                name = "Socket Hover Visual Material",
                hideFlags = HideFlags.DontSave
            };
            ConfigureTransparentMaterial(markerMaterial);
            markerRenderer.sharedMaterial = markerMaterial;

            var radius = ResolveSocketRadius();
            marker.transform.localScale = Vector3.one * radius * 2f * Mathf.Max(0.05f, radiusMultiplier);
        }

        private float ResolveSocketRadius()
        {
            if (TryGetComponent(out SphereCollider sphere))
            {
                return sphere.radius;
            }

            if (TryGetComponent(out Collider collider))
            {
                var size = collider.bounds.size;
                return Mathf.Max(size.x, size.y, size.z) * 0.5f;
            }

            return 0.2f;
        }

        private void UpdateVisual()
        {
            if (socket == null || markerRenderer == null)
            {
                return;
            }

            if (socket.hasSelection)
            {
                SetVisible(selectedColor, true);
                return;
            }

            if (socket.hasHover)
            {
                SetVisible(hoverColor, true);
                return;
            }

            SetVisible(idleColor, showIdleMarker);
        }

        private void SetVisible(Color color, bool visible)
        {
            markerRenderer.enabled = visible;
            if (markerMaterial != null)
            {
                markerMaterial.color = color;
                SetMaterialColor(markerMaterial, color);
            }
        }

        private static void ConfigureTransparentMaterial(Material material)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }
    }
}
