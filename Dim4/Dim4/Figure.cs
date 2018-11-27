using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Globalization;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

namespace Dim4
{
    class Figure
    {
        enum ReaderState
        {
            Outter,
            Polygon
        }

        enum KeyWord
        {
            DimensionSetup,
            End,
            Polygon,
            Space
        }

        readonly Dictionary<KeyWord, Regex> templates = new Dictionary<KeyWord, Regex>
        {
            { KeyWord.DimensionSetup, new Regex(@"^dimension\s*[=]\s*([1-9])$") },
            { KeyWord.Polygon, new Regex(@"^polygon$") },
            { KeyWord.End, new Regex(@"^end$") },
            { KeyWord.Space, new Regex(@"\s+") }
        };

        private int? lastDimension = null;
        public SortedList<int, List<Vector<double>>> polygons = new SortedList<int, List<Vector<double>>>();

        public int Dimension()
        {
            return lastDimension != null ? (int)lastDimension : 0;
        }
        
        public Figure() { }

        public Figure(string path)
        {
            Load(path);
        }

        public void Free()
        {
            polygons.Clear();
        }

        static string[] SeparateByRegex(string text, Regex regex)
        {
            var parts = new List<string>();
            MatchCollection matches = regex.Matches(text);
            int first = 0;
            foreach (Match match in matches)
            {
                parts.Add(text.Substring(first, match.Index - first));
                first = match.Index + match.Length;
            }
            if (first < text.Length)
                parts.Add(text.Substring(first, text.Length - first));
            return parts.ToArray();
        }

        public void Load(string path, bool rewrite = false)
        {
            StreamReader reader;
            try
            {
                reader = new StreamReader(path);
            }
            catch
            {
                Console.WriteLine($"Файл {path} не найден");
                return;
            }
            if (rewrite)
                Free();
            int begin = polygons.Count;
            string line;
            int lineNumber = 0;
            ReaderState readerState = ReaderState.Outter;
            while (!reader.EndOfStream)
            {
                ++lineNumber;
                line = reader.ReadLine().Trim();
                if (readerState == ReaderState.Outter)
                {
                    if (templates[KeyWord.DimensionSetup].IsMatch(line))
                    {
                        if (lastDimension == null)
                        {
                            lastDimension = int.Parse(templates[KeyWord.DimensionSetup].Match(line).Groups[1].Value);
                            continue;
                        }
                        else
                        {
                            Console.WriteLine("Измерение не может быть перезадано");
                            break;
                        }
                    }
                    if (templates[KeyWord.Polygon].IsMatch(line))
                    {
                        readerState = ReaderState.Polygon;
                        continue;
                    }
                }
                if (readerState == ReaderState.Polygon)
                {
                    if (templates[KeyWord.End].IsMatch(line))
                    {
                        ++begin;
                        readerState = ReaderState.Outter;
                        continue;
                    }
                    if (lastDimension == null)
                    {
                        Console.WriteLine("Вы не задали размерность векторов");
                        break;
                    }

                    List<double> coords;
                    try
                    {
                        coords = new List<double>(from coord in SeparateByRegex(line, templates[KeyWord.Space]) select double.Parse(coord, CultureInfo.InvariantCulture));
                    }
                    catch
                    {
                        Console.WriteLine($"Строка \"{line}\" под номером {lineNumber} должна описывать вектор");
                        break;
                    }
                    
                    if (coords.Count() == 0)
                    {
                        Console.WriteLine($"Не найден вектор в строке {lineNumber}");
                        break;
                    }
                    var vertex = Vector<double>.Build.Dense(coords.Count());
                    for (int i = 0; i < coords.Count(); ++i)
                        vertex[i] = coords[i];
                    if (coords.Count() != lastDimension)
                    {
                        Console.WriteLine($"Вектор ({(from v in vertex select v.ToString()).Aggregate((s, u) => $"{s}, {u}")}) должен иметь размерность, равную {lastDimension}");
                        break;
                    }
                    if (polygons.ContainsKey(begin))
                        polygons[begin].Add(vertex);
                    else
                        polygons.Add(begin, new List<Vector<double>>
                        {
                            vertex
                        });
                    continue;
                }
                if (line.Length > 0)
                {
                    Console.WriteLine($"Неизвестная последовательность: \"{line}\"");
                    break;
                }
            }
            reader.Close();
        }
    }
}
