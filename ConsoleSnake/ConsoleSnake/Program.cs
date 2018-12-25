using System;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Colorful;

using Console = Colorful.Console;

namespace ConsoleSnake
{
    class Coord : IEquatable<Coord>
    {
        public int X;
        public int Y;
        public bool Equals(Coord other)
        {
            return X == other.X && Y == other.Y;
        }
    }

    class Button
    {
        public static readonly int Width = Console.WindowWidth / 4;
        public const int Height = 3;
        public string Label;
        public int Y;
        public Coord[] Area
        {
            get
            {
                return new Coord[] {
                    new Coord { X = Console.WindowWidth / 2 - Width / 2 , Y = Y },
                    new Coord { X = Console.WindowWidth / 2 - Width / 2 + Width , Y = Y + Height - 1 }
                };
            }
        }
        public Action Click;
    }

    class Record : IComparable<Record>
    {
        public int TotalLifes;
        public int TotalScore;
        public int TotalCures;
        public int TotalPoisons;
        public TimeSpan GameTime;
        public static string File = "Records.json";

        public int CompareTo(Record obj)
        {
            if (TotalScore != obj.TotalScore)
                return TotalScore.CompareTo(obj.TotalScore);
            else
                return 1 - GameTime.CompareTo(obj.GameTime);
        }
    }

    enum Orientation
    {
        Left,
        Up,
        Right,
        Down
    }

    enum WindowType
    {
        Welcome,
        Menu,
        Records,
        Game,
        GameOver
    }

    class MainClass
    {
        private static double ySpeed = 1000.0 / 3;
        public static double DeltaTime
        {
            get
            {
                if (Direction == Orientation.Up || Direction == Orientation.Down)
                    return ySpeed;
                return ySpeed / 2;
            }
            set
            {
                ySpeed = value;
            }
        }
        public static Coord[] Field = {
            new Coord
            {
                X = (Console.WindowWidth - 41) / 2,
                Y = (Console.WindowHeight - 15) / 2
            },
            new Coord { X = 41, Y = 15 }};
        public static List<Coord> Snake = new List<Coord>
        {
            new Coord {
                X = Field[0].X + Field[1].X / 2 + 1,
                Y = Field[0].Y + Field[1].Y / 2 + 1}
        };
        public static Orientation Direction = Orientation.Up;
        public static List<Coord> Apples = new List<Coord>();
        public static List<Coord> Poisons = new List<Coord>();
        public static List<Coord> Cures = new List<Coord>();
        public static object Locker = new object();
        public static bool Playing = true;
        public const string StringGameOver = "GAME OVER!";
        public const string StringWelcome = "Welcome to ConsoleSnake game!";
        public static int Score;
        public static int Lifes = 3;
        public static Random Rand = new Random(DateTime.Now.Millisecond);
        public static DateTime ZeroTime;
        public static bool Pause;
        public static bool CanPlay = true;
        public static int ApplesCount = 5;
        public static int PoisonsMaxCount = 2;
        public static int CuresMaxCount = 2;
        public static WindowType WType;
        public static List<Button> Buttons = new List<Button> {
            new Button { Y = Console.WindowHeight / 4 - Button.Height, Label = "Play", Click = Play },
            new Button { Y = 2 * Console.WindowHeight / 4 - Button.Height, Label = "Records", Click = ShowRecords },
            new Button { Y = 3 * Console.WindowHeight / 4 - Button.Height, Label = "Exit", Click = Exit }
        };
        public static int ActiveButton = 0;
        public static Record RecordInfo;
        public static SortedSet<Record> AllRecords = new SortedSet<Record>();
        public static TimeSpan GameTime = TimeSpan.Zero;

        public static void Play()
        {
            WType = WindowType.Game;
        }

        public static void ShowRecords()
        {
            Pause = true;
            WType = WindowType.Records;
        }

        public static void Exit()
        {
            Playing = false;
        }

        public static string Center(string s, int width, char fill = ' ')
        {
            return s.PadLeft((width + s.Length) / 2, fill).PadRight(width, fill);
        }

        public static List<Coord> GetEmpty()
        {
            var map = new List<Coord>();
            for (int y = Field[0].Y + 1; y < Field[1].Y + Field[0].Y + 1; ++y)
                for (int x = Field[0].X + 1; x < Field[1].X + Field[0].X + 1; ++x)
                    map.Add(new Coord { X = x, Y = y });
            foreach (var coord in Snake)
                map.Remove(coord);
            foreach (var coord in Apples)
                map.Remove(coord);
            return map;
        }

        public static Coord Move(Coord coord, bool reverse = false)
        {
            int r = reverse ? -1 : 1;
            switch (Direction)
            {
                case Orientation.Right:
                    return new Coord { X = coord.X + r, Y = coord.Y };
                case Orientation.Left:
                    return new Coord { X = coord.X - r, Y = coord.Y };
                case Orientation.Up:
                    return new Coord { X = coord.X, Y = coord.Y - r };
                case Orientation.Down:
                    return new Coord { X = coord.X, Y = coord.Y + r };
            }
            return coord;
        }

        public static void Freeze(int milliseconds)
        {
            Pause = true;
            if (milliseconds >= 0)
            {
                Thread.Sleep(milliseconds);
                ResetTime();
            }
            else
                CanPlay = false;
        }

        public static void Revive()
        {
            Freeze(667);
            Snake.Clear();
            Snake.Add(new Coord
            {
                X = Field[0].X + Field[1].X / 2 + 1,
                Y = Field[0].Y + Field[1].Y / 2 + 1
            });
        }

        public static void Generate()
        {
            var gen = Math.Max(ApplesCount - Apples.Count, 0);
            var map = GetEmpty();
            while (gen > 0)
            {
                var index = Rand.Next() % map.Count;
                Apples.Add(map[index]);
                map.RemoveAt(index);
                --gen;
            }
        }

        public static void AddBonus()
        {
            var map = GetEmpty();
            var index = Rand.Next() % map.Count;
            if (Cures.Count < CuresMaxCount)
            {
                Cures.Add(map[index]);
                map.RemoveAt(index);
            }
            else if (Poisons.Count < CuresMaxCount)
            {
                Poisons.Add(map[index]);
                map.RemoveAt(index);
            }
        }

        public static void DrawStatus()
        {
            var status = $" Lifes: {Lifes} | Score: {Score} ";
            Console.SetCursorPosition(0, 0);
            var fore = Console.ForegroundColor;
            var back = Console.BackgroundColor;
            Console.BackgroundColor = Color.DarkCyan;
            Console.ForegroundColor = Color.White;
            Console.Write(Center(status, Console.WindowWidth, '='));
            Console.ForegroundColor = fore;
            Console.BackgroundColor = back;
        }

        public static void DrawField()
        {
            Console.SetCursorPosition(Field[0].X, Field[0].Y);
            Console.Write("+" + new string('-', Field[1].X) + "+");
            Console.SetCursorPosition(Field[0].X, Field[0].Y + Field[1].Y + 1);
            Console.Write("+" + new string('-', Field[1].X) + "+");
            Console.SetCursorPosition(0, Field[0].Y + 1);
            for (int line = 0; line < Field[1].Y; ++line)
                Console.WriteLine(new string(' ', Field[0].X) + "|".PadRight(Field[1].X + 1) + "|");
            var fore = Console.ForegroundColor;
            foreach (var apple in Apples)
            {
                Console.SetCursorPosition(apple.X, apple.Y);
                Console.ForegroundColor = Color.Red;
                Console.Write(((apple.X ^ apple.Y) & 0x01) == 1 ? '9' : '6');
            }
            foreach (var cure in Cures)
            {
                Console.SetCursorPosition(cure.X, cure.Y);
                Console.ForegroundColor = Color.Yellow;
                Console.Write("C");
            }
            foreach (var poison in Poisons)
            {
                Console.SetCursorPosition(poison.X, poison.Y);
                Console.ForegroundColor = Color.Magenta;
                Console.Write("P");
            }
            Console.ForegroundColor = fore;
        }

        public static void Reverse()
        {
            Direction = (Orientation)(((int)Direction + 2) % 4);
        }

        public static void DrawSnake()
        {
            var head = Snake[Snake.Count - 1];
            var newXY = Move(head);
            if (Snake.IndexOf(newXY) >= 0)
            {
                if (Snake.IndexOf(newXY) == Snake.Count - 2)
                {
                    Reverse();
                    newXY = Move(head);
                }
                else
                {
                    SubstractLife();
                    return;
                }
            }
            if (newXY.X == Field[0].X || newXY.X == Field[0].X + Field[1].X + 1
                || newXY.Y == Field[0].Y || newXY.Y == Field[0].Y + Field[1].Y + 1)
            {
                SubstractLife();
                return;
            }
            if (Cures.Remove(newXY))
            {
                ++Lifes;
                ++RecordInfo.TotalCures;
                ++RecordInfo.TotalLifes;
            }
            if (Poisons.Remove(newXY))
            {
                Console.SetCursorPosition(newXY.X, newXY.Y);
                Console.Write(' ');
                ++RecordInfo.TotalPoisons;
                SubstractLife();
                return;
            }
            var fore = Console.ForegroundColor;
            Console.ForegroundColor = Color.LimeGreen;
            if (!Apples.Remove(newXY))
                Snake.RemoveAt(0);
            else
            {
                ++Score;
                ++RecordInfo.TotalScore;
            }
            foreach (var coord in Snake)
            {
                Console.SetCursorPosition(coord.X, coord.Y);
                Console.Write('#');
            }
            Snake.Add(newXY);
            Console.SetCursorPosition(newXY.X, newXY.Y);
            Console.Write('O');
            Console.ForegroundColor = fore;
        }

        public static void SubstractLife()
        {
            --Lifes;
            if (Lifes > 0)
                Revive();
            else
            {
                SaveRecord();
                DrawGameOver();
            }
        }

        public static string TimeToString(TimeSpan time)
        {
            return (time.TotalMinutes >= 1.0 ? $"{(int)time.TotalMinutes} min " : "") +
                $"{time.Seconds} s {time.Milliseconds} ms";
        }

        public static void DrawGameOver()
        {
            Pause = true;
            CanPlay = false;
            WType = WindowType.GameOver;
            Console.BackgroundColor = Color.DarkMagenta;
            Console.Clear();
            Console.ForegroundColor = Color.White;
            Console.SetCursorPosition(0, Console.WindowHeight / 2 - 1);
            Console.Write(Center(StringGameOver, Console.WindowWidth));
            Console.Write(Center($"Total score: {Score} | Game time: " + TimeToString(GameTime), Console.WindowWidth));
        }

        public static void HandleEvents()
        {
            ConsoleKeyInfo keyInfo;
            do
            {
                lock (Locker)
                {
                    keyInfo = Console.ReadKey(true);
                    if (WType == WindowType.Game)
                    {
                        if (keyInfo.Key == ConsoleKey.W || keyInfo.Key == ConsoleKey.UpArrow)
                            Direction = Orientation.Up;
                        else if (keyInfo.Key == ConsoleKey.S || keyInfo.Key == ConsoleKey.DownArrow)
                            Direction = Orientation.Down;
                        else if (keyInfo.Key == ConsoleKey.D || keyInfo.Key == ConsoleKey.RightArrow)
                            Direction = Orientation.Right;
                        else if (keyInfo.Key == ConsoleKey.A || keyInfo.Key == ConsoleKey.LeftArrow)
                            Direction = Orientation.Left;
                        else if (keyInfo.Key == ConsoleKey.Spacebar)
                        {
                            if (CanPlay)
                                Freeze(-1);
                            else
                                CanPlay = true;
                        }
                        else if (keyInfo.Key == ConsoleKey.Escape)
                        {
                            Pause = true;
                            WType = WindowType.Menu;
                        }
                    }
                    else if (WType == WindowType.Menu)
                    {
                        if (keyInfo.Key == ConsoleKey.W || keyInfo.Key == ConsoleKey.UpArrow
                            || keyInfo.Key == ConsoleKey.A || keyInfo.Key == ConsoleKey.LeftArrow)
                            ActiveButton = (ActiveButton + Buttons.Count - 1) % Buttons.Count;
                        else if (keyInfo.Key == ConsoleKey.S || keyInfo.Key == ConsoleKey.DownArrow
                            || keyInfo.Key == ConsoleKey.D || keyInfo.Key == ConsoleKey.RightArrow)
                            ActiveButton = (ActiveButton + 1) % Buttons.Count;
                        else if (keyInfo.Key == ConsoleKey.Escape)
                            WType = WindowType.Game;
                        else if (keyInfo.Key == ConsoleKey.Enter || keyInfo.Key == ConsoleKey.Spacebar)
                            Buttons[ActiveButton].Click();
                    }
                    else if (WType == WindowType.Records)
                    {
                        if (keyInfo.Key == ConsoleKey.Escape)
                            WType = WindowType.Menu;
                    }
                }
            } while (true);
        }

        public static void Draw()
        {
            Console.BackgroundColor = Color.Black;
            Console.Clear();
            DrawField();
            DrawStatus();
            var sum = Poisons.Count + Cures.Count;
            var max = PoisonsMaxCount + CuresMaxCount;
            if (Rand.NextDouble() < 0.1 * (1 - sum / max))
                AddBonus();
            DrawTime();
            DrawSnake();
        }

        public static string Fill(string str, int len)
        {
            return Center(str, (str.Length / len + 1) * len);
        }

        public static void DrawTime()
        {
            var label = Fill($"Elapsed: {TimeToString(GameTime)}", 4);
            var fore = Console.ForegroundColor;
            var back = Console.BackgroundColor;
            Console.SetCursorPosition((Console.WindowWidth - label.Length) / 2, 1);
            Console.WriteWithGradient(label, Color.LightSeaGreen, Color.Red, 4);
        }

        public static void DrawWelcome()
        {
            Console.BackgroundColor = Color.DarkSlateBlue;
            Console.Clear();
            Console.ForegroundColor = Color.LightYellow;
            Console.SetCursorPosition(0, Console.WindowHeight / 2 - 1);
            Console.Write(Center(StringWelcome, Console.WindowWidth));
        }

        public static void DrawButton(Button button)
        {
            var area = button.Area;
            var foreColor = Console.ForegroundColor;
            var backColor = Console.BackgroundColor;
            if (Buttons.IndexOf(button) == ActiveButton)
                Console.BackgroundColor = Color.White;
            else
                Console.BackgroundColor = Color.LightGray;
            Console.ForegroundColor = Color.DarkSlateBlue;
            Console.SetCursorPosition(area[0].X, area[0].Y);
            Console.Write(new string(' ', Button.Width));
            Console.SetCursorPosition(area[0].X, area[0].Y + 1);
            Console.Write(Center(button.Label, Button.Width));
            Console.SetCursorPosition(area[0].X, area[0].Y + 2);
            Console.Write(new string(' ', Button.Width));
            Console.ForegroundColor = foreColor;
            Console.BackgroundColor = backColor;
        }

        public static void DrawRecord(int line, Record record)
        {
            Console.SetCursorPosition(0, 2 + line * 2);
            Console.Write(new string(' ', 16) + $"#{line + 1} Score: {record.TotalScore} - Lifes: {record.TotalLifes} - Bonuses: {record.TotalPoisons + record.TotalCures} - Gaming Time: {TimeToString(record.GameTime)}");
        }

        public static void DrawMenu()
        {
            Console.BackgroundColor = Color.DarkSlateBlue;
            Console.Clear();
            Buttons.ForEach(DrawButton);
        }

        public static void DrawRecords()
        {
            Console.BackgroundColor = Color.DarkSlateBlue;
            Console.Clear();
            if (AllRecords.Count > 0)
            {
                var records = new List<Record>(AllRecords.Reverse());
                for (int i = 0; i < records.Count; ++i)
                    DrawRecord(i, records[i]);
            }
            else
            {
                Console.SetCursorPosition(0, Console.WindowHeight / 2);
                Console.Write(Center("Empty list . . .", Console.WindowWidth));
            }
        }

        public static void LoadRecords()
        {
            try
            {
                StreamReader file = new StreamReader(Record.File);
                string text = file.ReadToEnd();
                var data = JsonConvert.DeserializeObject<SortedSet<Record>>(text);
                if (data != null)
                    AllRecords = JsonConvert.DeserializeObject<SortedSet<Record>>(text);
                file.Close();
            }
            catch (Exception)
            {
                return;
            }
        }

        public static void SaveRecord()
        {
            StreamWriter file = new StreamWriter(Record.File);
            RecordInfo.GameTime = GameTime;
            AllRecords.Add(RecordInfo);
            string text = JsonConvert.SerializeObject(AllRecords);
            file.Write(text);
            file.Close();
        }

        public static void ResetTime()
        {
            ZeroTime = DateTime.Now;
        }

        public static void AccumulateTime()
        {
            GameTime += DateTime.Now - ZeroTime;
            ZeroTime = DateTime.Now;
        }

        public static void Main(string[] args)
        {
            LoadRecords();
            RecordInfo = new Record
            {
                TotalScore = Score,
                TotalLifes = Lifes,
                TotalCures = 0,
                TotalPoisons = 0
            };
            WType = WindowType.Welcome;
            Console.ForegroundColor = Color.White;
            Console.CursorVisible = false;
            var time = DateTime.Now;
            var thread = new Thread(HandleEvents);
            thread.Start();
            ResetTime();
            while (thread.IsAlive && Playing)
            {
                if (CanPlay)
                {
                    if (Pause)
                    {
                        Pause = false;
                        Thread.Sleep((int)DeltaTime);
                    }
                    else
                    {
                        var delta = DateTime.Now - time;
                        if (delta.TotalMilliseconds > DeltaTime)
                        {
                            time = time.AddMilliseconds(DeltaTime);
                            switch (WType)
                            {
                                case WindowType.Game:
                                    Generate();
                                    Draw();
                                    AccumulateTime();
                                    break;
                                case WindowType.Welcome:
                                    DrawWelcome();
                                    Freeze(1500);
                                    WType = WindowType.Menu;
                                    break;
                                case WindowType.Menu:
                                    DrawMenu();
                                    break;
                                case WindowType.Records:
                                    DrawRecords();
                                    break;
                            }
                            if (WType != WindowType.Game)
                                ResetTime();
                        }
                    }
                }
                else
                {
                    Thread.Sleep((int)DeltaTime);
                    ResetTime();
                }
            }
            thread.Abort();
            Console.BackgroundColor = Color.Black;
            Console.Clear();
            string byeString = Fill("Goodbye for now . . . :^)", 3);
            Console.SetCursorPosition((Console.WindowWidth - byeString.Length) / 2, Console.WindowHeight / 2);
            Console.WriteWithGradient(byeString, Color.DarkOrange, Color.Yellow, 3);
            Console.ReadKey(true);
        }
    }
}
