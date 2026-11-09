using ByteLoop.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ByteLoop.UI
{
    /// <summary>
    /// 为旧版主菜单补上清晰的开始入口和玩法提示。
    /// 运行时创建，避免手改升级后的场景 YAML。
    /// </summary>
    public static class MainMenuEnhancer
    {
        public static void Ensure(Transform mainMenu, GameManager gameManager)
        {
            if (mainMenu == null || gameManager == null) return;
            if (mainMenu.Find("EnhancedStartPanel") != null) return;

            var panel = new GameObject("EnhancedStartPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(mainMenu, false);
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 54f);
            panelRect.sizeDelta = new Vector2(600f, 150f);

            var panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.09f, 0.055f, 0.035f, 0.9f);
            panelImage.raycastTarget = false;

            var buttonGo = new GameObject("StartBusiness", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(panel.transform, false);
            var buttonRect = (RectTransform)buttonGo.transform;
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 1f);
            buttonRect.pivot = new Vector2(0.5f, 1f);
            buttonRect.anchoredPosition = new Vector2(0f, -14f);
            buttonRect.sizeDelta = new Vector2(390f, 76f);

            var buttonImage = buttonGo.GetComponent<Image>();
            buttonImage.color = new Color(0.84f, 0.28f, 0.12f, 1f);

            var button = buttonGo.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.72f, 1f);
            colors.pressedColor = new Color(0.84f, 0.78f, 0.68f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(gameManager.StartGame);
            buttonGo.AddComponent<MainMenuStartButton>();
            buttonGo.AddComponent<UIButtonJuice>();

            var font = FindFont();
            var title = CreateText(buttonGo.transform, "开始营业", font, 38f, FontStyles.Bold,
                new Color(1f, 0.93f, 0.68f, 1f));
            var titleRect = (RectTransform)title.transform;
            Stretch(titleRect);

            var hint = CreateText(panel.transform, "拖动食材到锅里  ·  B 上餐  ·  A 下一位", font, 20f,
                FontStyles.Normal, new Color(1f, 0.89f, 0.69f, 0.95f));
            var hintRect = (RectTransform)hint.transform;
            hintRect.anchorMin = hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0f, 10f);
            hintRect.sizeDelta = new Vector2(560f, 34f);
        }

        private static TMP_FontAsset FindFont()
        {
            var texts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            return texts != null && texts.Length > 0 ? texts[0].font : null;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string value, TMP_FontAsset font,
            float size, FontStyles style, Color color)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
            text.enableAutoSizing = false;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
