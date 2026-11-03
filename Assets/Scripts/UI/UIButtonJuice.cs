using ByteLoop.Tool;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ByteLoop.UI
{
    /// <summary>
    /// 按钮手感：按下缩小回弹 + 点击音效。由 UIButtonJuiceBootstrapper 自动挂到所有 Button 上。
    /// </summary>
    public class UIButtonJuice : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float PressScale = 0.93f;
        private Button _button;
        private Vector3 _baseScale;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            transform.localScale = _baseScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            transform.localScale = _baseScale * PressScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            transform.localScale = _baseScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = _baseScale;
        }

        /// <summary>把所有场景按钮接上手感与点击音（含未激活面板里的按钮）。</summary>
        public static void WireAll()
        {
            var uiManager = UIManager.Instance;
            Transform mainMenuRoot = uiManager != null && uiManager.MainMenu != null ? uiManager.MainMenu.transform : null;
            foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (button.GetComponent<UIButtonJuice>() == null)
                {
                    button.gameObject.AddComponent<UIButtonJuice>();
                }
                bool isMenuButton = mainMenuRoot != null && button.transform.IsChildOf(mainMenuRoot);
                var sfx = isMenuButton ? SfxType.MenuClick : SfxType.UiClick;
                button.onClick.AddListener(() =>
                {
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlaySfx(sfx, 0.7f);
                    }
                });
            }
        }
    }

    /// <summary>启动后自动给场景里的全部按钮接上手感。</summary>
    public static class UIButtonJuiceBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            Juice.DelayCall(0.6f, UIButtonJuice.WireAll);
        }
    }
}
