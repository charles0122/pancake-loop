using System.Collections;
using System.Collections.Generic;
using ByteLoop.Tool;
using UnityEngine;
using UnityEngine.UI;

namespace ByteLoop.Manager
{
    /// <summary>
    /// 锅内煎饼的视觉呈现：倒面糊 → 摊饼 → 烹饪变色/蒸汽 → 加料落位 → 上餐折叠/结果CG。
    /// 运行时自建（Ensure），挂在场景根部而非锅节点下，
    /// 因为 ProductionStation.ClearCurrentProduction 会清空锅的全部子物体。
    /// </summary>
    public class PanPresenter : MonoBehaviour
    {
        public static PanPresenter Instance { get; private set; }

        public static PanPresenter Ensure()
        {
            if (Instance != null) return Instance;
            var existing = FindFirstObjectByType<PanPresenter>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }
            var go = new GameObject("PanPresenter");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<PanPresenter>();
            return Instance;
        }

        // —— 可调参数 ——
        [Header("尺寸(世界单位)")]
        [SerializeField] private float _crepeSize = 5.2f;      // 面饼直径
        [SerializeField] private float _doneSize = 4.6f;       // 成品宽度
        [SerializeField] private float _toppingSize = 1.25f;   // 配料直径
        [Header("节奏")]
        [SerializeField] private float _ladleSweepTime = 0.45f;   // 勺子扫过
        [SerializeField] private float _spreadTime = 0.6f;        // 面糊摊开
        [SerializeField] private float _landTweenTime = 0.18f;    // 配料滑到槽位
        [SerializeField] private float _browningTime = 12f;       // 由生变金黄的总时长
        [SerializeField] private float _foldTime = 0.55f;         // 折叠
        [SerializeField] private float _serveSlideTime = 0.6f;    // 成品滑出
        [SerializeField] private float _resultHoldTime = 1.5f;    // 结果CG停留
        [Header("布局")]
        [SerializeField] private float _slotRadius = 1.55f;       // 配料环半径
        [SerializeField] private float _slotJitter = 0.22f;
        [SerializeField] private Vector3 _crepeOffset = new Vector3(0f, -0.05f, 0f);
        [SerializeField] private Vector3 _timerUiOffset = new Vector3(0f, 2.4f, 0f);

        // 生饼 → 金黄
        private static readonly Color CrepeRawColor = new Color(1f, 0.96f, 0.85f);
        private static readonly Color CrepeCookedColor = new Color(0.95f, 0.73f, 0.42f);
        private static readonly Color SauceTint = new Color(0.82f, 0.58f, 0.42f);
        private static readonly Color PepperTint = new Color(0.88f, 0.62f, 0.52f);

        private Transform _pan;
        private SpriteRenderer _crepe, _glow, _donePancake;
        private ParticleSystem _steam, _burst;
        private Transform _serveGroup;
        private Sprite _crepeSprite, _doneSprite, _resultGood, _resultBad, _ladleSprite, _brushSauce, _brushPepper, _eggSplat;
        private Sprite _softDot;

        private bool _hasCrepe;
        private bool _spreadDone;
        private float _cookTime;
        private int _draggingCount;
        private int _slotIndex;
        private Coroutine _breathCo;

        public Vector3 CookUiPos => _pan != null ? _pan.position + _timerUiOffset : Vector3.zero;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            LoadSprites();
            BuildRig();
        }

        private void LoadSprites()
        {
            var catalog = Resources.Load<CookVisualCatalog>("CookVisualCatalog");
            if (catalog == null)
            {
                Debug.LogError("PanPresenter: 缺少 Resources/CookVisualCatalog 资源");
                return;
            }

            _crepeSprite = catalog.crepe;
            _doneSprite = catalog.pancakeDone;
            _resultGood = catalog.resultGood;
            _resultBad = catalog.resultBad;
            _ladleSprite = catalog.ladle;
            _brushSauce = catalog.brushSauce;
            _brushPepper = catalog.brushPepper;
            _eggSplat = catalog.eggSplat;
        }

        private void BuildRig()
        {
            _softDot = MakeSoftDotSprite();

            _serveGroup = new GameObject("ServeGroup").transform;
            _serveGroup.SetParent(transform, false);

            _glow = new GameObject("Glow").AddComponent<SpriteRenderer>();
            _glow.transform.SetParent(transform, false);
            _glow.enabled = false;

            _crepe = new GameObject("Crepe").AddComponent<SpriteRenderer>();
            _crepe.transform.SetParent(transform, false);
            _crepe.sprite = _crepeSprite;
            _crepe.sortingOrder = 2;
            _crepe.color = CrepeRawColor;
            _crepe.enabled = false;

            _donePancake = new GameObject("DonePancake").AddComponent<SpriteRenderer>();
            _donePancake.transform.SetParent(transform, false);
            _donePancake.sprite = _doneSprite;
            _donePancake.sortingOrder = 12;
            _donePancake.enabled = false;

            _steam = BuildParticles("Steam", 7f);
            _burst = BuildParticles("Burst", 0f);

            if (_pan == null) _pan = GameObject.FindGameObjectWithTag("MarterialParent")?.transform;
            RefreshPanVisuals();
        }

        private static void FitToSize(SpriteRenderer sr, Sprite sprite, float size)
        {
            if (sprite == null || size <= 0f) return;
            float w = sprite.bounds.size.x;
            if (w <= 0.0001f) return;
            float s = size / w;
            sr.transform.localScale = new Vector3(s, s, 1f);
        }

        // 程序生成柔和圆形贴图，用于蒸汽/烟雾粒子
        private Sprite MakeSoftDotSprite()
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - size / 2f + 0.5f) / (size / 2f);
                    float dy = (y - size / 2f + 0.5f) / (size / 2f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a); // smoothstep 边缘
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private ParticleSystem BuildParticles(string name, float ratePerSecond)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 1.1f;
            main.startSpeed = 0.75f;
            main.startSize = 0.35f;
            main.startColor = Color.white;
            main.gravityModifier = 0f;
            main.playOnAwake = false;
            main.maxParticles = 120;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = ratePerSecond;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1.6f;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.38f, 0.25f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.25f;
            noise.frequency = 0.6f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new UnityEngine.Material(Shader.Find("Sprites/Default"));
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 20;

            var sheet = ps.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Sprites;
            sheet.AddSprite(_softDot);

            return ps;
        }

        // 让蒸汽/粒子跟随锅的位置
        private void RefreshPanVisuals()
        {
            if (_pan == null) return;
            Vector3 p = _pan.position + _crepeOffset;
            _crepe.transform.position = p;
            _donePancake.transform.position = p;
            if (_steam != null) _steam.transform.position = p + new Vector3(0f, 0.4f, 0f);
            if (_burst != null) _burst.transform.position = p;
            if (_glow.sprite == null && _pan.GetComponentInChildren<SpriteRenderer>() is var panSr && panSr != null)
            {
                _glow.sprite = panSr.sprite;
            }
            if (_glow.sprite != null)
            {
                _glow.transform.position = _pan.position;
                float w = _glow.sprite.bounds.size.x;
                if (w > 0.001f)
                {
                    float s = (_crepeSize + 1.7f) / w;
                    _glow.transform.localScale = new Vector3(s, s, 1f);
                }
                _glow.sortingOrder = 0;
                _glow.color = new Color(1f, 0.92f, 0.55f, 0f);
            }
        }

        private void EnsurePan()
        {
            if (_pan == null)
            {
                _pan = GameObject.FindGameObjectWithTag("MarterialParent")?.transform;
                RefreshPanVisuals();
            }
        }

        private void Update()
        {
            EnsurePan();
            if (_hasCrepe)
            {
                _cookTime += Time.deltaTime;
                if (_spreadDone)
                {
                    _crepe.color = Color.Lerp(CrepeRawColor, CrepeCookedColor, Mathf.Clamp01(_cookTime / _browningTime));
                }
            }
            // 拖拽时的锅体呼吸高亮
            if (_glow.enabled)
            {
                float a = 0.16f + 0.14f * (Mathf.Sin(Time.time * 4.5f) * 0.5f + 0.5f);
                var c = _glow.color;
                c.a = a;
                _glow.color = c;
            }
        }

        // —— 对外事件 ——————————————————————————————

        /// <summary>任意配料开始被拖拽（锅体高亮提示落点）。</summary>
        public void OnDragStart()
        {
            _draggingCount++;
            if (_glow != null && _pan != null) _glow.enabled = true;
        }

        public void OnDragEnd()
        {
            _draggingCount = Mathf.Max(0, _draggingCount - 1);
            if (_draggingCount == 0 && _glow != null) _glow.enabled = false;
        }

        /// <summary>配料落锅（Material 下锅后立即调用），负责落位与特效。</summary>
        public void OnMaterialDropped(Material material)
        {
            EnsurePan();
            if (material == null || _pan == null) return;

            if (material.Type == MaterialType.Flour && !_hasCrepe)
            {
                StartCoroutine(BatterPourRoutine(material));
                return;
            }
            StartCoroutine(ToppingLandRoutine(material));
        }

        /// <summary>上餐（按 B）：在 ClearCurrentProduction 之前调用，接管锅内视觉。</summary>
        public void BeginServe(bool success)
        {
            EnsurePan();
            StopAllCoroutines();
            _breathCo = null;
            AudioManager.Instance.StopSfxLoop();
            if (_steam != null) _steam.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            // 把锅里残留的配料收进 ServeGroup，避免被 ClearCurrentProduction 销毁
            if (_pan != null)
            {
                for (int i = _pan.childCount - 1; i >= 0; i--)
                {
                    _pan.GetChild(i).SetParent(_serveGroup, true);
                }
            }

            StartCoroutine(ServeRoutine(success));
        }

        /// <summary>清空锅内视觉（换顾客/换天时调用）。</summary>
        public void ResetVisual()
        {
            StopAllCoroutines();
            _breathCo = null;
            _hasCrepe = false;
            _spreadDone = false;
            _cookTime = 0f;
            _slotIndex = 0;
            AudioManager.Instance.StopSfxLoop();
            if (_steam != null) _steam.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (_crepe != null) _crepe.enabled = false;
            if (_donePancake != null) _donePancake.enabled = false;
            ClearCrepeChildren();
            if (_serveGroup != null)
            {
                foreach (Transform child in _serveGroup) Destroy(child.gameObject);
            }
        }

        // —— 倒面糊 → 摊饼 ————————————————————————————
        private IEnumerator BatterPourRoutine(Material material)
        {
            _hasCrepe = true;
            _spreadDone = false;
            _cookTime = 0f;
            _slotIndex = 0;
            ClearCrepeChildren();
            HideMaterial(material);

            _crepe.enabled = true;
            _crepe.color = CrepeRawColor;
            _crepe.transform.localScale = Vector3.zero;

            // 面粉勺子从左上扫到锅上方，倾斜倒糊
            SpriteRenderer ladle = null;
            if (_ladleSprite != null)
            {
                ladle = new GameObject("Ladle").AddComponent<SpriteRenderer>();
                ladle.transform.SetParent(transform, false);
                ladle.sprite = _ladleSprite;
                ladle.sortingOrder = 15;
                FitToSize(ladle, _ladleSprite, 2.6f);
                Vector3 start = _pan.position + new Vector3(-3.6f, 3.4f, 0f);
                Vector3 end = _pan.position + new Vector3(0f, 2.1f, 0f);
                ladle.transform.position = start;
                ladle.transform.rotation = Quaternion.Euler(0f, 0f, -35f);
                Juice.TweenPosition(ladle.transform, end, _ladleSweepTime, Juice.Ease.Quad);
                Juice.TweenScale(ladle.transform, ladle.transform.localScale * 0.9f, _ladleSweepTime);
            }

            yield return new WaitForSeconds(_ladleSweepTime * 0.6f);

            // 面糊摊开：面饼从中心弹性放大
            Vector3 targetScale = ScaleForSize(_crepeSprite, _crepeSize);
            Juice.TweenScale(_crepe.transform, targetScale, _spreadTime, Juice.Ease.Elastic);
            AudioManager.Instance.PlaySfx(SfxType.PourBatter, 0.8f);
            AudioManager.Instance.PlaySfxLoop(SfxType.Sizzle, 0.35f);
            if (_steam != null) _steam.Play();

            yield return new WaitForSeconds(_spreadTime);
            if (ladle != null)
            {
                Juice.FadeSprite(ladle, 0f, 0.2f, () => Destroy(ladle.gameObject));
            }
            _spreadDone = true;
            StartBreathing();
        }

        // —— 配料落位 ——————————————————————————————
        private IEnumerator ToppingLandRoutine(Material material)
        {
            if (!_hasCrepe)
            {
                // 没摊饼就下了料：轻微抖动提示（判定逻辑保持原样）
                Juice.Shake(material.transform, 0.15f, 0.25f);
                yield break;
            }

            // 配料要显示在饼上面（锅 sortingOrder=1，面饼=2）
            var matSr = material.GetComponent<SpriteRenderer>();
            if (matSr != null) matSr.sortingOrder = 5;

            // 滑到饼上的槽位
            Vector3 slot = NextSlotPosition(material.Type);
            Juice.TweenPosition(material.transform, slot, _landTweenTime, Juice.Ease.Back);

            if (material.Type == MaterialType.Egg)
            {
                AudioManager.Instance.PlaySfx(SfxType.EggBreak, 0.9f);
                SpawnSplat(slot);
                yield return new WaitForSeconds(_landTweenTime);
                HideMaterial(material);
                yield break;
            }

            if (material.Type == MaterialType.Sauce) StartCoroutine(BrushSwipeRoutine(_brushSauce, SauceTint));
            else if (material.Type == MaterialType.Pepper) StartCoroutine(BrushSwipeRoutine(_brushPepper, PepperTint));
            else if (material.Type == MaterialType.Oil) StartCoroutine(BrushSwipeRoutine(null, CrepeCookedColor));

            yield return new WaitForSeconds(_landTweenTime);
            Juice.PunchScale(material.transform, 0.3f, 0.28f);
            AudioManager.Instance.PlaySfx(SfxType.Sizzle, 0.45f);
        }

        private void SpawnSplat(Vector3 slot)
        {
            if (_eggSplat == null) return;
            var sr = new GameObject("EggSplat").AddComponent<SpriteRenderer>();
            sr.transform.SetParent(_crepe.transform, false);
            sr.sprite = _eggSplat;
            sr.sortingOrder = 3;
            sr.color = new Color(1f, 0.99f, 0.9f);
            // 挂在缩放后的面饼下，需要按父级缩放折算局部缩放
            float parentScale = _crepe.transform.lossyScale.x;
            float s = (_toppingSize * 2.2f) / (_eggSplat.bounds.size.x * parentScale);
            sr.transform.localScale = new Vector3(s, s, 1f);
            var local = _crepe.transform.InverseTransformPoint(slot);
            sr.transform.localPosition = new Vector3(local.x, local.y, 0f);
            Juice.PopScale(sr.transform, sr.transform.localScale, 0.35f);
        }

        private IEnumerator BrushSwipeRoutine(Sprite brushSprite, Color crepeTint)
        {
            SpriteRenderer brush = null;
            if (brushSprite != null)
            {
                brush = new GameObject("Brush").AddComponent<SpriteRenderer>();
                brush.transform.SetParent(transform, false);
                brush.sprite = brushSprite;
                brush.sortingOrder = 14;
                FitToSize(brush, brushSprite, 2.2f);
                Vector3 start = _pan.position + new Vector3(-2.4f, 0.7f, 0f);
                Vector3 end = _pan.position + new Vector3(2.4f, 0.1f, 0f);
                brush.transform.position = start;
                brush.transform.rotation = Quaternion.Euler(0f, 0f, -12f);
                Juice.TweenPosition(brush.transform, end, 0.35f, Juice.Ease.Quad);
            }

            // 饼体颜色向酱料色靠拢
            Color from = _crepe.color;
            Color target = Color.Lerp(from, crepeTint, 0.55f);
            Juice.TweenColor(_crepe, target, 0.35f);

            if (brush != null)
            {
                yield return new WaitForSeconds(0.4f);
                Juice.FadeSprite(brush, 0f, 0.2f, () => Destroy(brush.gameObject));
            }
        }

        // —— 上餐：折叠 → 滑出 → 结果CG ——————————————————
        private IEnumerator ServeRoutine(bool success)
        {
            yield return new WaitForSeconds(0.05f);

            if (_hasCrepe && _crepe.enabled)
            {
                // 1) 所有配料+蛋花一起压向饼中心（折叠前奏）
                if (_serveGroup.childCount > 0)
                {
                    foreach (Transform child in _serveGroup)
                    {
                        Juice.TweenPosition(child, _crepe.transform.position, 0.2f, Juice.Ease.Quad);
                        Juice.TweenScale(child, child.localScale * 0.6f, 0.2f);
                    }
                    yield return new WaitForSeconds(0.22f);
                }

                // 2) 折叠：饼横向压扁，然后换成成品精灵
                Vector3 crepeScale = _crepe.transform.localScale;
                yield return StartCoroutine(FoldRoutine(crepeScale));
            }
            else
            {
                // 没有饼就上餐（空锅/失败）：直接进结果
                yield return new WaitForSeconds(0.1f);
            }

            foreach (Transform child in _serveGroup) Destroy(child.gameObject);

            // 3) 成品滑向对话框方向淡出
            if (_donePancake.enabled)
            {
                AudioManager.Instance.PlaySfx(SfxType.Give, 0.7f);
                Vector3 slideTarget = _pan.position + new Vector3(0f, 6.5f, 0f);
                Juice.TweenPosition(_donePancake.transform, slideTarget, _serveSlideTime, Juice.Ease.Quad);
                Juice.TweenColor(_donePancake, new Color(1f, 1f, 1f, 0f), _serveSlideTime);
                yield return new WaitForSeconds(_serveSlideTime);
            }

            // 4) 结果CG
            if (success)
            {
                AudioManager.Instance.PlaySfx(SfxType.Coin, 0.9f);
                BurstParticles(new Color(1f, 0.86f, 0.35f, 0.9f), 22, 2.2f, 0.55f, 0.16f);
            }
            else
            {
                BurstParticles(new Color(0.22f, 0.22f, 0.22f, 0.75f), 16, 1.4f, 0.75f, 0.5f);
                if (_pan != null) Juice.Shake(_pan, 0.18f, 0.35f);
            }
            yield return StartCoroutine(ResultCardRoutine(success ? _resultGood : _resultBad));

            ResetVisual();
        }

        private IEnumerator FoldRoutine(Vector3 crepeScale)
        {
            float t = 0f;
            Vector3 flat = new Vector3(crepeScale.x * 0.45f, crepeScale.y, crepeScale.z);
            while (t < _foldTime)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / _foldTime);
                _crepe.transform.localScale = Vector3.Lerp(crepeScale, flat, JuiceCurve(k));
                yield return null;
            }
            // 换成成品
            _crepe.enabled = false;
            _donePancake.enabled = true;
            _donePancake.color = Color.white;
            Vector3 doneScale = ScaleForSize(_doneSprite, _doneSize);
            _donePancake.transform.localScale = doneScale * 0.5f;
            Juice.TweenScale(_donePancake.transform, doneScale, 0.25f, Juice.Ease.Back);
            yield return new WaitForSeconds(0.25f);
        }

        private static float JuiceCurve(float t) => 1f - (1f - t) * (1f - t);

        private IEnumerator ResultCardRoutine(Sprite sprite)
        {
            if (sprite == null) yield break;
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) yield break;

            var root = new GameObject("ResultCG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(canvas.transform, false);
            var img = root.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = new Color(1f, 1f, 1f, 0f);

            var rt = (RectTransform)root.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(620f, 620f);
            rt.localPosition = Vector3.zero;
            root.transform.localScale = Vector3.one * 0.4f;

            var cg = root.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            Juice.Fade(cg, 1f, 0.18f);
            Juice.TweenScale(root.transform, Vector3.one, 0.35f, Juice.Ease.Back);
            yield return new WaitForSeconds(_resultHoldTime);
            Juice.Fade(cg, 0f, 0.35f);
            yield return new WaitForSeconds(0.4f);
            Destroy(root);
        }

        // —— 辅助 ——————————————————————————————
        private void HideMaterial(Material material)
        {
            var sr = material.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Juice.TweenColor(sr, new Color(1f, 1f, 1f, 0f), 0.2f);
            }
        }

        private void ClearCrepeChildren()
        {
            if (_crepe == null) return;
            for (int i = _crepe.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(_crepe.transform.GetChild(i).gameObject);
            }
        }

        private Vector3 NextSlotPosition(MaterialType type)
        {
            EnsurePan();
            // 鸡蛋靠近中心，其余配料绕环分布
            if (type == MaterialType.Egg)
            {
                return _pan.position + new Vector3(0f, 0.45f, 0f);
            }
            const int slotCount = 8;
            float angle = Mathf.PI * 2f * _slotIndex / slotCount + 0.35f;
            _slotIndex = (_slotIndex + 1) % slotCount;
            var jitter = new Vector3(Random.Range(-_slotJitter, _slotJitter), Random.Range(-_slotJitter, _slotJitter), 0f);
            return _pan.position + new Vector3(Mathf.Cos(angle) * _slotRadius, Mathf.Sin(angle) * _slotRadius * 0.8f, 0f) + jitter;
        }

        private Vector3 ScaleForSize(Sprite sprite, float size)
        {
            if (sprite == null || sprite.bounds.size.x <= 0.0001f) return Vector3.one;
            float s = size / sprite.bounds.size.x;
            return new Vector3(s, s, 1f);
        }

        private void StartBreathing()
        {
            if (_breathCo != null) StopCoroutine(_breathCo);
            _breathCo = StartCoroutine(BreathRoutine());
        }

        private IEnumerator BreathRoutine()
        {
            Vector3 baseScale = _crepe.transform.localScale;
            while (_hasCrepe && _crepe.enabled)
            {
                float k = 1f + Mathf.Sin(Time.time * 3.4f) * 0.012f;
                _crepe.transform.localScale = baseScale * k;
                yield return null;
            }
        }

        private void BurstParticles(Color color, int count, float speed, float lifetime, float size)
        {
            if (_burst == null) return;
            var main = _burst.main;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            var emission = _burst.emission;
            emission.rateOverTime = 0f;
            _burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _burst.Play();
            var emitParams = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.PI * 2f * i / count + Random.Range(-0.2f, 0.2f);
                emitParams.velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * 0.8f, 0f) * speed;
                _burst.Emit(emitParams, 1);
            }
        }
    }
}
