using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DG.Tweening
{
    public delegate void TweenCallback();

    public enum Ease
    {
        Linear,
        InSine,
        OutSine,
        InOutSine,
        InQuad,
        OutQuad,
        InOutQuad,
        InCubic,
        OutCubic,
        InOutCubic,
        OutBack,
        InOutBack,
        OutElastic
    }

    public enum LoopType
    {
        Restart,
        Yoyo
    }

    public abstract class Tween
    {
        public bool IsActive { get; internal set; } = true;
        public object Target { get; protected set; }
        public Ease EaseType { get; protected set; } = Ease.OutCubic;
        public float Delay;
        public int Loops = 1;
        public LoopType LoopType = LoopType.Restart;
        public TweenCallback OnCompleteCallback;

        public Tween SetEase(Ease ease)
        {
            EaseType = ease;
            return this;
        }

        public Tween SetDelay(float delay)
        {
            Delay = delay;
            return this;
        }

        public Tween SetLoops(int loops, LoopType loopType = LoopType.Restart)
        {
            Loops = loops;
            LoopType = loopType;
            return this;
        }

        public Tween SetTarget(object target)
        {
            Target = target;
            return this;
        }

        public Tween OnComplete(TweenCallback callback)
        {
            OnCompleteCallback = callback;
            return this;
        }

        public Tween SetUpdate(bool independentTime)
        {
            _independent = independentTime;
            return this;
        }

        internal bool _independent;

        public void Kill(bool complete = false)
        {
            if (complete)
            {
                CompleteImmediate();
            }

            IsActive = false;
            DOTween.Remove(this);
        }

        internal abstract void CompleteImmediate();
        internal abstract bool Step(float dt);
    }

    public sealed class Sequence : Tween
    {
        readonly List<Tween> _append = new List<Tween>();
        readonly List<List<Tween>> _groups = new List<List<Tween>>();
        int _index;

        public Sequence()
        {
            Target = this;
        }

        public Sequence Append(Tween tween)
        {
            if (tween == null)
            {
                return this;
            }

            DOTween.Steal(tween);
            _groups.Add(new List<Tween> { tween });
            return this;
        }

        public Sequence Join(Tween tween)
        {
            if (tween == null)
            {
                return this;
            }

            DOTween.Steal(tween);
            if (_groups.Count == 0)
            {
                _groups.Add(new List<Tween>());
            }

            _groups[_groups.Count - 1].Add(tween);
            return this;
        }

        public Sequence AppendInterval(float duration)
        {
            return Append(DOVirtual.DelayedCall(duration, null));
        }

        public Sequence AppendCallback(TweenCallback callback)
        {
            return Append(DOVirtual.DelayedCall(0f, callback));
        }

        internal override void CompleteImmediate()
        {
            for (int i = 0; i < _groups.Count; i++)
            {
                for (int j = 0; j < _groups[i].Count; j++)
                {
                    _groups[i][j].CompleteImmediate();
                }
            }

            OnCompleteCallback?.Invoke();
        }

        internal override bool Step(float dt)
        {
            if (_index >= _groups.Count)
            {
                OnCompleteCallback?.Invoke();
                return false;
            }

            var group = _groups[_index];
            bool any = false;
            for (int i = 0; i < group.Count; i++)
            {
                if (group[i].IsActive && group[i].Step(dt))
                {
                    any = true;
                }
            }

            if (!any)
            {
                _index++;
            }

            return _index < _groups.Count;
        }
    }

    public static class DOVirtual
    {
        public static Tween DelayedCall(float delay, TweenCallback callback)
        {
            return DOTween.To(() => 0f, _ => { }, 1f, Mathf.Max(0.0001f, delay)).OnComplete(callback);
        }

        public static Tween Float(float from, float to, float duration, Action<float> onUpdate)
        {
            float current = from;
            return DOTween.To(() => current, v =>
            {
                current = v;
                onUpdate?.Invoke(v);
            }, to, duration);
        }
    }

    sealed class ValueTween<T> : Tween
    {
        readonly Func<T> _getter;
        readonly Action<T> _setter;
        readonly T _end;
        readonly Func<T, T, float, T> _lerp;
        T _start;
        float _elapsed;
        bool _started;
        int _loopCount;

        public ValueTween(Func<T> getter, Action<T> setter, T end, float duration, Func<T, T, float, T> lerp, object target)
        {
            _getter = getter;
            _setter = setter;
            _end = end;
            _lerp = lerp;
            Duration = Mathf.Max(0.0001f, duration);
            Target = target;
        }

        public float Duration { get; }

        internal override void CompleteImmediate()
        {
            if (DOTween.IsMissing(Target))
            {
                return;
            }

            _setter(_end);
            OnCompleteCallback?.Invoke();
        }

        internal override bool Step(float dt)
        {
            if (DOTween.IsMissing(Target))
            {
                return false;
            }

            if (!_started)
            {
                Delay -= dt;
                if (Delay > 0f)
                {
                    return true;
                }

                _start = _getter();
                _started = true;
            }

            _elapsed += dt;
            float t = Mathf.Clamp01(_elapsed / Duration);
            t = EaseEval.Evaluate(EaseType, t);
            _setter(_lerp(_start, _end, t));

            if (_elapsed < Duration)
            {
                return true;
            }

            _loopCount++;
            if (Loops < 0 || _loopCount < Loops)
            {
                _elapsed = 0f;
                if (LoopType == LoopType.Yoyo)
                {
                    var tmp = _start;
                    _start = _end;
                    // cannot reassign _end; yoyo by swapping via getter next time
                    _setter(_lerp(tmp, _end, 1f));
                }

                return true;
            }

            _setter(_end);
            OnCompleteCallback?.Invoke();
            return false;
        }
    }

    static class EaseEval
    {
        public static float Evaluate(Ease ease, float t)
        {
            switch (ease)
            {
                case Ease.Linear:
                    return t;
                case Ease.InSine:
                    return 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
                case Ease.OutSine:
                    return Mathf.Sin(t * Mathf.PI * 0.5f);
                case Ease.InOutSine:
                    return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
                case Ease.InQuad:
                    return t * t;
                case Ease.OutQuad:
                    return 1f - (1f - t) * (1f - t);
                case Ease.InOutQuad:
                    return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
                case Ease.InCubic:
                    return t * t * t;
                case Ease.OutCubic:
                    return 1f - Mathf.Pow(1f - t, 3f);
                case Ease.InOutCubic:
                    return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
                case Ease.OutBack:
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                case Ease.InOutBack:
                    const float c2 = 1.70158f * 1.525f;
                    return t < 0.5f
                        ? Mathf.Pow(2f * t, 2f) * ((c2 + 1f) * 2f * t - c2) * 0.5f
                        : (Mathf.Pow(2f * t - 2f, 2f) * ((c2 + 1f) * (t * 2f - 2f) + c2) + 2f) * 0.5f;
                case Ease.OutElastic:
                    if (t == 0f || t == 1f)
                    {
                        return t;
                    }

                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
                default:
                    return t;
            }
        }
    }

    public static class DOTween
    {
        static readonly List<Tween> Active = new List<Tween>();
        static TweenHost _host;

        public static int MaxActiveTweens => Active.Count;

        internal static bool IsMissing(object target)
        {
            return target is UnityEngine.Object unityObject && unityObject == null;
        }

        public static void Init()
        {
            EnsureHost();
        }

        public static Sequence Sequence()
        {
            var seq = new Sequence();
            Add(seq);
            return seq;
        }

        public static Tween To(Func<float> getter, Action<float> setter, float end, float duration)
        {
            var tw = new ValueTween<float>(getter, setter, end, duration, Mathf.LerpUnclamped, setter.Target);
            Add(tw);
            return tw;
        }

        public static void Kill(object target, bool complete = false)
        {
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                if (Active[i].Target == target)
                {
                    Active[i].Kill(complete);
                }
            }
        }

        internal static void Add(Tween tween)
        {
            EnsureHost();
            Active.Add(tween);
        }

        internal static void Remove(Tween tween)
        {
            Active.Remove(tween);
        }

        internal static void Steal(Tween tween)
        {
            Active.Remove(tween);
        }

        internal static void Tick(float dt, float unscaled)
        {
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                var tw = Active[i];
                if (!tw.IsActive || !tw.Step(tw._independent ? unscaled : dt))
                {
                    tw.IsActive = false;
                    if (i < Active.Count)
                    {
                        Active.RemoveAt(i);
                    }
                }
            }
        }

        static void EnsureHost()
        {
            if (_host != null)
            {
                return;
            }

            var go = new GameObject("DOTween");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _host = go.AddComponent<TweenHost>();
        }

        sealed class TweenHost : MonoBehaviour
        {
            void Update()
            {
                Tick(Time.deltaTime, Time.unscaledDeltaTime);
            }
        }
    }

    public static class ShortcutExtensions
    {
        public static Tween DOMove(this Transform t, Vector3 endValue, float duration)
        {
            var tw = new ValueTween<Vector3>(() => t.position, v => t.position = v, endValue, duration, Vector3.LerpUnclamped, t);
            DOTween.Add(tw);
            return tw;
        }

        public static Tween DOLocalMove(this Transform t, Vector3 endValue, float duration)
        {
            var tw = new ValueTween<Vector3>(() => t.localPosition, v => t.localPosition = v, endValue, duration, Vector3.LerpUnclamped, t);
            DOTween.Add(tw);
            return tw;
        }

        public static Tween DOScale(this Transform t, Vector3 endValue, float duration)
        {
            var tw = new ValueTween<Vector3>(() => t.localScale, v => t.localScale = v, endValue, duration, Vector3.LerpUnclamped, t);
            DOTween.Add(tw);
            return tw;
        }

        public static Tween DOScale(this Transform t, float endValue, float duration)
        {
            return t.DOScale(Vector3.one * endValue, duration);
        }

        public static Tween DORotate(this Transform t, Vector3 endValue, float duration)
        {
            Quaternion end = Quaternion.Euler(endValue);
            var tw = new ValueTween<Quaternion>(() => t.rotation, v => t.rotation = v, end, duration, Quaternion.SlerpUnclamped, t);
            DOTween.Add(tw);
            return tw;
        }

        public static Tween DOLocalRotate(this Transform t, Vector3 endValue, float duration)
        {
            Quaternion end = Quaternion.Euler(endValue);
            var tw = new ValueTween<Quaternion>(() => t.localRotation, v => t.localRotation = v, end, duration, Quaternion.SlerpUnclamped, t);
            DOTween.Add(tw);
            return tw;
        }

        public static Tween DOPunchScale(this Transform t, Vector3 punch, float duration, int vibrato = 8, float elasticity = 1f)
        {
            Vector3 baseScale = t.localScale;
            float elapsed = 0f;
            return DOTween.To(() => 0f, v =>
            {
                elapsed = v;
                float damp = 1f - v;
                float wave = Mathf.Sin(v * vibrato * Mathf.PI) * damp * elasticity;
                t.localScale = baseScale + punch * wave;
            }, 1f, duration).SetTarget(t).OnComplete(() => t.localScale = baseScale);
        }

        public static Tween DOColor(this Material material, Color endValue, float duration)
        {
            var tw = new ValueTween<Color>(() => material.color, v => material.color = v, endValue, duration, Color.LerpUnclamped, material);
            DOTween.Add(tw);
            return tw;
        }

        public static Tween DOFade(this CanvasGroup group, float endValue, float duration)
        {
            var tw = new ValueTween<float>(() => group.alpha, v => group.alpha = v, endValue, duration, Mathf.LerpUnclamped, group);
            DOTween.Add(tw);
            return tw;
        }

        public static Tween DOFade(this Graphic graphic, float endValue, float duration)
        {
            var c = graphic.color;
            var tw = new ValueTween<float>(() => graphic.color.a, v =>
            {
                c = graphic.color;
                c.a = v;
                graphic.color = c;
            }, endValue, duration, Mathf.LerpUnclamped, graphic);
            DOTween.Add(tw);
            return tw;
        }
    }
}
