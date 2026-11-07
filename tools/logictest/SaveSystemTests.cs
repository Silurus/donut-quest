// Офлайн-тесты логики сохранения. Запуск: bash tools/logictest/run.sh
// Тестируется НАСТОЯЩИЙ Assets/Scripts/Gameplay/SaveSystem.cs — подменён только UnityEngine.
using System;
using System.IO;
using DonutQuest.Gameplay;
using UnityEngine;

static class SaveSystemTests
{
    static int passed, failed;

    static void Check(bool ok, string name, string detail = "")
    {
        if (ok) { passed++; Console.WriteLine("  ✓ " + name); }
        else { failed++; Console.WriteLine("  ✗ " + name + (detail.Length > 0 ? " — " + detail : "")); }
    }

    static void ResetDir()
    {
        if (Directory.Exists(Application.persistentDataPath))
            Directory.Delete(Application.persistentDataPath, true);
        Directory.CreateDirectory(Application.persistentDataPath);
    }

    static SaveData Sample() => new SaveData
    {
        posX = 8.5f, posY = 0.25f, posZ = 0f,
        cucumbers = 3, cheese = 1,
        playTimeSeconds = 42.5f,
    };

    static int Main()
    {
        ResetDir();
        Console.WriteLine("SaveSystem (файл: " + SaveSystem.Path + ")\n");

        Console.WriteLine("Пустой старт");
        Check(!SaveSystem.HasSave, "HasSave == false без файла");
        Check(SaveSystem.Load() == null, "Load() == null без файла");

        Console.WriteLine("\nЗапись и чтение");
        var d = Sample();
        Check(SaveSystem.Save(d, out string err1), "Save() вернул true", err1);
        Check(SaveSystem.HasSave, "HasSave == true после Save");
        Check(string.IsNullOrEmpty(d.savedAtUtc) == false, "savedAtUtc проставлен");

        var back = SaveSystem.Load();
        Check(back != null, "Load() вернул объект");
        if (back != null)
        {
            Check(Math.Abs(back.posX - 8.5f) < 0.001f, "posX сохранился", "получено " + back.posX);
            Check(Math.Abs(back.posY - 0.25f) < 0.001f, "posY сохранился", "получено " + back.posY);
            Check(back.cucumbers == 3, "cucumbers сохранился");
            Check(back.cheese == 1, "cheese сохранился");
            Check(Math.Abs(back.playTimeSeconds - 42.5f) < 0.001f, "playTimeSeconds сохранился");
            Check(Math.Abs(back.Position.x - 8.5f) < 0.001f, "Position собирается из полей");
            Check(back.savedAtUtc == d.savedAtUtc, "savedAtUtc читается обратно");
        }

        Console.WriteLine("\nАтомарность (перезапись поверх существующего файла)");
        // Второй Save идёт по ветке File.Replace — самый рискованный путь.
        var d2 = Sample();
        d2.cucumbers = 77; d2.posX = -3.25f;
        Check(SaveSystem.Save(d2, out string err2), "повторный Save() вернул true", err2);
        Check(!File.Exists(SaveSystem.Path + ".tmp"), "временный файл .tmp убран");
        var back2 = SaveSystem.Load();
        Check(back2 != null && back2.cucumbers == 77, "перезапись применилась (cucumbers == 77)");
        Check(back2 != null && Math.Abs(back2.posX + 3.25f) < 0.001f, "отрицательная координата сохранилась");

        Console.WriteLine("\nБитый сейв");
        File.WriteAllText(SaveSystem.Path, "{ это не json ");
        Debug.Errors.Clear();
        Check(SaveSystem.Load() == null, "Load() вернул null на битом файле");
        Check(Debug.Errors.Count > 0, "ошибка залогирована, а не проглочена");
        Check(SaveSystem.HasSave, "HasSave остаётся true (файл есть, пусть и битый)");

        Console.WriteLine("\nУдаление");
        SaveSystem.Delete();
        Check(!SaveSystem.HasSave, "HasSave == false после Delete");
        Check(SaveSystem.Load() == null, "Load() == null после Delete");
        SaveSystem.Delete(); // повторное удаление не должно падать
        Check(true, "повторный Delete() не бросает исключение");

        Console.WriteLine("\nЦикл «Продолжить → Сохранить» — значения не должны теряться");
        var orig = Sample();
        orig.cucumbers = 12; orig.cheese = 5;
        SaveSystem.Save(orig, out _);
        var loaded = SaveSystem.Load();          // что делает пункт «Продолжить»
        SaveSystem.Save(loaded, out _);          // что делает зона сохранения сразу после
        var again = SaveSystem.Load();
        Check(again != null && again.cucumbers == 12 && again.cheese == 5,
              "огурчики и сыр пережили цикл загрузки-сохранения");

        Console.WriteLine("\nСовместимость версии");
        Check(again != null && again.version == 1, "version == 1");

        Console.WriteLine();
        Console.WriteLine(failed == 0
            ? $"✓ Все тесты прошли: {passed}"
            : $"✗ Провалено {failed} из {passed + failed}");
        return failed == 0 ? 0 : 1;
    }
}
