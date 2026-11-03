using System.Collections;
using System.Collections.Generic;
using ByteLoop.Manager;
using ByteLoop.Tool;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ByteLoop.UI
{
    /// <summary>
    /// 订单卡片：显示当前顾客的配方（图标×数量），
    /// 加满的配料点亮，加错料整卡红闪。运行时构建，挂在 Persistent Canvas 下。
    /// 只读查询，不参与判定。
    /// </summary>
    public class OrderPanel : MonoBehaviour
    {
        public static OrderPanel Instance { get; private set; }

        public static OrderPanel Ensure()
        {
            if (Instance != null) return Instance;
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return null;
            var existing = canvas.transform.Find("OrderPanel");
            if (existing != null)
            {
                Instance = existing.GetComponent<OrderPanel>();
                return Instance;
            }
            var go = new GameObject("OrderPanel", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            Instance = go.AddComponent<OrderPanel>();
            return Instance;
        }

        private static readonly Dictionary<MaterialType, string> IconPaths = new Dictionary<MaterialType, string>
        {
            { MaterialType.Egg, "Cook/icon_egg" },
            { MaterialType.Pepper, "Cook/icon_pepper" },
            { MaterialType.Sauce, "Cook/icon_sauce" },
            { MaterialType.Sausage, "Cook/icon_sausage" },
            { MaterialType.Sesame, "Cook/icon_sesame" },
            { MaterialType.Coriander, "Cook/icon_coriander" },
            { MaterialType.Onion, "Cook/icon_onion" },
            { MaterialType.RemaineLettuce, "Cook/icon_lettuce" },
            { MaterialType.Flour, "Cook/icon_flour" },
            { MaterialType.Meat, "Cook/icon_meat" },
            { MaterialType.Oil, "Cook/icon_oil" },
            { MaterialType.Crisp, "Cook/icon_crisp" },
        };

        private static readonly Color PanelColor = new Color(0.12f, 0.09f, 0.07f, 0.78f);
        private static readonly Color ItemMissingColor = new Color(0.62f, 0.62f, 0.62f, 0.55f);
        private static readonly Color ItemDoneColor = new Color(1f, 0.95f, 0.75f);
        private static readonly Color TextNormal = new Color(0.9f, 0.88f, 0.82f);
        private static readonly Color TextGold = new Color(1f, 0.84f, 0.35f);

        private Image _bg;
        private Image _avatar;
        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _titleText;
        private RectTransform _grid;
        private readonly List<ItemView> _items = new List<ItemView>();

        private TMP_FontAsset _font;
        private bool _flashRunning;

        private class ItemView
        {
            public MaterialType Type;
            public int Need;
            public Image Icon;
            public TextMeshProUGUI Count;
        }

        private void Awake()
        {
            Instance = this;
            _font = GrabFont();
            Build();
            gameObject.SetActive(false); // 开局隐藏，进入游戏后再显示
        }

        private static TMP_FontAsset GrabFont()
        {
            var all = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            return all != null && all.Length > 0 ? all[0].font : null;
        }

        private void Build()
        {
            var rt = (RectTransform)transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(26f, -26f);
            rt.sizeDelta = new Vector2(470f, 400f);

            _bg = gameObject.AddComponent<Image>();
            _bg.color = PanelColor;
            _bg.raycastTarget = false;

            // 头部：头像 + 店名
            var avatarGo = new GameObject("Avatar", typeof(RectTransform));
            avatarGo.transform.SetParent(transform, false);
            _avatar = avatarGo.AddComponent<Image>();
            _avatar.preserveAspect = true;
            _avatar.raycastTarget = false;
            var avatarRt = (RectTransform)avatarGo.transform;
            avatarRt.anchorMin = avatarRt.anchorMax = new Vector2(0f, 1f);
            avatarRt.pivot = new Vector2(0f, 1f);
            avatarRt.anchoredPosition = new Vector2(18f, -18f);
            avatarRt.sizeDelta = new Vector2(86f, 86f);

            _nameText = CreateText("Name", 26f, TextNormal, FontStyles.Bold);
            var nameRt = (RectTransform)_nameText.transform;
            nameRt.anchorMin = nameRt.anchorMax = new Vector2(0f, 1f);
            nameRt.pivot = new Vector2(0f, 1f);
            nameRt.anchoredPosition = new Vector2(120f, -22f);
            nameRt.sizeDelta = new Vector2(320f, 34f);

            _titleText = CreateText("Title", 20f, TextGold, FontStyles.Normal);
            _titleText.text = "今日订单";
            var titleRt = (RectTransform)_titleText.transform;
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0f, 1f);
            titleRt.pivot = new Vector2(0f, 1f);
            titleRt.anchoredPosition = new Vector2(120f, -60f);
            titleRt.sizeDelta = new Vector2(320f, 28f);

            // 配料网格
            var gridGo = new GameObject("Grid", typeof(RectTransform));
            gridGo.transform.SetParent(transform, false);
            _grid = (RectTransform)gridGo.transform;
            _grid.anchorMin = _grid.anchorMax = new Vector2(0f, 1f);
            _grid.pivot = new Vector2(0f, 1f);
            _grid.anchoredPosition = new Vector2(18f, -122f);
            _grid.sizeDelta = new Vector2(434f, 260f);
            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(100f, 112f);
            grid.spacing = new Vector2(8f, 10f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
        }

        private TextMeshProUGUI CreateText(string name, float size, Color color, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            if (_font != null) text.font = _font;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>根据当前 NPC 重建订单内容。</summary>
        public void Rebuild()
        {
            var gm = GameManager.Instance;
            var station = ProductionStation.Instance;
            if (gm == null || station == null || station.CurrentRecipe == null) return;

            foreach (Transform child in _grid) Destroy(child.gameObject);
            _items.Clear();

            if (gm.npcList != null && gm.Npc < gm.npcList.Count && gm.npcList[gm.Npc] != null)
            {
                var role = gm.npcList[gm.Npc];
                if (_avatar != null) _avatar.sprite = role.RoleAvatar;
                if (_nameText != null) _nameText.text = role.RoleName;
            }

            foreach (var item in station.CurrentRecipe.RecipeItemList)
            {
                var cellGo = new GameObject("Item_" + item.materialTypes, typeof(RectTransform));
                cellGo.transform.SetParent(_grid, false);
                var cellBg = cellGo.AddComponent<Image>();
                cellBg.color = new Color(1f, 1f, 1f, 0.06f);
                cellBg.raycastTarget = false;

                var iconGo = new GameObject("Icon", typeof(RectTransform));
                iconGo.transform.SetParent(cellGo.transform, false);
                var icon = iconGo.AddComponent<Image>();
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                if (IconPaths.TryGetValue(item.materialTypes, out var path))
                {
                    icon.sprite = Resources.Load<Sprite>(path);
                }
                var iconRt = (RectTransform)iconGo.transform;
                iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 1f);
                iconRt.pivot = new Vector2(0.5f, 1f);
                iconRt.anchoredPosition = new Vector2(0f, -6f);
                iconRt.sizeDelta = new Vector2(62f, 62f);

                var count = CreateText("Count", 22f, TextNormal, FontStyles.Bold);
                count.text = "×" + item.nums;
                count.transform.SetParent(cellGo.transform, false);
                var countRt = (RectTransform)count.transform;
                countRt.anchorMin = countRt.anchorMax = new Vector2(0.5f, 0f);
                countRt.pivot = new Vector2(0.5f, 0f);
                countRt.anchoredPosition = new Vector2(0f, 6f);
                countRt.sizeDelta = new Vector2(90f, 28f);

                _items.Add(new ItemView { Type = item.materialTypes, Need = item.nums, Icon = icon, Count = count });
            }

            Refresh();
        }

        /// <summary>统计锅内原料，点亮已满足的行；检测加错的料红闪提醒。</summary>
        public void Refresh()
        {
            if (_items.Count == 0) return;
            Dictionary<MaterialType, int> pot = CountPot();
            bool hasWrong = false;

            foreach (var item in _items)
            {
                pot.TryGetValue(item.Type, out int got);
                bool done = got >= item.Need;
                item.Icon.color = done ? ItemDoneColor : ItemMissingColor;
                item.Count.color = done ? TextGold : TextNormal;
                if (got > 0) pot.Remove(item.Type);
            }
            // 剩下的就是配方里没有的料
            foreach (var kv in pot)
            {
                if (kv.Key != MaterialType.None && kv.Value > 0) hasWrong = true;
            }

            if (hasWrong) FlashWrong();
        }

        private static Dictionary<MaterialType, int> CountPot()
        {
            var dic = new Dictionary<MaterialType, int>();
            var pan = GameObject.FindGameObjectWithTag("MarterialParent");
            if (pan == null) return dic;
            foreach (var m in pan.GetComponentsInChildren<Material>())
            {
                dic.TryGetValue(m.Type, out int c);
                dic[m.Type] = c + 1;
            }
            return dic;
        }

        private void FlashWrong()
        {
            if (_flashRunning) return;
            _flashRunning = true;
            StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            Color baseColor = PanelColor;
            Color wrong = new Color(0.55f, 0.12f, 0.08f, 0.85f);
            float t = 0f;
            Juice.Shake(transform, 6f, 0.3f);
            while (t < 0.45f)
            {
                t += Time.unscaledDeltaTime;
                _bg.color = Color.Lerp(wrong, baseColor, Mathf.Clamp01(t / 0.45f));
                yield return null;
            }
            _bg.color = baseColor;
            _flashRunning = false;
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
