using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class BalanceLabDesignUnifier
{
    private const string MaterialFolder = "Assets/math/Materials";
    private const string DesignRootName = "BalanceLabDesign";

    [MenuItem("Tools/Math/Unify Balance Lab Design")]
    public static void UnifyBalanceLabDesign()
    {
        Directory.CreateDirectory(MaterialFolder);

        var silver = CreateOrUpdateMaterial("Balance_Lab_Silver", new Color(0.72f, 0.75f, 0.78f, 1f), 1f, 0.88f);
        var darkMetal = CreateOrUpdateMaterial("Balance_Lab_DarkMetal", new Color(0.12f, 0.15f, 0.18f, 1f), 0.9f, 0.72f);
        var panel = CreateOrUpdateMaterial("Balance_Lab_PanelBlueGrey", new Color(0.18f, 0.26f, 0.34f, 1f), 0.4f, 0.65f);
        var glassBlue = CreateOrUpdateMaterial("Balance_Lab_BlueGlass", new Color(0.16f, 0.55f, 0.92f, 0.75f), 0.05f, 0.92f);
        var glow = CreateOrUpdateMaterial("Balance_Lab_BlueGlow", new Color(0.08f, 0.62f, 1f, 1f), 0f, 0.35f, true, new Color(0.05f, 0.55f, 1f, 1f), 3.4f);

        var scaleRoot = GameObject.Find("OverlapScale");
        if (scaleRoot == null)
        {
            throw new InvalidOperationException("OverlapScale was not found in the active scene.");
        }

        Undo.RegisterFullObjectHierarchyUndo(scaleRoot, "Unify balance lab design");
        ApplyBalanceMaterials(scaleRoot, silver, darkMetal);
        BuildBalanceGlowAccents(scaleRoot.transform, glow);

        var oldChest = GameObject.Find("Old Chest");
        if (oldChest != null)
        {
            Undo.RegisterFullObjectHierarchyUndo(oldChest, "Unify old chest lab design");
            ApplyStorageMaterials(oldChest, panel, silver, glassBlue);
        }

        HideTemplateSampleUi();

        EditorSceneManager.MarkSceneDirty(scaleRoot.scene);
        EditorSceneManager.SaveScene(scaleRoot.scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Unified balance scene design: metallic lab scale, blue glow rings, and lab-style storage materials.");
    }

    private static Material CreateOrUpdateMaterial(
        string materialName,
        Color color,
        float metallic,
        float smoothness,
        bool emission = false,
        Color emissionColor = default,
        float emissionIntensity = 1f)
    {
        var path = $"{MaterialFolder}/{materialName}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader)
            {
                name = materialName
            };
            AssetDatabase.CreateAsset(material, path);
        }

        SetColor(material, color);
        SetFloatIfPresent(material, "_Metallic", metallic);
        SetFloatIfPresent(material, "_Smoothness", smoothness);
        SetFloatIfPresent(material, "_Glossiness", smoothness);

        if (emission)
        {
            var hdr = emissionColor * Mathf.Max(1f, emissionIntensity);
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", hdr);
            }

            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            material.DisableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ApplyBalanceMaterials(GameObject root, Material silver, Material darkMetal)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var path = GetPath(renderer.transform).ToLowerInvariant();
            if (path.Contains("/canvas", StringComparison.Ordinal))
            {
                continue;
            }

            var localPath = path.StartsWith("overlapscale/", StringComparison.Ordinal)
                ? path["overlapscale/".Length..]
                : renderer.name.ToLowerInvariant();
            var isDarkPart = localPath.Contains("chain", StringComparison.Ordinal)
                             || localPath.Contains("leftscale", StringComparison.Ordinal)
                             || localPath.Contains("rightscale", StringComparison.Ordinal)
                             || path.Contains("plate", StringComparison.Ordinal)
                             || path.Contains("tray", StringComparison.Ordinal)
                             || path.Contains("dish", StringComparison.Ordinal);

            SetAllSharedMaterials(renderer, isDarkPart ? darkMetal : silver);
            EditorUtility.SetDirty(renderer);
        }
    }

    private static void ApplyStorageMaterials(GameObject root, Material panel, Material silver, Material glassBlue)
    {
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var path = GetPath(renderer.transform).ToLowerInvariant();
            if (path.Contains("workblock", StringComparison.Ordinal) || path.EndsWith("/x", StringComparison.Ordinal))
            {
                SetAllSharedMaterials(renderer, glassBlue);
            }
            else if (path.Contains("number", StringComparison.Ordinal))
            {
                SetAllSharedMaterials(renderer, silver);
            }
            else
            {
                SetAllSharedMaterials(renderer, panel);
            }

            EditorUtility.SetDirty(renderer);
        }
    }

    private static void HideTemplateSampleUi()
    {
        HideIfFound("UI/Spatial Panel Manipulator Model");
        HideIfFound("UI/Spatial Panel Manipulator UI Examples");
    }

    private static void HideIfFound(string path)
    {
        var target = GameObject.Find(path);
        if (target == null)
        {
            return;
        }

        Undo.RecordObject(target, "Hide template sample UI");
        target.SetActive(false);
        EditorUtility.SetDirty(target);
    }

    private static void BuildBalanceGlowAccents(Transform scaleRoot, Material glowMaterial)
    {
        var existing = scaleRoot.Find(DesignRootName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        DestroyChildIfPresent(scaleRoot.Find("ChainsOriginLeft/LeftScale"), "LeftTrayGlowRing");
        DestroyChildIfPresent(scaleRoot.Find("ChainsOriginRight/RightScale"), "RightTrayGlowRing");

        var designRoot = new GameObject(DesignRootName);
        Undo.RegisterCreatedObjectUndo(designRoot, "Create balance lab design root");
        designRoot.transform.SetParent(scaleRoot, false);
        designRoot.transform.localPosition = Vector3.zero;
        designRoot.transform.localRotation = Quaternion.identity;
        designRoot.transform.localScale = Vector3.one;

        CreateRing("BaseGlowRing", designRoot.transform, new Vector3(0f, 0.035f, 0f), 0.46f, 0.57f, glowMaterial);

        var leftScale = scaleRoot.Find("ChainsOriginLeft/LeftScale");
        var rightScale = scaleRoot.Find("ChainsOriginRight/RightScale");
        if (leftScale != null)
        {
            CreateRing("LeftTrayGlowRing", leftScale, new Vector3(0f, 0.075f, 0f), 0.33f, 0.42f, glowMaterial);
        }

        if (rightScale != null)
        {
            CreateRing("RightTrayGlowRing", rightScale, new Vector3(0f, 0.075f, 0f), 0.33f, 0.42f, glowMaterial);
        }

        var lightObject = new GameObject("BalanceBlueFillLight");
        Undo.RegisterCreatedObjectUndo(lightObject, "Create balance blue fill light");
        lightObject.transform.SetParent(designRoot.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 0.45f, -0.35f);

        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.2f, 0.65f, 1f, 1f);
        light.intensity = 0.45f;
        light.range = 2.6f;
    }

    private static void DestroyChildIfPresent(Transform parent, string childName)
    {
        if (parent == null)
        {
            return;
        }

        var child = parent.Find(childName);
        while (child != null)
        {
            Undo.DestroyObjectImmediate(child.gameObject);
            child = parent.Find(childName);
        }
    }

    private static void CreateRing(string name, Transform parent, Vector3 localPosition, float innerRadius, float outerRadius, Material material)
    {
        var ring = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(ring, $"Create {name}");
        ring.transform.SetParent(parent, false);
        ring.transform.localPosition = localPosition;
        ring.transform.localRotation = Quaternion.identity;
        ring.transform.localScale = Vector3.one;

        var meshFilter = ring.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateAnnulusMesh(name, innerRadius, outerRadius, 96);

        var meshRenderer = ring.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
    }

    private static void SetAllSharedMaterials(Renderer renderer, Material material)
    {
        var slotCount = Math.Max(1, renderer.sharedMaterials.Length);
        renderer.sharedMaterials = Enumerable.Repeat(material, slotCount).ToArray();
    }

    private static Mesh CreateAnnulusMesh(string name, float innerRadius, float outerRadius, int segments)
    {
        var vertices = new Vector3[segments * 2];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[segments * 6];

        for (var i = 0; i < segments; i++)
        {
            var angle = i / (float)segments * Mathf.PI * 2f;
            var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            vertices[i * 2] = direction * innerRadius;
            vertices[i * 2 + 1] = direction * outerRadius;
            uv[i * 2] = new Vector2(0f, i / (float)segments);
            uv[i * 2 + 1] = new Vector2(1f, i / (float)segments);

            var next = (i + 1) % segments;
            var tri = i * 6;
            triangles[tri] = i * 2;
            triangles[tri + 1] = next * 2;
            triangles[tri + 2] = i * 2 + 1;
            triangles[tri + 3] = i * 2 + 1;
            triangles[tri + 4] = next * 2;
            triangles[tri + 5] = next * 2 + 1;
        }

        var mesh = new Mesh
        {
            name = name,
            vertices = vertices,
            uv = uv,
            triangles = triangles
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void SetColor(Material material, Color color)
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

    private static void SetFloatIfPresent(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private static string GetPath(Transform transform)
    {
        if (transform.parent == null)
        {
            return transform.name;
        }

        return $"{GetPath(transform.parent)}/{transform.name}";
    }
}
