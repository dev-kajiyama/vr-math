using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 重りの教材上の重さを保持し、天秤が読む Rigidbody.mass と同期します。
/// 標準分銅として見えるように、子の Renderer へ金属質な材質を適用します。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public sealed class WeightedDumbbell : MonoBehaviour
{
    [SerializeField, Min(0f), Tooltip("天秤計算に使う重りの重さです。")]
    private float weight = 1f;

    [Header("見た目")]
    [SerializeField, Tooltip("未設定なら画像の分銅に近い銀黒の金属材質を自動生成します。")]
    private Material weightMaterial;

    [SerializeField, Tooltip("標準分銅らしい暗めの銀色です。")]
    private Color metalColor = new(0.56f, 0.55f, 0.52f, 1f);

    [SerializeField, Range(0f, 1f), Tooltip("金属度です。")]
    private float metallic = 1f;

    [SerializeField, Range(0f, 1f), Tooltip("表面のなめらかさです。")]
    private float smoothness = 0.82f;

    private static Material sharedGeneratedMaterial;

    /// <summary>
    /// 天秤計算に使う重りの重さです。
    /// 値を変更すると、この GameObject の Rigidbody.mass にも反映されます。
    /// </summary>
    public float Weight
    {
        get => weight;
        set
        {
            weight = Mathf.Max(0f, value);
            ApplyToRigidbody();
        }
    }

    private void Awake()
    {
        ApplyToRigidbody();
        ApplyMetallicAppearance();
    }

    private void OnValidate()
    {
        weight = Mathf.Max(0f, weight);
        ApplyToRigidbody();
        ApplyMetallicAppearance();
    }

    private void ApplyToRigidbody()
    {
        if (TryGetComponent(out Rigidbody targetRigidbody))
        {
            targetRigidbody.mass = weight;
        }
    }

    private void ApplyMetallicAppearance()
    {
        var material = weightMaterial != null ? weightMaterial : GetOrCreateGeneratedMaterial();
        if (material == null)
        {
            return;
        }

        foreach (var meshRenderer in GetComponentsInChildren<Renderer>(true))
        {
            meshRenderer.sharedMaterial = material;
        }
    }

    private Material GetOrCreateGeneratedMaterial()
    {
        if (sharedGeneratedMaterial == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                return null;
            }

            sharedGeneratedMaterial = new Material(shader)
            {
                name = "Generated Standard Weight Metal",
                hideFlags = HideFlags.DontSave
            };
        }

        SetMaterialProperties(sharedGeneratedMaterial);
        return sharedGeneratedMaterial;
    }

    private void SetMaterialProperties(Material material)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", metalColor);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", metalColor);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", metallic);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", smoothness);
        }

        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
    }
}
