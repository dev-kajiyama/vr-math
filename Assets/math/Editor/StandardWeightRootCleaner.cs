using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class StandardWeightRootCleaner
{
    [MenuItem("Tools/Math/Clean Standard Weight Root Components")]
    public static void CleanStandardWeightRootComponents()
    {
        var root = GameObject.Find("StandardWeight");
        if (root == null)
        {
            Debug.LogWarning("StandardWeight root was not found.");
            return;
        }

        RemoveIfExists<WeightedDumbbell>(root);
        RemoveIfExists<XRGrabInteractable>(root);
        RemoveIfExists<BoxCollider>(root);
        RemoveIfExists<Rigidbody>(root);

        foreach (var oldFixedDumbbell in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (oldFixedDumbbell.name.StartsWith("TutorialFixedDumbbell", System.StringComparison.Ordinal))
            {
                Object.DestroyImmediate(oldFixedDumbbell.gameObject);
            }
        }

        var dumbbellsRack = GameObject.Find("DumbbellsRack");
        if (dumbbellsRack != null)
        {
            dumbbellsRack.SetActive(false);
        }

        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorSceneManager.SaveScene(root.scene);
        Debug.Log("Cleaned StandardWeight root and disabled the old dumbbell rack; split child weights remain interactable.");
    }

    private static void RemoveIfExists<T>(GameObject target) where T : Component
    {
        var component = target.GetComponent<T>();
        if (component != null)
        {
            Object.DestroyImmediate(component);
        }
    }
}
