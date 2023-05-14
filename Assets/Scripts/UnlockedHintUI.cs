using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;

namespace StarterAssets
{
    public class UnlockedHintUI : MonoBehaviour
    {
        public enum ItemType
        {
            Unknown,
            Achievement,
            Clue,
            Contact
        }

        private Animator _animator;
        
        public ItemType type;

        public LocalizedString forAchievementTitle;
        public LocalizedString forClueTitle;
        public LocalizedString forContactTitle;

        public TMP_Text title;
        public TMP_Text description;

        // 触发结束显示事件
        public UnityEvent hintDestroyed;

        // 设置类型与描述信息
        public void SetInfo(ItemType itemType, string itemDescription)
        {
            type = itemType;
            description.text = itemDescription;
        }

        // 当标题文本发生变化时进行的操作
        private void TitleStringChanged(string str)
        {
            title.text = str;
        }

        private IEnumerator Start()
        {
            // 等待类型被设置
            yield return new WaitUntil(() => description != null && type != ItemType.Unknown);

            switch (type)
            {
                case ItemType.Achievement:
                    forAchievementTitle.StringChanged += TitleStringChanged;
                    break;
                case ItemType.Clue:
                    forClueTitle.StringChanged += TitleStringChanged;
                    break;
                case ItemType.Contact:
                    forContactTitle.StringChanged += TitleStringChanged;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // 取得动画控制者组件
            _animator = GetComponent<Animator>();
            // 等待 3.5 秒
            yield return new WaitForSeconds(3.5f);
            _animator.SetTrigger("Leave");
            // 等待动画播放完成
            yield return new WaitForSeconds(1f);
            // 销毁自己
            Destroy(gameObject);
            yield return null;
        }

        private void OnDisable()
        {
            switch (type)
            {
                case ItemType.Achievement:
                    forAchievementTitle.StringChanged -= TitleStringChanged;
                    break;
                case ItemType.Clue:
                    forClueTitle.StringChanged -= TitleStringChanged;
                    break;
                case ItemType.Contact:
                    forContactTitle.StringChanged -= TitleStringChanged;
                    break;
            }
        }

        private void OnDestroy()
        {
            hintDestroyed.Invoke();
        }
    }
}