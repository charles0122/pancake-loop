using ByteLoop.Manager;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ByteLoop.UI
{
    /// <summary>网页主菜单入口使用显式指针接口，避免依赖运行时 UnityEvent。</summary>
    public class MainMenuStartButton : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            GameManager.Instance.StartGame();
        }
    }
}
