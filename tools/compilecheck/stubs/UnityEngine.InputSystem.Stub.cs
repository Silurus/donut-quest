// Заглушки UnityEngine.InputSystem для проверки компиляции на сервере,
// где нет Unity. НЕ попадает в игру: лежит вне папки Assets.
// Если код начнёт использовать новые методы пакета — добавь их сюда же.
using System;

namespace UnityEngine.InputSystem
{
    public enum InputActionType { Value = 0, Button = 1, PassThrough = 2 }

    public class InputBindingSyntax
    {
        public InputBindingSyntax With(string name, string binding) => this;
    }

    public class InputAction
    {
        public string name;
        public InputActionType type;
        public InputAction() { }
        public InputAction(string n) { name = n; }
        public InputAction(string n, InputActionType t) { name = n; type = t; }
        public InputAction(string n, InputActionType t, string binding) { name = n; type = t; }

        public InputAction AddBinding(string path) => this;
        public InputBindingSyntax AddCompositeBinding(string composite) => new InputBindingSyntax();
        public T ReadValue<T>() where T : struct => default(T);
        public bool WasPressedThisFrame() => false;
        public bool WasReleasedThisFrame() => false;
        public bool IsPressed() => false;
        public void Enable() { }
        public void Disable() { }
    }
}

namespace UnityEngine.InputSystem.Controls
{
    public class StubControl { }
}
