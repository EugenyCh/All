using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DimN
{
    static public class Program
    {
        static public void Main(string[] args)
        {
            Figure figure = new Figure("cube.txt");
            figure.Load();
            figure.Trace();
            foreach (var polygon in figure.Polygons)
                foreach (var vertex in polygon)
                    Console.WriteLine(Tracer.VectorToString(vertex));
        }
    }
}
