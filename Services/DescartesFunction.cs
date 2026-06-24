using System;

namespace DescartesApp.Services
{
    public static class DescartesFunction
    {
        public static double L(double a) => 3 * a / Math.Sqrt(2);

        public static (double? positive, double? negative) Compute(double x, double a)
        {
            double l = L(a);
            double denominator = l - 3 * x;
            if (Math.Abs(denominator) < 1e-12)
                return (null, null);

            double radicand = (l + x) / denominator;
            if (radicand < 0)
                return (null, null);

            double sqrtVal = Math.Sqrt(radicand);
            double yAbs = x * sqrtVal;
            return (yAbs, -yAbs);
        }

        public static (double left, double right)? GetDomain(double a)
        {
            double l = L(a);
            if (a > 0)
                return (-l, l / 3);
            else if (a < 0)
                return (l / 3, -l);
            else
                return null;
        }
    }
}