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
        public static Dictionary<States, string> Templates = new Dictionary<States, string>
        {
            [States.SYMBOL] = @"^\s*(\w+)\s*",
            [States.DEF] = @"^\s*\=\s*",
            [States.INTEGER] = @"^\s*(\d+)",
            [States.POLYGON] = @"^\s*polygon(?:[^\S\n]*\n)+",
            [States.NEW_LINE] = @"^[^\S\n]*(?:\n[^\S\n]*)+|\Z",
            [States.DOUBLE] = @"^[^\S\n]*(-?(?:\d*(?:\.\d+))|-?(?:\d+(?:\.\d*)?))",
            [States.END] = @"^[^\S\n]*end(?:[^\S\n]*\n)+"
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
            Regex regex = new Regex(Syntax.Templates[States.SYMBOL]);
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
                    regex = new Regex(Syntax.Templates[States.DEF]);
                    match = regex.Match(In);
                    if (match.Success)
                    {
                        In = In.Substring(match.Length);
                        return true;
                    }
                    break;
                case States.INTEGER:
                    regex = new Regex(Syntax.Templates[States.INTEGER]);
                    match = regex.Match(In);
                    if (match.Success)
                    {
                        if (Dimension != 0)
                        {
                            Console.WriteLine($"The value of dimension has been setted already ({Dimension})!");
                            return false;
                        }
                        Dimension = int.Parse(match.Groups[1].Value);
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
                    regex = new Regex(Syntax.Templates[States.NEW_LINE]);
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
            Regex regex = new Regex(Syntax.Templates[States.DOUBLE]);
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
            Regex regex = new Regex(Syntax.Templates[States.NEW_LINE]);
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
            Regex regex = new Regex(Syntax.Templates[States.POLYGON]);
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
            Regex regex = new Regex(Syntax.Templates[States.END]);
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
        public int Dimension { get; set; }
        public List<Polygon> Polygons { get; set; }

        public Figure() { }
        public Figure(string pathToFile)
        {
            Update(pathToFile);
        }
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
        public bool Trace()
        {
            Tracer tracer = new Tracer(stream);
            StateMachine<int> machine = new StateMachine<int>();
            machine.ZeroState((int)States.MAIN);
            machine.AddState((int)States.DEF);
            machine.AddState((int)States.SYMBOL);
            machine.AddState((int)States.DOUBLE);
            machine.AddState((int)States.INTEGER);
            machine.AddState((int)States.VECTOR);
            machine.AddState((int)States.POLYGON);
            machine.AddState((int)States.NEW_LINE);
            // --- MAIN ---
            machine.AddTransition((int)States.MAIN, (int)States.SYMBOL, tracer.SymbolIntroTracer);
            machine.AddTransition((int)States.MAIN, (int)States.POLYGON, tracer.NewPolygonTracer);
            // --- SYMBOL DEF INTEGER ---
            machine.AddTransition((int)States.SYMBOL, (int)States.DEF, tracer.SymbolTracer);
            machine.AddTransition((int)States.DEF, (int)States.INTEGER, tracer.SymbolTracer);
            machine.AddTransition((int)States.INTEGER, (int)States.MAIN, tracer.SymbolTracer);
            // --- VECTOR ---
            machine.AddTransition((int)States.VECTOR, (int)States.DOUBLE, tracer.VectorDoubleTracer);
            machine.AddTransition((int)States.DOUBLE, (int)States.DOUBLE, tracer.VectorDoubleTracer);
            machine.AddTransition((int)States.DOUBLE, (int)States.VECTOR, tracer.VectorEndTracer);
            // --- POLYGON ---
            machine.AddTransition((int)States.POLYGON, (int)States.VECTOR);
            machine.AddTransition((int)States.VECTOR, (int)States.MAIN, tracer.VectorToMainTracer);
            machine.Startup();
            Dimension = tracer.Dimension;
            Polygons = tracer.Polygons;
            return true;
        }
    }
}
