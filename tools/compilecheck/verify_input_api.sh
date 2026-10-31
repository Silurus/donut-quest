#!/usr/bin/env bash
# Проверяет, что заглушки Input System в tools/compilecheck/stubs
# описывают сигнатуры, которые реально есть в пакете com.unity.inputsystem.
#
# Зачем: заглушка живёт в репозитории и может разойтись с настоящим API.
# Тогда компиляция «чистая», а Unity в редакторе ругается. Этот скрипт ловит такое расхождение.
#
# Запуск: bash tools/compilecheck/verify_input_api.sh
set -euo pipefail
cd "$(dirname "$0")/../.."

VERSION="1.20.1"
CACHE="${TMPDIR:-/tmp}/unity-inputsystem-$VERSION"
PKG="$CACHE/package/InputSystem/Runtime/Actions"
DEVICES="$CACHE/package/InputSystem/Runtime/Devices"

if [ ! -d "$PKG" ]; then
  echo "→ Качаю com.unity.inputsystem $VERSION (один раз)"
  mkdir -p "$CACHE"
  curl -sL "https://packages.unity.com/com.unity.inputsystem/-/com.unity.inputsystem-$VERSION.tgz" \
    | tar xzf - -C "$CACHE"
fi

# Каждая строка: <подстрока сигнатуры>, которую наш код использует.
# Если сигнатура пропала из пакета — скрипт падает.
declare -a SIGS=(
  "public InputAction(string name"
  "public unsafe bool WasPressedThisFrame()"
  "public unsafe bool IsPressed()"
  "public unsafe bool WasReleasedThisFrame()"
  "public TValue ReadValue<TValue>()"
  "public static BindingSyntax AddBinding(this InputAction action, string path"
  "public static CompositeSyntax AddCompositeBinding(this InputAction action"
  "public CompositeSyntax With(string name"
  "public enum InputActionType"
)

fail=0
for sig in "${SIGS[@]}"; do
  if grep -rqF "$sig" "$PKG"; then
    echo "  ✓ ${sig:0:60}"
  else
    echo "  ✗ НЕТ В ПАКЕТЕ: $sig"
    fail=1
  fi
done

# Имена контролов геймпада, которые мы биндим строками ("<Gamepad>/buttonWest").
# В Devices/Gamepad.cs они объявлены либо атрибутом [InputControl(name = "...")],
# либо прямо именем поля в структуре состояния (так объявлен leftStick).
for btn in buttonSouth buttonEast buttonWest buttonNorth leftStick dpad start; do
  if grep -qE "name = \"$btn\"|public [A-Za-z0-9]+ $btn;" "$DEVICES/Gamepad.cs"; then
    echo "  ✓ кнопка $btn"
  else
    echo "  ✗ кнопка $btn не найдена"
    fail=1
  fi
done

if [ "$fail" -ne 0 ]; then
  echo "✗ API Input System разошёлся с заглушками"
  exit 1
fi
echo "✓ Все используемые API Input System есть в пакете $VERSION"
