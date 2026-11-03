using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ByteLoop.Tool
{
    /// <summary>
    /// 轻量补间/手感工具库。所有动画通过协程驱动；
    /// 同一目标上同标签的动画再次启动时会先停掉旧的，避免叠加冲突。
    /// </summary>
    public static class Juice
    {
        private class Runner : MonoBehaviour { }

        private static Runner _runner;
        private static Runner R
        {
            get
            {
                if (_runner == null)
                {
                    var go = new GameObject("JuiceRunner");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    go.hideFlags = HideFlags.HideInHierarchy;
                    _runner = go.AddComponent<Runner>();
                }
                return _runner;
            }
        }

        // (目标实例ID, 标签) → 正在跑的协程
        private static readonly Dictionary<(int, string), Coroutine> _active = new Dictionary<(int, string), Coroutine>();

        private static Coroutine Start(UnityEngine.Object target, string tag, IEnumerator routine)
        {
            var key = (IdOf(target), tag);
            if (_active.TryGetValue(key, out var old) && old != null)
            {
                R.StopCoroutine(old);
            }
            var c = R.StartCoroutine(routine);
            _active[key] = c;
            return c;
        }

        private static int IdOf(UnityEngine.Object target)
        {
            return target == null ? 0 : target.GetInstanceID();
        }

        /// <summary>手动停掉某个目标上指定标签的动画。</summary>
        public static void Stop(UnityEngine.Object target, string tag)
        {
            var key = (IdOf(target), tag);
            if (_active.TryGetValue(key, out var c) && c != null)
            {
                R.StopCoroutine(c);
            }
        }

        // —— 缓动曲线 ——
        private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
        private static float EaseOutBack(float t)
        {
            const float c = 2.0f;
            float u = t - 1f;
            return 1f + (c + 1f) * u * u * u + c * u * u;
        }
        private static float EaseOutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return Mathf.Pow(2f, -9f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 2.6f)) + 1f;
        }

        public enum Ease { Quad, Back, Elastic, Linear }

        private static Func<float, float> Curve(Ease e)
        {
            switch (e)
            {
                case Ease.Back: return EaseOutBack;
                case Ease.Elastic: return EaseOutElastic;
                case Ease.Linear: return (t) => t;
                default: return EaseOutQuad;
            }
        }

        // —— 缩放 ——
        public static void TweenScale(Transform t, Vector3 target, float duration, Ease ease = Ease.Quad, string tag = "Scale")
        {
            if (t == null) return;
            Vector3 from = t.localScale;
            Start(t, tag, ScaleRoutine(t, from, target, duration, ease));
        }

        /// <summary>按倍率放大后弹回原尺寸（悬停/落料手感）。</summary>
        public static void PunchScale(Transform t, float strength, float duration)
        {
            if (t == null) return;
            Vector3 baseScale = t.localScale;
            Vector3 peak = baseScale * (1f + strength);
            Start(t, "Scale", PunchRoutine(t, baseScale, peak, duration));
        }

        /// <summary>从 0 弹性放大到目标缩放（面糊摊开、结果CG弹出）。</summary>
        public static void PopScale(Transform t, Vector3 target, float duration)
        {
            if (t == null) return;
            t.localScale = Vector3.zero;
            Start(t, "Scale", ScaleRoutine(t, Vector3.zero, target, duration, Ease.Elastic));
        }

        private static IEnumerator PunchRoutine(Transform t, Vector3 from, Vector3 peak, float duration)
        {
            float half = duration * 0.4f;
            yield return ScaleStep(t, from, peak, half, Ease.Quad);
            yield return ScaleStep(t, peak, from, duration - half, Ease.Back);
        }

        private static IEnumerator ScaleRoutine(Transform t, Vector3 from, Vector3 to, float duration, Ease ease)
        {
            yield return ScaleStep(t, from, to, duration, ease);
        }

        private static IEnumerator ScaleStep(Transform t, Vector3 from, Vector3 to, float duration, Ease ease)
        {
            if (duration <= 0f) { if (t) t.localScale = to; yield break; }
            float time = 0f;
            var curve = Curve(ease);
            while (time < duration)
            {
                if (t == null) yield break;
                time += Time.deltaTime;
                t.localScale = Vector3.LerpUnclamped(from, to, curve(Mathf.Clamp01(time / duration)));
                yield return null;
            }
            if (t) t.localScale = to;
        }

        // —— 位移 ——
        public static void TweenPosition(Transform t, Vector3 target, float duration, Ease ease = Ease.Quad)
        {
            if (t == null) return;
            Vector3 from = t.position;
            Start(t, "Pos", PosRoutine(t, from, target, duration, ease));
        }

        private static IEnumerator PosRoutine(Transform t, Vector3 from, Vector3 to, float duration, Ease ease)
        {
            float time = 0f;
            var curve = Curve(ease);
            while (time < duration)
            {
                if (t == null) yield break;
                time += Time.deltaTime;
                t.position = Vector3.LerpUnclamped(from, to, curve(Mathf.Clamp01(time / duration)));
                yield return null;
            }
            if (t) t.position = to;
        }

        // —— 抖动 ——
        /// <summary>位置抖动（失败/错料反馈），结束后回到起点。</summary>
        public static void Shake(Transform t, float strength, float duration)
        {
            if (t == null) return;
            Start(t, "Shake", ShakeRoutine(t, strength, duration));
        }

        private static IEnumerator ShakeRoutine(Transform t, float strength, float duration)
        {
            Vector3 origin = t.position;
            float time = 0f;
            while (time < duration)
            {
                if (t == null) yield break;
                time += Time.deltaTime;
                float k = 1f - time / duration; // 逐渐衰减
                t.position = origin + (Vector3)(UnityEngine.Random.insideUnitCircle * strength * k);
                yield return null;
            }
            if (t) t.position = origin;
        }

        // —— 颜色 ——
        public static void TweenColor(SpriteRenderer sr, Color target, float duration)
        {
            if (sr == null) return;
            var key = (sr.GetInstanceID(), "Color");
            if (_active.TryGetValue(key, out var old) && old != null) R.StopCoroutine(old);
            var c = R.StartCoroutine(ColorRoutine(sr, target, duration));
            _active[key] = c;
        }

        /// <summary>淡出/淡入 SpriteRenderer（透明度走颜色通道），结束后回调。</summary>
        public static void FadeSprite(SpriteRenderer sr, float targetAlpha, float duration, Action onDone = null)
        {
            if (sr == null) { onDone?.Invoke(); return; }
            var c = sr.color;
            c.a = targetAlpha;
            TweenColor(sr, c, duration);
            if (onDone != null) DelayCall(duration + 0.02f, onDone);
        }

        /// <summary>延迟执行一次回调（挂在 JuiceRunner 上，不受宿主销毁影响）。</summary>
        public static void DelayCall(float seconds, Action action)
        {
            R.StartCoroutine(DelayRoutine(seconds, action));
        }

        private static IEnumerator DelayRoutine(float seconds, Action action)
        {
            yield return new WaitForSeconds(seconds);
            action?.Invoke();
        }

        private static IEnumerator ColorRoutine(SpriteRenderer sr, Color target, float duration)
        {
            Color from = sr.color;
            float time = 0f;
            while (time < duration)
            {
                if (sr == null) yield break;
                time += Time.deltaTime;
                sr.color = Color.Lerp(from, target, Mathf.Clamp01(time / duration));
                yield return null;
            }
            if (sr) sr.color = target;
        }

        // —— 淡入淡出（CanvasGroup）——
        public static void Fade(CanvasGroup cg, float target, float duration, Action onDone = null)
        {
            if (cg == null) { onDone?.Invoke(); return; }
            var key = (cg.GetInstanceID(), "Fade");
            if (_active.TryGetValue(key, out var old) && old != null) R.StopCoroutine(old);
            var c = R.StartCoroutine(FadeRoutine(cg, target, duration, onDone));
            _active[key] = c;
        }

        private static IEnumerator FadeRoutine(CanvasGroup cg, float target, float duration, Action onDone)
        {
            float from = cg.alpha;
            float time = 0f;
            while (time < duration)
            {
                if (cg == null) yield break;
                time += Time.deltaTime;
                cg.alpha = Mathf.Lerp(from, target, Mathf.Clamp01(time / duration));
                yield return null;
            }
            if (cg) cg.alpha = target;
            onDone?.Invoke();
        }

        // —— 呼吸脉冲（锅体高亮、按键提示发光），返回协程句柄供停止 ——
        public static Coroutine Pulse(CanvasGroup cg, float min, float max, float period)
        {
            if (cg == null) return null;
            var key = (cg.GetInstanceID(), "Pulse");
            if (_active.TryGetValue(key, out var old) && old != null) R.StopCoroutine(old);
            var c = R.StartCoroutine(PulseRoutine(cg, min, max, period));
            _active[key] = c;
            return c;
        }

        public static void StopPulse(CanvasGroup cg)
        {
            if (cg == null) return;
            Stop(cg, "Pulse");
        }

        private static IEnumerator PulseRoutine(CanvasGroup cg, float min, float max, float period)
        {
            float time = 0f;
            while (cg != null)
            {
                time += Time.deltaTime;
                cg.alpha = Mathf.Lerp(min, max, (Mathf.Sin(time / period * Mathf.PI * 2f) + 1f) * 0.5f);
                yield return null;
            }
        }
    }
}
