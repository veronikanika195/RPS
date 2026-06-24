using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using System.Linq;
using DescartesApp.Models;

namespace DescartesApp.Services
{
    public static class FileManager
    {
        public static void SaveTable(string filePath, DataTable data)
        {
            var sb = new StringBuilder();
            sb.AppendLine("X,YPositive,YNegative");
            foreach (var p in data.Points)
            {
                string xStr = p.X.ToString(CultureInfo.InvariantCulture);
                string ypStr = p.YPositive.HasValue ? p.YPositive.Value.ToString(CultureInfo.InvariantCulture) : "";
                string ynStr = p.YNegative.HasValue ? p.YNegative.Value.ToString(CultureInfo.InvariantCulture) : "";
                sb.AppendLine($"{xStr},{ypStr},{ynStr}");
            }
            File.WriteAllText(filePath, sb.ToString());
        }

        public static DataTable LoadTable(string filePath)
        {
            // Проверяем расширение файла
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (extension != ".csv")
                throw new Exception("Поддерживаются только файлы с расширением .csv.");

            var lines = File.ReadAllLines(filePath);
            if (lines.Length < 2)
                throw new Exception("Файл пуст или не содержит данных.");

            // Проверяем заголовок: ожидаем "X,YPositive,YNegative" или хотя бы три колонки
            string header = lines[0].Trim();
            var headerParts = header.Split(',');
            if (headerParts.Length < 3)
                throw new Exception("Неверный формат файла: ожидается три колонки (X, YPositive, YNegative).");

            // Если заголовок не совпадает точно, но есть три колонки – всё равно пробуем парсить
            var data = new DataTable();
            int parsedCount = 0;
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var parts = lines[i].Split(',');
                if (parts.Length < 3) continue; // пропускаем строки с недостаточным количеством колонок

                try
                {
                    double x = double.Parse(parts[0].Trim(), CultureInfo.InvariantCulture);
                    double? yp = string.IsNullOrEmpty(parts[1].Trim()) ? (double?)null : double.Parse(parts[1].Trim(), CultureInfo.InvariantCulture);
                    double? yn = string.IsNullOrEmpty(parts[2].Trim()) ? (double?)null : double.Parse(parts[2].Trim(), CultureInfo.InvariantCulture);
                    data.Points.Add(new DataPoint { X = x, YPositive = yp, YNegative = yn });
                    parsedCount++;
                }
                catch (FormatException)
                {
                    // Если в строке ошибка, просто пропускаем
                    continue;
                }
            }

            if (parsedCount == 0)
                throw new Exception("Не удалось распарсить ни одной строки данных. Проверьте формат файла.");

            return data;
        }
    }
}