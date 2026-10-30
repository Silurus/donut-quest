#!/bin/bash
# Проверка компиляции C#-скриптов Donut Quest без Unity.
#
# Unity Editor на сервере не запустить (нужен GPU и ≥8 ГБ RAM), но сам C#
# компилируется через Mono с эталонными сборками UnityEngine.
# Ловит синтаксис, опечатки в именах, несовпадение сигнатур и забытые using.
#
# Запуск:  bash tools/compilecheck/check.sh
# Выход:   0 — всё скомпилировалось, 1 — есть ошибки.
#
# Ограничения: проверяются только типы UnityEngine и UnityEngine.InputSystem
# (заглушки). Реальные API Unity 6 могут отличаться — финальная проверка
# всё равно за тобой в редакторе.

set -uo pipefail
cd "$(dirname "$0")/../.." || exit 1

REFS_DIR="${UNITY_REFS_DIR:-/root/.hermes/cache/scratch/unityrefs/sdk/lib}"
CSC="${CSC:-mcs}"
OUT="/tmp/donutquest-check"
mkdir -p "$OUT"

if [ ! -f "$REFS_DIR/UnityEngine.dll" ]; then
  echo "✗ Нет эталонных сборок Unity в $REFS_DIR"
  echo "  Скачай пакет unity3d.sdk с nuget.org и распакуй, либо задай UNITY_REFS_DIR."
  exit 1
fi

fail=0

echo "→ Игровые скрипты (Assets/Scripts/Gameplay)"
$CSC -target:library -langversion:latest -nowarn:0169,0414 \
  -out:"$OUT/Gameplay.dll" \
  -r:"$REFS_DIR/UnityEngine.dll" -r:System.Core.dll \
  Assets/Scripts/Gameplay/*.cs tools/compilecheck/stubs/*.cs 2>&1
[ ${PIPESTATUS[0]:-0} -ne 0 ] && fail=1

if [ ! -f "$OUT/Gameplay.dll" ]; then
  echo "✗ Игровые скрипты не собрались — редакторные проверять нет смысла"
  exit 1
fi

echo "→ Редакторные скрипты (Assets/Scripts/Editor)"
$CSC -target:library -langversion:latest -nowarn:0169,0414 \
  -out:"$OUT/Editor.dll" \
  -r:"$REFS_DIR/UnityEngine.dll" -r:"$REFS_DIR/UnityEditor.dll" \
  -r:"$OUT/Gameplay.dll" -r:System.Core.dll \
  Assets/Scripts/Editor/*.cs 2>&1
[ ${PIPESTATUS[0]:-0} -ne 0 ] && fail=1

if [ $fail -eq 0 ]; then
  echo "✓ Компиляция чистая"
else
  echo "✗ Есть ошибки — см. вывод выше"
fi
exit $fail