﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;

/*
 * 3rd theme. Fixed-point iteration
 */
namespace NumAn_1_03
{
    class Program
    {
        static double t0 = 1.0;
        static double U0 = 1.3;
        static double T = 2.0;
        static int N = 100;
        static double epsilon = Math.Pow(0.1, 10);
        static double C = 1.3 + Math.Sqrt(2.69);

        static double F(double t, double u) => u / t + Math.Sqrt(1 + (u / t) * (u / t));
        static double U(double t) => t * t * 0.5 * C - 0.5 / C;
        static double Distance(double a, double b) => Math.Abs(a - b);
        static Vector<double> SplitT()
        {
            Vector<double> t = Vector<double>.Build.Dense(N);
            double delta = T - t0;
            t[0] = t0;
            for (int i = 1; i < N; ++i)
                t[i] = t0 + delta * i / N;
            return t;
        }

        static void Main(string[] args)
        {
            double h = (T - t0) / N;
            double h_1_2 = h * 0.5;
            var y = Vector<double>.Build.Dense(N);
            var u = Vector<double>.Build.Dense(N);
            var err = Vector<double>.Build.Dense(N);
            var growing = Vector<double>.Build.Dense(N);
            var t = SplitT();
            y[0] = u[0] = U0;
            err[0] = 0.0;
            growing[0] = 0.0;
            for (int i = 1; i < N; ++i)
            {
                double f = F(t[i - 1], y[i - 1]);
                double yPrev = y[i - 1] + h * f;
                while (Math.Abs(y[i] - yPrev) >= epsilon)
                {
                    yPrev = y[i];
                    y[i] = y[i - 1] + h_1_2 * (f + F(t[i], yPrev));
                }
                u[i] = U(t[i]);
                err[i] = Distance(y[i], u[i]);
                growing[i] = err[i] - err[i - 1];
            }
            string labelN = "#";
            string labelU = $"U(0..{N - 1})";
            string labelY = $"Y(0..{N - 1})";
            string labelErr = $"Err(0..{N - 1})";
            string labelGrowing = $"Growing of error";
            int lenN = Math.Max(N.ToString().Length, labelN.Length);
            int lenU = labelU.Length;
            int lenY = labelY.Length;
            int lenErr = labelErr.Length;
            int lenGrowing = labelGrowing.Length;
            for (int i = 0; i < N; ++i)
            {
                if (lenU < u[i].ToString().Length)
                    lenU = u[i].ToString().Length;
                if (lenY < y[i].ToString().Length)
                    lenY = y[i].ToString().Length;
                if (lenErr < err[i].ToString().Length)
                    lenErr = err[i].ToString().Length;
                if (lenGrowing < growing[i].ToString().Length)
                    lenGrowing = growing[i].ToString().Length;
            }

            string format =
                "{0," + lenN.ToString() +
                "} {1," + lenU.ToString() +
                "} {2," + lenY.ToString() +
                "} {3," + lenErr.ToString() +
                "} {4,-" + lenGrowing.ToString() + "}";
            Console.WriteLine(format, labelN, labelU, labelY, labelErr, labelGrowing);
            for (int i = 0; i < N; ++i)
                Console.WriteLine(format, i, u[i], y[i], err[i], growing[i]);
        }
    }
}
