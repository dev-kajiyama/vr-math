using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Usage:
/// Tools > BalanceBoard > Create Hex Bolt Prefab を実行すると、
/// 厚みのある六角形ボルトのPrefabを Assets/BalanceBoard/Prefabs/HexBolt_Orange.prefab に生成します。
/// バランス台の支点前面などに貼り付ける用途を想定し、ローカルZ方向に厚みがあります。
/// </summary>
public static class CreateHexBoltPrefab
{
    private const string RootFolder = "Assets/BalanceBoard";
    private const string PrefabFolder = RootFolder + "/Prefabs";
    private const string MaterialFolder = RootFolder + "/Materials";
    private const string MeshFolder = RootFolder + "/Meshes";

    private const string PrefabPath = PrefabFolder + "/HexBolt_Orange.prefab";
    private const string MeshPath = MeshFolder + "/HexBoltMesh.asset";
    private const string OrangeMaterialPath = MaterialFolder + "/MAT_BalanceBoard_Orange.mat";

    [MenuItem("Tools/BalanceBoard/Create Hex Bolt Prefab")]
    public static void CreatePrefab()
    {
        EnsureFolders();

        var material = CreateOrUpdateMaterial(
            OrangeMaterialPath,
            new Color(1f, 0.48f, 0.02f, 1f),
            0f,
            0.42f);
        var mesh = CreateOrUpdateHexBoltMesh();

        var root = new GameObject("HexBolt_Orange");
        var meshFilter = root.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        var renderer = root.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;

        var collider = root.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
        collider.convex = true;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out var success);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!success || prefab == null)
        {
            Debug.LogError($"Failed to create hex bolt prefab at {PrefabPath}");
            return;
        }

        Selection.activeObject = prefab;
        Debug.Log($"Created hex bolt prefab: {PrefabPath}");
    }

    private static void EnsureFolders()
    {
        EnsureFolder(RootFolder);
        EnsureFolder(PrefabFolder);
        EnsureFolder(MaterialFolder);
        EnsureFolder(MeshFolder);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        var folderName = Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent))
        {
            return;
        }

        EnsureFolder(parent);
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    private static Material CreateOrUpdateMaterial(string path, Color color, float metallic, float smoothness)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(FindLitShader())
            {
                name = Path.GetFileNameWithoutExtension(path)
            };
            AssetDatabase.CreateAsset(material, path);
        }

        if (material.shader == null)
        {
            material.shader = FindLitShader();
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        SetFloatIfPresent(material, "_Metallic", metallic);
        SetFloatIfPresent(material, "_Smoothness", smoothness);
        SetFloatIfPresent(material, "_Glossiness", smoothness);
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Shader FindLitShader()
    {
        return Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
    }

    private static void SetFloatIfPresent(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private static Mesh CreateOrUpdateHexBoltMesh()
    {
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (mesh == null)
        {
            mesh = new Mesh
            {
                name = "HexBoltMesh"
            };
            AssetDatabase.CreateAsset(mesh, MeshPath);
        }

        const int sides = 6;
        const float radius = 0.16f;
        const float bevelRadius = 0.135f;
        const float halfDepth = 0.045f;
        const float bevelDepth = 0.016f;

        var vertices = new Vector3[sides * 4 + 2];
        var triangles = new int[sides * 24];

        vertices[0] = new Vector3(0f, 0f, -halfDepth - bevelDepth);
        vertices[1] = new Vector3(0f, 0f, halfDepth + bevelDepth);

        for (var i = 0; i < sides; i++)
        {
            var angle = Mathf.PI * 2f * i / sides + Mathf.PI / 6f;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            vertices[2 + i] = new Vector3(direction.x * bevelRadius, direction.y * bevelRadius, -halfDepth - bevelDepth);
            vertices[2 + sides + i] = new Vector3(direction.x * radius, direction.y * radius, -halfDepth);
            vertices[2 + sides * 2 + i] = new Vector3(direction.x * radius, direction.y * radius, halfDepth);
            vertices[2 + sides * 3 + i] = new Vector3(direction.x * bevelRadius, direction.y * bevelRadius, halfDepth + bevelDepth);
        }

        var triangleIndex = 0;
        for (var i = 0; i < sides; i++)
        {
            var next = (i + 1) % sides;

            var backCenter = 0;
            var frontCenter = 1;
            var backInner = 2 + i;
            var nextBackInner = 2 + next;
            var backOuter = 2 + sides + i;
            var nextBackOuter = 2 + sides + next;
            var frontOuter = 2 + sides * 2 + i;
            var nextFrontOuter = 2 + sides * 2 + next;
            var frontInner = 2 + sides * 3 + i;
            var nextFrontInner = 2 + sides * 3 + next;

            triangles[triangleIndex++] = backCenter;
            triangles[triangleIndex++] = nextBackInner;
            triangles[triangleIndex++] = backInner;

            triangles[triangleIndex++] = backInner;
            triangles[triangleIndex++] = nextBackInner;
            triangles[triangleIndex++] = nextBackOuter;
            triangles[triangleIndex++] = backInner;
            triangles[triangleIndex++] = nextBackOuter;
            triangles[triangleIndex++] = backOuter;

            triangles[triangleIndex++] = backOuter;
            triangles[triangleIndex++] = nextBackOuter;
            triangles[triangleIndex++] = nextFrontOuter;
            triangles[triangleIndex++] = backOuter;
            triangles[triangleIndex++] = nextFrontOuter;
            triangles[triangleIndex++] = frontOuter;

            triangles[triangleIndex++] = frontOuter;
            triangles[triangleIndex++] = nextFrontOuter;
            triangles[triangleIndex++] = nextFrontInner;
            triangles[triangleIndex++] = frontOuter;
            triangles[triangleIndex++] = nextFrontInner;
            triangles[triangleIndex++] = frontInner;

            triangles[triangleIndex++] = frontCenter;
            triangles[triangleIndex++] = frontInner;
            triangles[triangleIndex++] = nextFrontInner;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        return mesh;
    }
}
