using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace VrMath.Rendering
{
    /// <summary>
    /// 箱の中や下に並べた答えブロックを、答えの値に合わせて表示します。
    /// </summary>
    public sealed class AnswerBlockDisplay : MonoBehaviour
    {
        [SerializeField, Tooltip("答えブロックの親。未設定ならこの GameObject の子を使います。")]
        private Transform blocksRoot;

        [SerializeField, Tooltip("この値の数だけ、先頭からブロックを表示します。")]
        private int answerValue = 8;

        [SerializeField, Tooltip("各ブロックに表示する文字。x = 8 なら 1 を8個見せる想定です。")]
        private string blockLabel = "1";

        [SerializeField, Tooltip("開始時に答えブロックを隠します。")]
        private bool hideOnStart = true;

        [SerializeField, Tooltip("オンなら表示更新時に子の TextMeshPro ラベルも書き換えます。")]
        private bool updateChildLabels = true;

        private readonly List<GameObject> blocks = new();

        /// <summary>
        /// 現在表示する答えの値です。0 未満は 0 として扱います。
        /// </summary>
        public int AnswerValue
        {
            get => answerValue;
            set
            {
                answerValue = Mathf.Max(0, value);
                Apply(answerValue);
            }
        }

        private void Awake()
        {
            CollectBlocks();
        }

        private void Start()
        {
            if (hideOnStart)
            {
                HideAll();
                return;
            }

            Apply(answerValue);
        }

        private void OnValidate()
        {
            answerValue = Mathf.Max(0, answerValue);
        }

        /// <summary>
        /// 答えの値を受け取り、その数だけブロックを表示します。
        /// </summary>
        public void ShowAnswer(int value)
        {
            AnswerValue = value;
        }

        /// <summary>
        /// Inspector の answerValue を使って答えブロックを表示します。
        /// </summary>
        public void ShowConfiguredAnswer()
        {
            Apply(answerValue);
        }

        /// <summary>
        /// すべての答えブロックを非表示にします。
        /// </summary>
        public void HideAll()
        {
            CollectBlocks();

            foreach (var block in blocks)
            {
                if (block != null)
                {
                    block.SetActive(false);
                }
            }
        }

        private void Apply(int visibleCount)
        {
            CollectBlocks();

            for (var i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];
                if (block == null)
                {
                    continue;
                }

                var shouldShow = i < visibleCount;
                block.SetActive(shouldShow);

                if (shouldShow && updateChildLabels)
                {
                    SetLabels(block);
                }
            }
        }

        private void CollectBlocks()
        {
            blocks.Clear();

            var root = blocksRoot != null ? blocksRoot : transform;
            for (var i = 0; i < root.childCount; i++)
            {
                blocks.Add(root.GetChild(i).gameObject);
            }
        }

        private void SetLabels(GameObject block)
        {
            var labels = block.GetComponentsInChildren<TMP_Text>(true);
            foreach (var label in labels)
            {
                label.text = blockLabel;
            }
        }
    }
}
