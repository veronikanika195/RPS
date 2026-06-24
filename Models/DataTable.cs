using DescartesApp.Services;
using System;
using System.Collections.Generic;

namespace DescartesApp.Models
{
    public class DataTable
    {
        public List<DataPoint> Points { get; } = new List<DataPoint>();
        public double A { get; set; }

        public void Generate(double left, double right, double step, double a)
        {
            A = a;
            Points.Clear();
            if (step <= 0) throw new ArgumentException("Шаг должен быть положительным");
            if (left > right) throw new ArgumentException("Левая граница больше правой");

            for (double x = left; x <= right + step / 2; x += step)
            {
                var (pos, neg) = DescartesFunction.Compute(x, a);
                Points.Add(new DataPoint { X = x, YPositive = pos, YNegative = neg });
            }
        }
    }
}