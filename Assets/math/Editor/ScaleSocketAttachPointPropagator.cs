using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public static class ScaleSocketAttachPointPropagator
{
    private const string SourcePath = "OverlapScale/ChainsOriginLeft/LeftScale/LeftScaleSocket01/AttachPoint";
    private const string AttachPointName = "AttachPoint";

    [MenuItem("Tools/Math/Copy Scale Socket Attach Points")]
    public static void CopyScaleSocketAttachPoints()
    {
        var source = GameObject.Find(SourcePath);
        if (source == null)
        {
            throw new InvalidOperationException($"Source attach point was not found: {SourcePath}");
        }

        var sourceTransform = source.transform;
        var sockets = UnityEngine.Object.FindObjectsByType<XRSocketInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(socket => socket.name.StartsWith("LeftScaleSocket", StringComparison.Ordinal)
                             || socket.name.StartsWith("RightScaleSocket", StringComparison.Ordinal))
            .OrderBy(socket => socket.name)
            .ToArray();

        foreach (var socket in sockets)
        {
            var attachTransform = socket.transform.Find(AttachPointName);
            if (attachTransform == null)
            {
                var attachObject = new GameObject(AttachPointName);
                Undo.RegisterCreatedObjectUndo(attachObject, "Create scale socket attach point");
                attachTransform = attachObject.transform;
                attachTransform.SetParent(socket.transform, false);
            }

            Undo.RecordObject(attachTransform, "Copy scale socket attach point transform");
            attachTransform.localPosition = sourceTransform.localPosition;
            attachTransform.localRotation = sourceTransform.localRotation;
            attachTransform.localScale = sourceTransform.localScale;

            Undo.RecordObject(socket, "Assign scale socket attach point");
            socket.attachTransform = attachTransform;
            EditorUtility.SetDirty(socket);
        }

        if (sockets.Length > 0)
        {
            EditorSceneManager.MarkSceneDirty(sockets[0].gameObject.scene);
            EditorSceneManager.SaveScene(sockets[0].gameObject.scene);
        }

        Debug.Log($"Copied {AttachPointName} from LeftScaleSocket01 to {sockets.Length} scale sockets.");
    }
}
