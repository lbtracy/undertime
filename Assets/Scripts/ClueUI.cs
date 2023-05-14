using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarterAssets
{
    public class ClueUI : MonoBehaviour
    {
        private int _clueId = -1;
        private readonly List<GameObject> _textAndImages = new();

        [Tooltip("线索标题")]
        public TMP_Text title;
        [Tooltip("线索内容，可以被 \\n 拆分成多段")]
        public TMP_Text content;
        [Tooltip("线索图片，可以通过在内容中写入 {image1} 这样的文本将内容拆开，并且在其中插入图片")] // TODO: 没有实现
        public Image image;
        [Tooltip("可滑动布局中的内容部分")]
        public GameObject  scrollContent;
        [Tooltip("当前线索 ID")] 
        public int clueId;

        private void Update()
        {
            if (_clueId == clueId)
                return;
            if (GameSaveManager.instance == null)
                return;
            // 销毁当前所有的文本和图片
            foreach (var textOrImage in _textAndImages)
            {
                Destroy(textOrImage);
            } 
            _textAndImages.Clear();

            // 根据线索内容来生成文本和图片
            var clue = GameSaveManager.instance.definedClues[clueId];
            title.text = clue.title.GetLocalizedString();
            var clueLines = clue.content.GetLocalizedString().Split('\n');
            content.text = clueLines[0];
            for (var i = 1; i < clueLines.Length; i++)
            {
                var text = Instantiate(content, scrollContent.transform);
                _textAndImages.Add(text.gameObject);
            }
            _clueId = clueId;
        }
    }
}