using UnityEngine;
using UnityEngine.InputSystem;

namespace DonutQuest.Gameplay
{
    /// <summary>
    /// Единая точка ввода. Остальные скрипты читают свойства отсюда
    /// и не знают, геймпад это или клавиатура.
    /// Действия создаются в коде (без .inputactions-ассета), чтобы всё было видно в git-диффе.
    /// </summary>
    public class KorraInput : MonoBehaviour
    {
        InputAction _move, _jump, _run, _sprint, _bark, _pause;

        public float Move => _move.ReadValue<float>();          // -1..1 по X
        public bool JumpPressed => _jump.WasPressedThisFrame();
        public bool JumpHeld => _jump.IsPressed();
        public bool RunHeld => _run.IsPressed();
        public bool SprintPressed => _sprint.WasPressedThisFrame();
        public bool BarkPressed => _bark.WasPressedThisFrame();
        public bool BarkHeld => _bark.IsPressed();               // удержание X у куста = сохранение
        public bool BarkReleased => _bark.WasReleasedThisFrame();
        public bool PausePressed => _pause.WasPressedThisFrame();

        void Awake()
        {
            // Ось движения: стик, крестовина, A/D, стрелки
            _move = new InputAction("Move", InputActionType.Value);
            _move.AddBinding("<Gamepad>/leftStick/x");
            _move.AddBinding("<Gamepad>/dpad/x");
            _move.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
            _move.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");

            // buttonSouth/East/West/North = A/B/X/Y на Xbox и Steam Deck
            _jump = Button("Jump", "<Gamepad>/buttonSouth", "<Keyboard>/space");
            _run = Button("Run", "<Gamepad>/buttonEast", "<Keyboard>/leftShift");
            _sprint = Button("Sprint", "<Gamepad>/buttonNorth", "<Keyboard>/leftCtrl");
            _bark = Button("Bark", "<Gamepad>/buttonWest", "<Keyboard>/e");
            _pause = Button("Pause", "<Gamepad>/start", "<Keyboard>/escape");
        }

        static InputAction Button(string name, params string[] paths)
        {
            var a = new InputAction(name, InputActionType.Button);
            foreach (var p in paths) a.AddBinding(p);
            return a;
        }

        void OnEnable() { foreach (var a in All()) a.Enable(); }
        void OnDisable() { foreach (var a in All()) a.Disable(); }
        InputAction[] All() => new[] { _move, _jump, _run, _sprint, _bark, _pause };
    }
}
