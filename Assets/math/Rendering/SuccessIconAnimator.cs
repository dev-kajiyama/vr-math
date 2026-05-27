using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VrMath.Rendering
{
    /// <summary>
    /// 正解時の丸チェックアイコンを、リングが時計回りに塗られてからチェックが出るように制御します。
    /// </summary>
    public sealed class SuccessIconAnimator : MonoBehaviour
    {
        [SerializeField, Tooltip("緑の丸背景です。")]
        private Image backgroundImage;

        [SerializeField, Tooltip("白い円ラインです。Image Type は Filled / Radial 360 にします。")]
        private Image ringImage;

        [SerializeField, Tooltip("白いチェックです。")]
        private Image checkImage;

        [SerializeField, Min(0.01f), Tooltip("円ラインが一周する時間です。")]
        private float ringDuration = 0.55f;

        [SerializeField, Min(0.01f), Tooltip("チェックが表示される時間です。")]
        private float checkFadeDuration = 0.18f;

        private Coroutine animationCoroutine;

        private void Awake()
        {
            AutoAssignImages();
            ConfigureRingImage();
            Hide();
        }

        public void Play()
        {
            gameObject.SetActive(true);

            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }

            animationCoroutine = StartCoroutine(PlayRoutine());
        }

        public void Hide()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }

            if (ringImage != null)
            {
                ringImage.fillAmount = 0f;
            }

            SetImageAlpha(checkImage, 0f);
            gameObject.SetActive(false);
        }

        private IEnumerator PlayRoutine()
        {
            if (backgroundImage != null)
            {
                SetImageAlpha(backgroundImage, 1f);
            }

            if (ringImage != null)
            {
                ringImage.fillAmount = 0f;
                SetImageAlpha(ringImage, 1f);
            }

            SetImageAlpha(checkImage, 0f);

            var time = 0f;
            while (time < ringDuration)
            {
                time += Time.deltaTime;
                if (ringImage != null)
                {
                    ringImage.fillAmount = Mathf.Clamp01(time / ringDuration);
                }

                yield return null;
            }

            if (ringImage != null)
            {
                ringImage.fillAmount = 1f;
            }

            time = 0f;
            while (time < checkFadeDuration)
            {
                time += Time.deltaTime;
                SetImageAlpha(checkImage, Mathf.Clamp01(time / checkFadeDuration));
                yield return null;
            }

            SetImageAlpha(checkImage, 1f);
            animationCoroutine = null;
        }

        private void AutoAssignImages()
        {
            foreach (var image in GetComponentsInChildren<Image>(true))
            {
                if (image == null)
                {
                    continue;
                }

                if (backgroundImage == null && image.name.Contains("Background", System.StringComparison.OrdinalIgnoreCase))
                {
                    backgroundImage = image;
                }
                else if (ringImage == null && image.name.Contains("Ring", System.StringComparison.OrdinalIgnoreCase))
                {
                    ringImage = image;
                }
                else if (checkImage == null && image.name.Contains("Check", System.StringComparison.OrdinalIgnoreCase))
                {
                    checkImage = image;
                }
            }
        }

        private void ConfigureRingImage()
        {
            if (ringImage == null)
            {
                return;
            }

            ringImage.type = Image.Type.Filled;
            ringImage.fillMethod = Image.FillMethod.Radial360;
            ringImage.fillOrigin = (int)Image.Origin360.Top;
            ringImage.fillClockwise = true;
            ringImage.fillAmount = 0f;
        }

        private static void SetImageAlpha(Image image, float alpha)
        {
            if (image == null)
            {
                return;
            }

            var color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }
}
