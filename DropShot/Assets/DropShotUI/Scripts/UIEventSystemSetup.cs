using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace DropShot.UI
{
    // 按项目的输入设置自动给 EventSystem 配上对应的输入模块（新版 Input System 或旧版 Input Manager），
    // 免得换项目后按钮点不动或者 Console 报错
    [DefaultExecutionOrder(-1000)]
    public class UIEventSystemSetup : MonoBehaviour
    {
        void Awake()
        {
            var es = FindAnyObjectByType<EventSystem>();
            if (es == null) es = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            if (es.GetComponent<BaseInputModule>() != null) return;
#if ENABLE_INPUT_SYSTEM
            es.gameObject.AddComponent<InputSystemUIInputModule>();
#else
            es.gameObject.AddComponent<StandaloneInputModule>();
#endif
        }
    }

    public static class UIInput
    {
        public static bool ClickDown()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }
    }
}
