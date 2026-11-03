using ByteLoop.Manager;
using ByteLoop.Tool;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ByteLoop.UI
{
    /// <summary>
    /// 按键提示条（右下角）：B=上餐，A=下一位顾客。
    /// 可操作时金色脉冲，不可操作时置灰，让隐藏的键盘操作可见。
    /// </summary>
    public class KeyHintBar : MonoBehaviour
    {
        public static KeyHintBar Instance { get; private set; }

        public static KeyHintBar Ensure()
        {
            if (Instance != null) return Instance;
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return null;
            var existing = canvas.transform.Find("KeyHintBar");
            if (existing != null)
            {
                Instance = existing.GetComponent<KeyHintBar>();
                return Instance;
            }
            var go = new GameObject("KeyHintBar", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            Instance = go.AddComponent<KeyHintBar>();
            return Instance;
        }

        private static readonly Color BgColor = new Color(0.1f, 0.08f, 0.06f, 0.7f);
        private static readonly Color ActiveColor = new Color(1f, 0.84f, 0.35f);
        private static readonly Color IdleColor = new Color(0.55f, 0.53f, 0.5f, 0.8f);

        private RectTransform _serveChip;
        private RectTransform _nextChip;
        private TextMeshProUGUI _serveLabel;
        private TextMeshProUGUI _nextLabel;
        private TMP_FontAsset _font;

        private void Awake()
        {
            Instance = this;
            _font = GrabFont();
            Build();
            gameObject.SetActive(false); // 进入游戏后再显示
        }

        private static TMP_FontAsset GrabFont()
        {
            var all = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            return all != null && all.Length > 0 ? all[0].font : null;
        }

        private void Build()
        {
            var rt = (RectTransform)transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-26f, 26f);
            rt.sizeDelta = new Vector2(360f, 60f);

            _serveChip = CreateChip("ServeChip", "B 上餐", new Vector2(-190f, 0f), out _serveLabel);
            _nextChip = CreateChip("NextChip", "A 下一位", new Vector2(-20f, 0f), out _nextLabel);
        }

        private RectTransform CreateChip(string name, string label, Vector2 rightOffset, out TextMeshProUGUI text)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = rightOffset;
            rt.sizeDelta = new Vector2(160f, 52f);

            var bg = go.AddComponent<Image>();
            bg.color = BgColor;
            bg.raycastTarget = false;

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            text = textGo.AddComponent<TextMeshProUGUI>();
            if (_font != null) text.font = _font;
            text.text = label;
            text.fontSize = 24f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = IdleColor;
            text.raycastTarget = false;
            var textRt = (RectTransform)textGo.transform;
            textRt.anchorMin = textRt.anchorMax = new Vector2(0.5f, 0.5f);
            textRt.pivot = new Vector2(0.5f, 0.5f);
            textRt.sizeDelta = new Vector2(150f, 44f);
            textRt.anchoredPosition = Vector2.zero;

            return rt;
        }

        private void Update()
        {
            bool serveReady = false;
            var station = ProductionStation.Instance;
            if (station != null)
            {
                var state = station.CurrentRecipeState;
                serveReady = state == RecipeState.Confirm || state == RecipeState.UnConfirm;
            }
            bool nextReady = DialogSystemTest.Instance != null
                && DialogSystemTest.Instance._index == 0
                && GameManager.Instance != null
                && GameManager.Instance.InputAllowed;

            SetChip(_serveLabel, serveReady);
            SetChip(_nextLabel, nextReady);
        }

        private void SetChip(TextMeshProUGUI label, bool active)
        {
            if (label == null) return;
            Color target = active ? ActiveColor : IdleColor;
            // 可用时轻微呼吸亮度
            if (active)
            {
                float k = 0.9f + 0.1f * (Mathf.Sin(Time.time * 5f) * 0.5f + 0.5f);
                target *= k;
                target.a = 1f;
            }
            label.color = Color.Lerp(label.color, target, Time.deltaTime * 10f);
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
