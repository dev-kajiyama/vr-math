using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using VrMath.Rendering;

namespace VrMath.Editor
{
    public static class SuccessIconPrefabBuilder
    {
        private const string MenuPath = "Tools/Math/Create Success Icon Prefab";
        private const string PrefabPath = "Assets/math/UI/SuccessIcon/SuccessIcon.prefab";
        private const string BackgroundPath = "Assets/math/UI/SuccessIcon/SuccessIcon_Background.png";
        private const string RingPath = "Assets/math/UI/SuccessIcon/SuccessIcon_Ring.png";
        private const string CheckPath = "Assets/math/UI/SuccessIcon/SuccessIcon_Check.png";

        [MenuItem(MenuPath)]
        public static void CreatePrefabAndPlaceInstance()
        {
            var prefab = CreatePrefab();
            PlaceInstance(prefab);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"Success icon prefab created: {PrefabPath}");
        }

        private static GameObject CreatePrefab()
        {
            Directory.CreateDirectory("Assets/math/UI/SuccessIcon");

            var root = CreateIconHierarchy();
            root.SetActive(false);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject CreateIconHierarchy()
        {
            var root = new GameObject("SuccessIcon", typeof(RectTransform), typeof(SuccessIconAnimator));
            ConfigureRect(root.GetComponent<RectTransform>(), Vector2.zero, new Vector2(170f, 170f));

            CreateImage(root.transform, "BackgroundImage", BackgroundPath, Image.Type.Simple, 0f);
            CreateImage(root.transform, "RingImage", RingPath, Image.Type.Filled, 0f);
            CreateImage(root.transform, "CheckImage", CheckPath, Image.Type.Simple, 0f);

            return root;
        }

        private static void CreateImage(Transform parent, string name, string spritePath, Image.Type imageType, float initialAlpha)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);

            ConfigureRect(imageObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);

            var image = imageObject.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            image.type = imageType;
            image.preserveAspect = true;
            image.raycastTarget = false;

            if (imageType == Image.Type.Filled)
            {
                image.fillMethod = Image.FillMethod.Radial360;
                image.fillOrigin = (int)Image.Origin360.Top;
                image.fillClockwise = true;
                image.fillAmount = 0f;
            }

            var color = image.color;
            color.a = initialAlpha;
            image.color = color;
        }

        private static void ConfigureRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;

            if (sizeDelta == Vector2.zero)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                return;
            }

            rectTransform.sizeDelta = sizeDelta;
        }

        private static void PlaceInstance(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogWarning("Success icon prefab was not created.");
                return;
            }

            var cardRoot = FindCardRoot();
            if (cardRoot == null)
            {
                Debug.LogWarning("CoachingCardRoot was not found. Prefab was created but not placed in the scene.");
                return;
            }

            var existing = cardRoot.Find("SuccessIcon");
            GameObject instance;
            if (existing != null)
            {
                instance = existing.gameObject;
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (Transform child in instance.transform)
                {
                    Object.DestroyImmediate(child.gameObject);
                }

                Object.DestroyImmediate(instance.GetComponent<SuccessIconAnimator>());
                instance.AddComponent<SuccessIconAnimator>();

                var prefabRoot = prefab.transform;
                foreach (Transform child in prefabRoot)
                {
                    var childInstance = Object.Instantiate(child.gameObject, instance.transform);
                    childInstance.name = child.name;
                }
            }
            else
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, cardRoot);
                instance.name = "SuccessIcon";
            }

            var rectTransform = instance.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = instance.AddComponent<RectTransform>();
            }

            ConfigureRect(rectTransform, new Vector2(0f, -82f), new Vector2(170f, 170f));
            instance.SetActive(false);
        }

        private static Transform FindCardRoot()
        {
            var mainCard = Object.FindAnyObjectByType<ExpressionBalanceMainCard>(FindObjectsInactive.Include);
            if (mainCard != null)
            {
                return mainCard.transform;
            }

            var cardRoots = Object.FindObjectsByType<EquationLessonCardDisplay>(FindObjectsInactive.Include);
            foreach (var display in cardRoots)
            {
                if (display != null && display.name == "CoachingCardRoot")
                {
                    return display.transform;
                }
            }

            return null;
        }
    }
}
