using UnityEngine;

namespace DonutQuest.Gameplay
{
    /// <summary>
    /// Движение Корры: шаг, бег, рывок, прыжок. Камера сбоку, поэтому Z всегда 0.
    /// Числа — стартовые из SPEC.md (раздел 4), крутятся в инспекторе Unity.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(CorraInput))]
    public class CorraController : MonoBehaviour
    {
        [Header("Скорости, м/с")]
        public float walkSpeed = 3f;
        public float runSpeed = 6f;
        public float sprintSpeed = 10f;

        [Header("Рывок")]
        public float sprintDuration = 1.5f;
        public float sprintCooldown = 3f;

        [Header("Прыжок")]
        public float jumpHeight = 1.6f;
        public float gravity = -25f;
        [Tooltip("Отпустил кнопку раньше — прыжок ниже")]
        public float jumpCutMultiplier = 0.5f;
        public float coyoteTime = 0.1f;
        public float jumpBuffer = 0.1f;

        public bool IsGrounded => _cc.isGrounded;
        public bool IsSprinting => _sprintLeft > 0f;
        /// <summary>0..1 — для полоски выносливости в HUD.</summary>
        public float Stamina => _cooldownLeft > 0f ? 1f - _cooldownLeft / sprintCooldown : 1f;
        public Vector3 Velocity => _velocity;
        /// <summary>Блокировка управления (сохранение, кат-сцена).</summary>
        public bool Locked { get; set; }

        CharacterController _cc;
        CorraInput _input;
        Vector3 _velocity;
        float _sprintLeft, _cooldownLeft, _coyoteLeft, _bufferLeft;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _input = GetComponent<CorraInput>();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float move = Locked ? 0f : _input.Move;

            // --- рывок ---
            if (_cooldownLeft > 0f) _cooldownLeft -= dt;
            if (!Locked && _input.SprintPressed && _cooldownLeft <= 0f && _sprintLeft <= 0f)
                _sprintLeft = sprintDuration;
            if (_sprintLeft > 0f)
            {
                _sprintLeft -= dt;
                if (_sprintLeft <= 0f) _cooldownLeft = sprintCooldown;
            }

            float speed = IsSprinting ? sprintSpeed : (_input.RunHeld ? runSpeed : walkSpeed);
            _velocity.x = move * speed;

            // --- прыжок: coyote time + буфер нажатия ---
            _coyoteLeft = _cc.isGrounded ? coyoteTime : _coyoteLeft - dt;
            _bufferLeft = (!Locked && _input.JumpPressed) ? jumpBuffer : _bufferLeft - dt;

            if (_cc.isGrounded && _velocity.y < 0f) _velocity.y = -2f; // прижимаем к земле
            if (_bufferLeft > 0f && _coyoteLeft > 0f)
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity); // v = sqrt(2gh)
                _bufferLeft = _coyoteLeft = 0f;
            }
            if (!_input.JumpHeld && _velocity.y > 0f) _velocity.y += gravity * (1f / jumpCutMultiplier - 1f) * dt;
            _velocity.y += gravity * dt;

            _cc.Move(_velocity * dt);

            // фиксируем плоскость сайд-скроллера и разворачиваем модель по направлению
            var p = transform.position; p.z = 0f; transform.position = p;
            if (Mathf.Abs(move) > 0.01f)
                transform.rotation = Quaternion.Euler(0f, move > 0 ? 90f : -90f, 0f);
        }

        /// <summary>Телепорт (загрузка сохранения, возврат после падения).</summary>
        public void Teleport(Vector3 pos)
        {
            _cc.enabled = false;
            transform.position = pos;
            _velocity = Vector3.zero;
            _cc.enabled = true;
        }

        /// <summary>Восстановить выносливость (съеденная какашка кота).</summary>
        public void RefillStamina() => _cooldownLeft = 0f;
    }
}
