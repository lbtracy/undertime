using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Localization;

/// <summary>
/// 当靠近某个物体时显示的按键提示，请挂载到有 Collider 组件的对象上，并且 Collider 的 Trigger 属性需要设置为 true
/// </summary>
public class ShowKeyPressHint : MonoBehaviour, INeedReset
{
    // 需要绑定的按键
    public InputAction bindKey;
    // 需要额外显示的消息
    public LocalizedString textToShow;
    // 当按键按下时要触发的事件
    public UnityEvent onInputActionPerformed;
    // 消息预制体
    public GameObject textPrefab;
    // 消息文本的位置
    public Vector3 textPosition;
    // 主摄像机
    public GameObject mainCamera;
    // 玩家
    public GameObject player;
    // 按键名称
    public string KeyName;
    [Tooltip("是否需要视线看向物体才能触发事件")]
    public bool needLookAt;

    // 消息所在的画布
    private GameObject _textObject;
    private Renderer _textRenderer;
    private TMP_Text _text; 
    
    // Start is called before the first frame update
    void Start()
    {
        _textObject = Instantiate(textPrefab, gameObject.transform);
        _textObject.transform.Translate(textPosition);
        _text = _textObject.GetComponentInChildren<TMP_Text>();

        _textObject.SetActive(false); // 隐藏文本
        
        textToShow.StringChanged += UpdateString;
    }

    // Update is called once per frame
    void Update()
    {
        // 使文本始终朝向玩家
        _textObject.transform.eulerAngles = player.transform.eulerAngles;
        _text.transform.eulerAngles = mainCamera.transform.eulerAngles;
        
        if (bindKey == null) return;
        if (!bindKey.WasPerformedThisFrame() || !_textObject.activeSelf) return;
        // 如果需要看向当前物体，而没有看向当前物体，按下按键也没有用
        if (needLookAt && FirstPersonController.playerLookingAt != gameObject) return;
        onInputActionPerformed?.Invoke();
    }

    // 玩家进入区域
    private void OnTriggerStay(Collider other)
    {
        _textObject.SetActive(true);
    }

    // 玩家离开区域
    private void OnTriggerExit(Collider other)
    {
        _textObject.SetActive(false);
    }

    // 响应本地化字符串更新事件
    private void UpdateString(string v)
    {
        _text.text = v;
    }

    private void OnEnable()
    {
        bindKey.Enable();
        // TODO: 适配虚拟按键、手柄
        KeyName = $"[{bindKey.bindings[0].path["<keyboard>/".Length..].ToUpper()}]"; // 设置按键名
    }

    private void OnDisable()
    {
        bindKey.Disable();
        textToShow.StringChanged -= UpdateString;
    }

    public void Reset()
    {
        // 重置，隐藏文本
        _textObject.SetActive(false);
    }
}
