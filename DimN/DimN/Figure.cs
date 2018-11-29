using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using StateMachine;
using System.Text.RegularExpressions;
using MathNet.Numerics.LinearAlgebra;

namespace DimN
{
    using Vertex = Vector<double>;
    using Polygon = List<Vector<double>>;

    public enum States
    {
        MAIN,
        DEF,
        SYMBOL,
        DOUBLE,
        INTEGER,
        VECTOR,
        POLYGON,
        NEW_LINE,
        END
    }

    public static class Syntax
    {
        public static Dictionary<States, Regex> Templates = new Dictionary<States, Regex>
        {
            [States.SYMBOL] = new Regex(@"^\s*(\w+)\s*"),
            [States.DEF] = new Regex(@"^\s*\=\s*"),
            [States.INTEGER] = new Regex(@"^\s*(\d+)"),
            [States.POLYGON] = new Regex(@"^\s*polygon(?:[^\S\n]*\n)+", RegexOptions.IgnoreCase),
            [States.NEW_LINE] = new Regex(@"^[^\S\n]*(?:\n[^\S\n]*)+|\Z"),
            [States.DOUBLE] = new Regex(@"^[^\S\n]*(-?(?:\d*(?:\.\d+))|-?(?:\d+(?:\.\d*)?))"),
            [States.END] = new Regex(@"^[^\S\n]*end(?:[^\S\n]*\n)+", RegexOptions.IgnoreCase)
        };
    }
    
    public class Tracer
    {
        public States OutterState { get; set; }
        public int Dimension { get; set; }
        public string In { get; set; }
        public int LastPolygon { get; set; } = 0;
        public List<double> VectorCounter { get; set; } = new List<double>();
        public List<Polygon> Polygons { get; set; } = new List<Polygon>();
        public static NumberFormatInfo NumberFormat = new NumberFormatInfo
        {
            NumberDecimalSeparator = "."
        };

        public Tracer() => In = "";
        public Tracer(string stream) => In = stream;
        public static string VectorToString(Vector<double> vector)
        {
            return "(" + (from n in vector.ToArray() select n.ToString(NumberFormat)).Aggregate((s, n) => s + ", " + n) + ")";
        }
        public bool SymbolIntroTracer(State<int> stateFrom, State<int> stateTo, ref double weight)
        {
            Regex regex = Syntax.Templates[States.SYMBOL];
            Match match = regex.Match(In);
            if (match.Success & match.Groups[1].Value.ToLower() == "dim")
            {
                In = In.Substring(match.Length);
                return true;
            }
            return false;
        }
        public bool SymbolTracer(State<int> stateFrom, State<int> stateTo, ref double weight)
        {
            Regex regex;
            Match match;
            switch ((States)stateTo.Identifier)
            {
                case States.DEF:
                    regex = Syntax.Templates[States.DEF];
                    match = regex.Match(In);
                    if (match.Success)
                    {
                        In = In.Substring(match.Length);
                        return true;
                    }
                    break;
                case States.INTEGER:
                    regex = Syntax.Templates[States.INTEGER];
                    match = regex.Match(In);
                    if (match.Success)
                    {
                        int parsed = int.Parse(match.Groups[1].Value);
                        if (Dimension != 0 & Dimension != parsed)
                        {
                            Console.WriteLine($"The value of dimension has been setted already ({Dimension})!");
                            return false;
                        }
                        Dimension = parsed;
                        if (Dimension == 0)
                        {
                            Console.WriteLine($"Dimension must be greater than zero!");
                            return false;
                        }
                        In = In.Substring(match.Length);
                        return true;
                    }
                    break;
                case States.MAIN:
                    regex = Syntax.Templates[States.NEW_LINE];
                    match = regex.Match(In);
                    if (match.Success)
                    {
                        In = In.Substring(match.Length);
                        return true;
                    }
                    break;
            }
            return false;
        }
        public bool VectorDoubleTracer(State<int> stateFrom, State<int> stateTo, ref double weight)
        {
            Regex regex = Syntax.Templates[States.DOUBLE];
            Match match = regex.Match(In);
            if (match.Success)
            {
                if (Dimension == 0)
                {
                    Console.WriteLine("You might be set a 'dim' to the positive integer number!");
                    return false;
                }
                VectorCounter.Add(double.Parse(match.Groups[1].Value, NumberFormat));
                In = In.Substring(match.Length);
                return true;
            }
            return false;
        }
        public bool VectorEndTracer(State<int> stateFrom, State<int> stateTo, ref double weight)
        {
            Regex regex = Syntax.Templates[States.NEW_LINE];
            Match match = regex.Match(In);
            if (match.Success)
            {
                Vertex vertex = Vertex.Build.Dense(VectorCounter.ToArray());
                if (vertex.Count() != Dimension)
                {
                    Console.WriteLine($"Vector {VectorToString(vertex)} must have length equals to {Dimension}!");
                    return false;
                }
                Polygons[LastPolygon].Add(vertex);
                VectorCounter.Clear();
                In = In.Substring(match.Length);
                return true;
            }
            return false;
        }
        public bool NewPolygonTracer(State<int> stateFrom, State<int> stateTo, ref double weight)
        {
            Regex regex = Syntax.Templates[States.POLYGON];
            Match match = regex.Match(In);
            if (match.Success)
            {
                Polygons.Add(new Polygon());
                In = In.Substring(match.Length);
                return true;
            }
            return false;
        }
        public bool VectorToMainTracer(State<int> stateFrom, State<int> stateTo, ref double weight)
        {
            Regex regex = Syntax.Templates[States.END];
            Match match = regex.Match(In);
            if (match.Success)
            {
                ++LastPolygon;
                In = In.Substring(match.Length);
                return true;
            }
            return false;
        }
    }

    public class Figure
    {
        StreamReader file;
        string stream = "";
        StateMachine<int> Machine = new StateMachine<int>();
        Tracer FTracer = new Tracer();
        public int Dimension { get; set; }
        public List<Polygon> Polygons { get; set; } = new List<Polygon>();

        public Figure() { }
        public Figure(string pathToFile) => Update(pathToFile);
        public bool Update(string pathToFile)
        {
            if (file != null)
                file.Close();
            file = new StreamReader(pathToFile);
            return file != null;
        }
        public void Load()
        {
            string line;
            while ((line = file.ReadLine()) != null)
                stream += line + "\n";
        }
        public void Free()
        {
            stream = "";
            file.Close();
        }
        public void CreateMachine()
        {
            Machine.ZeroState((int)States.MAIN);
            Machine.AddState((int)States.DEF);
            Machine.AddState((int)States.SYMBOL);
            Machine.AddState((int)States.DOUBLE);
            Machine.AddState((int)States.INTEGER);
            Machine.AddState((int)States.VECTOR);
            Machine.AddState((int)States.POLYGON);
            Machine.AddState((int)States.NEW_LINE);
            Machine.AddState((int)States.END);
            // --- MAIN ---
            Machine.AddTransition((int)States.MAIN, (int)States.SYMBOL, FTracer.SymbolIntroTracer);
            Machine.AddTransition((int)States.MAIN, (int)States.POLYGON, FTracer.NewPolygonTracer);
            Machine.AddTransition((int)States.MAIN, (int)States.END);
            // --- SYMBOL DEF INTEGER ---
            Machine.AddTransition((int)States.SYMBOL, (int)States.DEF, FTracer.SymbolTracer);
            Machine.AddTransition((int)States.DEF, (int)States.INTEGER, FTracer.SymbolTracer);
            Machine.AddTransition((int)States.INTEGER, (int)States.MAIN, FTracer.SymbolTracer);
            // --- VECTOR ---
            Machine.AddTransition((int)States.VECTOR, (int)States.DOUBLE, FTracer.VectorDoubleTracer);
            Machine.AddTransition((int)States.DOUBLE, (int)States.DOUBLE, FTracer.VectorDoubleTracer);
            Machine.AddTransition((int)States.DOUBLE, (int)States.VECTOR, FTracer.VectorEndTracer);
            // --- POLYGON ---
            Machine.AddTransition((int)States.POLYGON, (int)States.VECTOR);
            Machine.AddTransition((int)States.VECTOR, (int)States.MAIN, FTracer.VectorToMainTracer);
        }
        public bool Trace()
        {
            FTracer.In = stream;
            if (!Machine.Startup())
                return false;
            if (Dimension != 0 & FTracer.Dimension != Dimension)
                return false;
            Dimension = FTracer.Dimension;
            Polygons = FTracer.Polygons;
            return true;
        }
    }
}
