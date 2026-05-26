using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class StandardWeightSetObjectSplitter
{
    private const string MeshOutputFolder = "Assets/math/Models/StandardWeights/SplitMeshes";
    private const string PrefabOutputFolder = "Assets/math/Prefabs";

    [MenuItem("Tools/Math/Split Selected Standard Weight Set Into Four Objects")]
    public static void SplitSelectedStandardWeightSetIntoFourObjects()
    {
        var root = Selection.activeGameObject != null ? Selection.activeGameObject : GameObject.Find("StandardWeight");
        if (root == null)
        {
            throw new InvalidOperationException("Select the StandardWeight set, or keep a GameObject named StandardWeight in the scene.");
        }

        var sourceFilter = root.GetComponentsInChildren<MeshFilter>(true)
            .FirstOrDefault(filter => filter.sharedMesh != null && filter.sharedMesh.triangles.Length > 0);
        if (sourceFilter == null)
        {
            throw new InvalidOperationException($"No source mesh found under {root.name}.");
        }

        var sourceRenderer = sourceFilter.GetComponent<MeshRenderer>();
        if (sourceRenderer == null)
        {
            throw new InvalidOperationException($"No MeshRenderer found on {sourceFilter.name}.");
        }

        Directory.CreateDirectory(MeshOutputFolder);
        Directory.CreateDirectory(PrefabOutputFolder);

        DeleteGeneratedAssets();

        var sourceMesh = sourceFilter.sharedMesh;
        var groups = SplitMeshIntoFourByX(sourceMesh);
        groups.Sort((a, b) => a.Bounds.center.x.CompareTo(b.Bounds.center.x));

        Undo.RegisterFullObjectHierarchyUndo(root, "Split StandardWeight into four objects");

        foreach (var existing in root.GetComponentsInChildren<Transform>(true)
                     .Where(t => t.name.StartsWith("StandardWeight_", StringComparison.Ordinal))
                     .Select(t => t.gameObject)
                     .ToArray())
        {
            Undo.DestroyObjectImmediate(existing);
        }

        sourceRenderer.enabled = false;
        var sourceCollider = sourceFilter.GetComponent<Collider>();
        if (sourceCollider != null)
        {
            sourceCollider.enabled = false;
        }

        var created = new List<GameObject>();
        for (var i = 0; i < groups.Count; i++)
        {
            var group = groups[i];
            var centeredMesh = CreateCenteredMesh(group.Mesh, $"StandardWeight_{i + 1:00}_Mesh", out var center);
            var meshPath = $"{MeshOutputFolder}/StandardWeight_{i + 1:00}.asset";
            AssetDatabase.CreateAsset(centeredMesh, meshPath);

            var weightObject = new GameObject($"StandardWeight_{i + 1:00}");
            Undo.RegisterCreatedObjectUndo(weightObject, "Create split standard weight");
            weightObject.transform.SetParent(sourceFilter.transform.parent, false);
            weightObject.transform.localRotation = sourceFilter.transform.localRotation;
            weightObject.transform.localScale = sourceFilter.transform.localScale;
            weightObject.transform.localPosition = sourceFilter.transform.localPosition
                + sourceFilter.transform.localRotation * Vector3.Scale(sourceFilter.transform.localScale, center);

            var filter = weightObject.AddComponent<MeshFilter>();
            filter.sharedMesh = centeredMesh;

            var renderer = weightObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = sourceRenderer.sharedMaterials;

            var collider = weightObject.AddComponent<BoxCollider>();
            collider.center = centeredMesh.bounds.center;
            collider.size = centeredMesh.bounds.size;

            var rigidbody = weightObject.AddComponent<Rigidbody>();
            rigidbody.mass = 1f;
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;

            var weighted = weightObject.AddComponent<WeightedDumbbell>();
            weighted.Weight.Value = 1f;

            var grab = weightObject.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = false;

            var prefabPath = $"{PrefabOutputFolder}/StandardWeight_{i + 1:00}.prefab";
            PrefabUtility.SaveAsPrefabAsset(weightObject, prefabPath);
            created.Add(weightObject);
        }

        Selection.objects = created.Cast<UnityEngine.Object>().ToArray();
        EditorSceneManager.MarkSceneDirty(root.scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Split {root.name} into {created.Count} standard weight objects.");
    }

    private static void DeleteGeneratedAssets()
    {
        foreach (var path in AssetDatabase.FindAssets("StandardWeight_", new[] { PrefabOutputFolder })
                     .Select(AssetDatabase.GUIDToAssetPath)
                     .Where(path => Path.GetFileNameWithoutExtension(path).StartsWith("StandardWeight_", StringComparison.Ordinal)))
        {
            AssetDatabase.DeleteAsset(path);
        }

        if (AssetDatabase.IsValidFolder(MeshOutputFolder))
        {
            AssetDatabase.DeleteAsset(MeshOutputFolder);
        }

        Directory.CreateDirectory(MeshOutputFolder);
        AssetDatabase.Refresh();
    }

    private static List<MeshGroup> SplitMeshIntoFourByX(Mesh source)
    {
        var vertices = source.vertices;
        var triangleInfos = new List<TriangleInfo>();
        for (var subMesh = 0; subMesh < source.subMeshCount; subMesh++)
        {
            var triangles = source.GetTriangles(subMesh);
            for (var i = 0; i < triangles.Length; i += 3)
            {
                var a = triangles[i];
                var b = triangles[i + 1];
                var c = triangles[i + 2];
                var centroidX = (vertices[a].x + vertices[b].x + vertices[c].x) / 3f;
                triangleInfos.Add(new TriangleInfo(subMesh, a, b, c, centroidX));
            }
        }

        var xs = triangleInfos.Select(t => t.CentroidX).OrderBy(x => x).ToArray();
        var centers = new float[4];
        for (var i = 0; i < centers.Length; i++)
        {
            centers[i] = xs[Mathf.Clamp(Mathf.RoundToInt((i + 0.5f) * xs.Length / 4f), 0, xs.Length - 1)];
        }

        for (var iteration = 0; iteration < 20; iteration++)
        {
            var sums = new float[4];
            var counts = new int[4];
            foreach (var triangle in triangleInfos)
            {
                var group = ClosestCenter(triangle.CentroidX, centers);
                sums[group] += triangle.CentroidX;
                counts[group]++;
            }

            for (var i = 0; i < centers.Length; i++)
            {
                if (counts[i] > 0)
                {
                    centers[i] = sums[i] / counts[i];
                }
            }
        }

        var buckets = new List<TriangleInfo>[4];
        for (var i = 0; i < buckets.Length; i++)
        {
            buckets[i] = new List<TriangleInfo>();
        }

        foreach (var triangle in triangleInfos)
        {
            buckets[ClosestCenter(triangle.CentroidX, centers)].Add(triangle);
        }

        return buckets.Select(bucket => new MeshGroup(BuildMesh(source, bucket))).ToList();
    }

    private static int ClosestCenter(float value, float[] centers)
    {
        var best = 0;
        var bestDistance = Mathf.Abs(value - centers[0]);
        for (var i = 1; i < centers.Length; i++)
        {
            var distance = Mathf.Abs(value - centers[i]);
            if (distance < bestDistance)
            {
                best = i;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static Mesh BuildMesh(Mesh source, List<TriangleInfo> triangles)
    {
        var sourceVertices = source.vertices;
        var sourceNormals = source.normals;
        var sourceUv = source.uv;
        var hasNormals = sourceNormals != null && sourceNormals.Length == sourceVertices.Length;
        var hasUv = sourceUv != null && sourceUv.Length == sourceVertices.Length;

        var remap = new Dictionary<int, int>();
        var vertices = new List<Vector3>();
        var normals = hasNormals ? new List<Vector3>() : null;
        var uvs = hasUv ? new List<Vector2>() : null;
        var subMeshTriangles = Enumerable.Range(0, source.subMeshCount).Select(_ => new List<int>()).ToArray();

        foreach (var triangle in triangles)
        {
            AddIndex(triangle.A, triangle.SubMesh);
            AddIndex(triangle.B, triangle.SubMesh);
            AddIndex(triangle.C, triangle.SubMesh);
        }

        var mesh = new Mesh
        {
            name = source.name + "_SingleWeight",
            indexFormat = source.indexFormat,
            subMeshCount = source.subMeshCount
        };
        mesh.SetVertices(vertices);
        if (hasNormals)
        {
            mesh.SetNormals(normals);
        }

        if (hasUv)
        {
            mesh.SetUVs(0, uvs);
        }

        for (var subMesh = 0; subMesh < subMeshTriangles.Length; subMesh++)
        {
            mesh.SetTriangles(subMeshTriangles[subMesh], subMesh);
        }

        mesh.RecalculateBounds();
        if (!hasNormals)
        {
            mesh.RecalculateNormals();
        }

        return mesh;

        void AddIndex(int oldIndex, int subMesh)
        {
            if (!remap.TryGetValue(oldIndex, out var newIndex))
            {
                newIndex = vertices.Count;
                remap.Add(oldIndex, newIndex);
                vertices.Add(sourceVertices[oldIndex]);
                normals?.Add(sourceNormals[oldIndex]);
                uvs?.Add(sourceUv[oldIndex]);
            }

            subMeshTriangles[subMesh].Add(newIndex);
        }
    }

    private static Mesh CreateCenteredMesh(Mesh source, string name, out Vector3 center)
    {
        center = source.bounds.center;
        var mesh = UnityEngine.Object.Instantiate(source);
        mesh.name = name;
        var vertices = mesh.vertices;
        for (var i = 0; i < vertices.Length; i++)
        {
            vertices[i] -= center;
        }

        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        return mesh;
    }

    private readonly struct TriangleInfo
    {
        public TriangleInfo(int subMesh, int a, int b, int c, float centroidX)
        {
            SubMesh = subMesh;
            A = a;
            B = b;
            C = c;
            CentroidX = centroidX;
        }

        public int SubMesh { get; }
        public int A { get; }
        public int B { get; }
        public int C { get; }
        public float CentroidX { get; }
    }

    private sealed class MeshGroup
    {
        public MeshGroup(Mesh mesh)
        {
            Mesh = mesh;
            Bounds = mesh.bounds;
        }

        public Mesh Mesh { get; }
        public Bounds Bounds { get; }
    }
}
