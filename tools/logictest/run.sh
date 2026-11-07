#!/bin/bash
# Офлайн-тесты игровой логики Donut Quest без Unity.
#
# Подменяет UnityEngine (Application.persistentDataPath, Debug, JsonUtility)
# и компилирует НАСТОЯЩИЕ скрипты из Assets/Scripts/Gameplay.
# Ловит ошибки в работе с файлами сейва, которые в редакторе видно только руками.
#
# Запуск: bash tools/logictest/run.sh
# Выход:  0 — все тесты прошли, 1 — есть падения.

set -uo pipefail
cd "$(dirname "$0")/../.." || exit 1

OUT=/tmp/donutquest-logictest
mkdir -p "$OUT"

declare -a SOURCES=(
  tools/logictest/UnityEngine.Fake.cs
  Assets/Scripts/Gameplay/SaveSystem.cs
  tools/logictest/SaveSystemTests.cs
)

echo "→ Компиляция тестов"
mcs -target:exe -langversion:latest -out:"$OUT/logictest.exe" \
  -r:System.Core.dll "${SOURCES[@]}" 2>&1
if [ ! -f "$OUT/logictest.exe" ]; then
  echo "✗ Тесты не скомпилировались"
  exit 1
fi

echo "→ Запуск"
mono "$OUT/logictest.exe"
