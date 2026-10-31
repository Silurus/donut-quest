using UnityEngine;

namespace DonutQuest.Gameplay
{
    /// <summary>
    /// Проигрывание анимаций Корры. Отдельный компонент: KorraController знает
    /// только физику, а про Animator не знает ничего.
    ///
    /// В модели corgi.glb три клипа от автора: Run, Jump, Sitting.
    /// Отдельных Idle и Bark в модели нет, поэтому:
    ///   • стойка  → Sitting на первом кадре (Корра просто сидит и ждёт);
    ///   • лай     → короткий Jump как заглушка (подскок «гав»);
    ///   • ходьба  → Run на пониженной скорости клипа.
    /// Когда появятся настоящие клипы Idle / Walk / Bark — назови их так и
    /// впиши имена в поля ниже. Код менять не придётся.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class KorraAnimator : MonoBehaviour
    {
        [Header("Имена клипов (как в модели или в Animator Controller)")]
        public string idleClip = "Sitting";
        public string runClip = "Run";
        public string jumpClip = "Jump";
        public string sitClip = "Sitting";
        public string barkClip = "Jump";

        [Header("Скорость клипа Run")]
        [Tooltip("Множитель, когда Корра идёт шагом")]
        public float walkClipSpeed = 0.55f;
        [Tooltip("Множитель на полном беге")]
        public float runClipSpeed = 1f;

        [Header("Плавность переходов, с")]
        public float crossfade = 0.12f;

        /// <summary>Идёт сохранение (сидит у куста) — контроллер движения это уважает.</summary>
        public bool IsSaving { get; private set; }

        KorraController _move;
        KorraInput _input;
        Animator _anim;

        string _current;
        float _lockUntil;

        void Awake()
        {
            _anim = GetComponent<Animator>();
            _move = GetComponentInParent<KorraController>();
            _input = GetComponentInParent<KorraInput>();
        }

        void Update()
        {
            if (_anim == null || _anim.runtimeAnimatorController == null) return;
            if (Time.time < _lockUntil) return; // доигрываем разовую анимацию

            // Лай: X. Удержание X у куста — сохранение.
            if (_input != null && _move != null && !_move.Locked)
            {
                if (_input.BarkHeld && CanSitHere())
                {
                    IsSaving = true;
                    PlayLoop(sitClip);
                    return;
                }

                if (IsSaving)
                {
                    IsSaving = false;
                    PlayLoop(idleClip);
                }

                if (_input.BarkPressed)
                {
                    PlayOnce(barkClip, 3f); // 3 — на случай отсутствия клипа
                    return;
                }
            }

            if (_move == null) return;

            if (!_move.IsGrounded)
            {
                PlayLoop(jumpClip, keepPhase: false);
                return;
            }

            float speed = Mathf.Abs(_move.Velocity.x);
            if (speed > 0.1f)
            {
                PlayLoop(runClip);
                float t = Mathf.InverseLerp(_move.walkSpeed, _move.runSpeed, speed);
                _anim.speed = Mathf.Lerp(walkClipSpeed, runClipSpeed, t);
            }
            else
            {
                _anim.speed = 1f;
                PlayLoop(idleClip, keepPhase: false);
            }
        }

        /// <summary>Пока зона сохранения не сделана, сидеть можно где угодно.</summary>
        bool CanSitHere() => true;

        void PlayLoop(string clipName, bool keepPhase = true)
        {
            if (!HasClip(clipName) || _current == clipName) return;
            _current = clipName;
            if (keepPhase) _anim.CrossFade(clipName, crossfade);
            else _anim.CrossFadeInFixedTime(clipName, crossfade);
        }

        void PlayOnce(string clipName, float fallbackLength)
        {
            if (!HasClip(clipName)) return;
            _current = clipName;
            _anim.CrossFadeInFixedTime(clipName, crossfade);
            _lockUntil = Time.time + Mathf.Max(0.1f, ClipLength(clipName, fallbackLength));
        }

        float ClipLength(string clipName, float fallback)
        {
            foreach (var c in _anim.runtimeAnimatorController.animationClips)
                if (c.name == clipName) return c.length * 0.9f;
            return fallback;
        }

        bool HasClip(string clipName)
        {
            foreach (var c in _anim.runtimeAnimatorController.animationClips)
                if (c.name == clipName) return true;
            return false;
        }
    }
}