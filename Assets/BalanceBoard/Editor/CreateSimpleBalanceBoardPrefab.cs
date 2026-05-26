using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Usage:
/// Unity Editor のメニューから Tools > BalanceBoard > Create Simple Balance Board Prefab を実行すると、
/// Starter Assets ThirdPerson URP の Playground に合う白・グレー・オレンジ基調のシンプルな
/// バランス台Prefabを Assets/BalanceBoard/Prefabs/BalanceBoard_Simple.prefab に生成します。
/// 既存のPrefab、Material、Meshがある場合は再利用または上書き更新します。
/// </summary>
public static class CreateSimpleBalanceBoardPrefab
{
    private const string RootFolder = "Assets/BalanceBoard";
    private const string EditorFolder = RootFolder + "/Editor";
    private const string PrefabFolder = RootFolder + "/Prefabs";
    private const string MaterialFolder = RootFolder + "/Materials";
    private const string MeshFolder = RootFolder + "/Meshes";

    private const string PrefabPath = PrefabFolder + "/BalanceBoard_Simple.prefab";
    private const string WhiteMaterialPath = MaterialFolder + "/MAT_BalanceBoard_White.mat";
    private const string DarkGrayMaterialPath = MaterialFolder + "/MAT_BalanceBoard_DarkGray.mat";
    private const string OrangeMaterialPath = MaterialFolder + "/MAT_BalanceBoard_Orange.mat";
    private const string TriangularSupportMeshPath = MeshFolder + "/TriangularSupportMesh.asset";

    [MenuItem("Tools/BalanceBoard/Create Simple Balance Board Prefab")]
    public static void CreatePrefab()
    {
        EnsureFolders();

        var white = CreateOrUpdateMaterial(
            WhiteMaterialPath,
            new Color(0.92f, 0.94f, 0.94f, 1f),
            0f,
            0.45f);
        var darkGray = CreateOrUpdateMaterial(
            DarkGrayMaterialPath,
            new Color(0.22f, 0.24f, 0.25f, 1f),
            0f,
            0.38f);
        var orange = CreateOrUpdateMaterial(
            OrangeMaterialPath,
            new Color(1f, 0.48f, 0.02f, 1f),
            0f,
            0.42f);
        var supportMesh = CreateOrUpdateTriangularSupportMesh();

        var root = new GameObject("BalanceBoard_Simple");

        CreateBox(
            "Board",
            root.transform,
            new Vector3(0f, 1.18f, 0f),
            new Vector3(6.8f, 0.16f, 0.28f),
            white);

        CreateBox(
            "Left_OrangeCap",
            root.transform,
            new Vector3(-3.46f, 1.18f, 0f),
            new Vector3(0.16f, 0.22f, 0.34f),
            orange);

        CreateBox(
            "Right_OrangeCap",
            root.transform,
            new Vector3(3.46f, 1.18f, 0f),
            new Vector3(0.16f, 0.22f, 0.34f),
            orange);

        CreateTriangularSupport(
            root.transform,
            supportMesh,
            new Vector3(0f, 0.18f, 0f),
            white);

        CreateBox(
            "Base",
            root.transform,
            new Vector3(0f, 0.08f, 0f),
            new Vector3(1.45f, 0.16f, 0.62f),
            darkGray);

        CreatePivotBolt(
            root.transform,
            new Vector3(0.15f, 1.02f, -0.255f),
            orange);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out var success);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!success || prefab == null)
        {
            Debug.LogError($"Failed to create balance board prefab at {PrefabPath}");
            return;
        }

        Selection.activeObject = prefab;
        Debug.Log($"Created simple balance board prefab: {PrefabPath}");
    }

    private static void EnsureFolders()
    {
        EnsureFolder(RootFolder);
        EnsureFolder(EditorFolder);
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

        SetColor(material, color);
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

    private static Mesh CreateOrUpdateTriangularSupportMesh()
    {
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(TriangularSupportMeshPath);
        if (mesh == null)
        {
            mesh = new Mesh
            {
                name = "TriangularSupportMesh"
            };
            AssetDatabase.CreateAsset(mesh, TriangularSupportMeshPath);
        }

        var bottomHalfWidth = 0.58f;
        var topHalfWidth = 0.13f;
        var halfDepth = 0.24f;
        var height = 0.92f;
        var vertices = new[]
        {
            new Vector3(-bottomHalfWidth, 0f, -halfDepth),
            new Vector3(bottomHalfWidth, 0f, -halfDepth),
            new Vector3(topHalfWidth, height, -halfDepth),
            new Vector3(-topHalfWidth, height, -halfDepth),
            new Vector3(-bottomHalfWidth, 0f, halfDepth),
            new Vector3(bottomHalfWidth, 0f, halfDepth),
            new Vector3(topHalfWidth, height, halfDepth),
            new Vector3(-topHalfWidth, height, halfDepth)
        };

        var triangles = new[]
        {
            0, 3, 2,
            0, 2, 1,
            4, 5, 6,
            4, 6, 7,
            0, 4, 7,
            0, 7, 3,
            1, 2, 6,
            1, 6, 5,
            3, 7, 6,
            3, 6, 2,
            0, 1, 5,
            0, 5, 4
        };

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        return mesh;
    }

    private static GameObject CreateBox(string name, Transform parent, Vector3 localPosition, Vector3 size, Material material)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        box.transform.localPosition = localPosition;
        box.transform.localRotation = Quaternion.identity;
        box.transform.localScale = size;

        var renderer = box.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;

        return box;
    }

    private static void CreateTriangularSupport(Transform parent, Mesh mesh, Vector3 localPosition, Material material)
    {
        var support = new GameObject("TriangularSupport");
        support.transform.SetParent(parent, false);
        support.transform.localPosition = localPosition;
        support.transform.localRotation = Quaternion.identity;
        support.transform.localScale = Vector3.one;

        var meshFilter = support.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        var renderer = support.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;

        var meshCollider = support.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        meshCollider.convex = true;
    }

    private static void CreatePivotBolt(Transform parent, Vector3 localPosition, Material material)
    {
        var bolt = new GameObject("PivotBolt");
        bolt.AddComponent<MeshFilter>().sharedMesh = CreateHexBoltMesh();
        bolt.AddComponent<MeshRenderer>().sharedMaterial = material;
        bolt.AddComponent<BoxCollider>();
        bolt.name = "PivotBolt";
        bolt.transform.SetParent(parent, false);
        bolt.transform.localPosition = localPosition;
        bolt.transform.localRotation = Quaternion.identity;
        bolt.transform.localScale = Vector3.one;
    }

    private static Mesh CreateHexBoltMesh()
    {
        const int sides = 6;
        const float radius = 0.105f;
        const float depth = 0.055f;
        var vertices = new Vector3[sides * 2 + 2];
        var triangles = new int[sides * 12];

        vertices[0] = new Vector3(0f, 0f, -depth * 0.5f);
        vertices[1] = new Vector3(0f, 0f, depth * 0.5f);

        for (var i = 0; i < sides; i++)
        {
            var angle = Mathf.PI * 2f * i / sides + Mathf.PI / 6f;
            var point = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            vertices[i + 2] = new Vector3(point.x, point.y, -depth * 0.5f);
            vertices[i + 2 + sides] = new Vector3(point.x, point.y, depth * 0.5f);
        }

        var triangleIndex = 0;
        for (var i = 0; i < sides; i++)
        {
            var next = (i + 1) % sides;
            var back = i + 2;
            var nextBack = next + 2;
            var front = i + 2 + sides;
            var nextFront = next + 2 + sides;

            triangles[triangleIndex++] = 0;
            triangles[triangleIndex++] = nextBack;
            triangles[triangleIndex++] = back;

            triangles[triangleIndex++] = 1;
            triangles[triangleIndex++] = front;
            triangles[triangleIndex++] = nextFront;

            triangles[triangleIndex++] = back;
            triangles[triangleIndex++] = nextBack;
            triangles[triangleIndex++] = nextFront;
            triangles[triangleIndex++] = back;
            triangles[triangleIndex++] = nextFront;
            triangles[triangleIndex++] = front;
        }

        var mesh = new Mesh
        {
            name = "HexPivotBoltMesh",
            vertices = vertices,
            triangles = triangles
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
