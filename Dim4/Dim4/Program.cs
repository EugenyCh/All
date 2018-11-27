using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using System.Globalization;
using SFML.System;
using SFML.Graphics;
using SFML.Window;

namespace Dim4
{
    public class DuplicateKeyComparer<TKey> : IComparer<TKey> where TKey : IComparable
    {
        public int Compare(TKey x, TKey y)
        {
            int result = x.CompareTo(y);

            if (result == 0)
                return 1; // y >= x
            else
                return result;
        }
    }

    class Scene
    {
        readonly RenderWindow window;
        readonly double sideInPixels;
        readonly Font font;
        readonly int dim;
        readonly int angn;
        readonly double speed = Math.PI / 3;
        readonly Text label;
        readonly ConvexShape originalShape = new ConvexShape();
        Figure figure = new Figure();
        List<int[]> planes = new List<int[]>();
        Vector<double> angle;
        Vector<double> movement;
        double targetFPS = 60.0;
        double fixedDeltaTime = 1.0 / 60.0;
        double realDeltaTime = 0.0;
        double direction = 1.0;
        double velocity = 1.0;

        int PlaneComparator(int[] x, int[] y)
        {
            return (Math.Pow(2, x[0]) + Math.Pow(2, x[1])).CompareTo(Math.Pow(2, y[0]) + Math.Pow(2, y[1]));
        }

        readonly Dictionary<Keyboard.Key, int> keyValues = new Dictionary<Keyboard.Key, int>
        {
            { Keyboard.Key.Num1, 0 },
            { Keyboard.Key.Num2, 1 },
            { Keyboard.Key.Num3, 2 },
            { Keyboard.Key.Num4, 3 },
            { Keyboard.Key.Num5, 4 },
            { Keyboard.Key.Num6, 5 },
            { Keyboard.Key.Num7, 6 },
        };

        public Scene()
        {
            ContextSettings settings = new ContextSettings
            {
                AntialiasingLevel = 8
            };
            window = new RenderWindow(
                VideoMode.FullscreenModes[0],
                "N-Dimentional Scene",
                Styles.Fullscreen,
                settings);

            sideInPixels = Math.Min(window.Size.X, window.Size.Y) / 4;
            figure.Load("model4d.txt");
            font = new Font("Inconsolata.otf");
            label = new Text
            {
                CharacterSize = 12,
                Color = Color.Green,
                Position = new Vector2f(0, 0),
                Font = font
            };
            dim = figure.Dimension();
            angn = dim * (dim - 1) / 2;
            angle = Vector<double>.Build.Dense(angn);
            movement = Vector<double>.Build.Dense(angn);
            MakePlanes();
            ScaleFigure(sideInPixels);

            originalShape.Position = new Vector2f(window.Size.X / 2, window.Size.Y / 2);
            originalShape.FillColor = new Color(70, 130, 180, 255);

            window.Closed += Closed;
            window.KeyPressed += KeyPressed;
            window.KeyReleased += KeyReleased;
        }

        public Scene(uint windowWidth, uint windowHeight)
        {
            ContextSettings settings = new ContextSettings
            {
                AntialiasingLevel = 8
            };
            window = new RenderWindow(
                new VideoMode(windowWidth, windowHeight),
                "N-Dimentional Scene",
                Styles.Default,
                settings);
            
            sideInPixels = Math.Min(windowWidth, windowHeight) / 4;
            figure.Load("model4d.txt");
            font = new Font("Inconsolata.otf");
            label = new Text
            {
                CharacterSize = 12,
                Color = Color.Green,
                Position = new Vector2f(0, 0),
                Font = font
            };
            dim = figure.Dimension();
            angn = dim * (dim - 1) / 2;
            angle = Vector<double>.Build.Dense(angn);
            movement = Vector<double>.Build.Dense(angn);
            MakePlanes();
            ScaleFigure(sideInPixels);

            originalShape.Position = new Vector2f(windowWidth / 2, windowHeight / 2);
            originalShape.FillColor = new Color(70, 130, 180, 255);

            window.Closed += Closed;
            window.KeyPressed += KeyPressed;
            window.KeyReleased += KeyReleased;
        }

        public void SetTargetFPS(double fps)
        {
            targetFPS = fps;
            fixedDeltaTime = 1 / targetFPS;
        }

        public double RealFPS()
        {
            return 1 / realDeltaTime;
        }

        public void Run()
        {
            Clock clock = new Clock();
            double timeSinceLastUpdate = 0;
            while (window.IsOpen)
            {
                window.DispatchEvents();
                timeSinceLastUpdate += clock.Restart().AsSeconds();
                while (timeSinceLastUpdate > fixedDeltaTime)
                {
                    realDeltaTime = timeSinceLastUpdate;
                    timeSinceLastUpdate -= fixedDeltaTime;
                    Update();
                }
                Render();
            }
        }

        void Closed(object sender, EventArgs e)
        {
            window.Close();
        }

        void KeyPressed(object sender, KeyEventArgs e)
        {
            if (keyValues.ContainsKey(e.Code) && keyValues[e.Code] < angn)
                movement[keyValues[e.Code]] = 1.0;
            if (e.Code == Keyboard.Key.Space)
                direction = -1.0;
            if (e.Code == Keyboard.Key.LShift || e.Code == Keyboard.Key.RShift)
                velocity = 0.5;
            if (e.Code == Keyboard.Key.S && e.Control)
                ScreenShot();
        }

        void KeyReleased(object sender, KeyEventArgs e)
        {
            if (keyValues.ContainsKey(e.Code) && keyValues[e.Code] < angn)
                movement[keyValues[e.Code]] = 0.0;
            if (e.Code == Keyboard.Key.Space)
                direction = 1.0;
            if (e.Code == Keyboard.Key.LShift || e.Code == Keyboard.Key.RShift)
                velocity = 1.0;
        }

        Matrix<double> MakePlaneRotationMatrix(double angle, int xa, int xb)
        {
            if (Math.Min(xa, xb) < 0 || Math.Max(xa, xb) > dim || dim < 2)
                return Matrix<double>.Build.Dense(1, 1, Math.Cos(angle));
            var matrix = Matrix<double>.Build.DenseDiagonal(dim, dim, 1.0);
            matrix[xa, xa] = Math.Cos(angle);
            matrix[xb, xb] = matrix[xa, xa];
            matrix[xa, xb] = ((xa < xb) ? -1.0 : 1.0) * Math.Sin(angle);
            matrix[xb, xa] = -matrix[xa, xb];
            return matrix;
        }

        void MakePlanes()
        {
            for (int k1 = 0; k1 < dim - 1; ++k1)
                for (int k2 = k1 + 1; k2 < dim; ++k2)
                {
                    if (((k1 + k2) & 1) == 1)
                        planes.Add(new int[] { k1, k2 });
                    else
                        planes.Add(new int[] { k2, k1 });
                }
            planes.Sort(PlaneComparator);
        }

        int[] GetPlane(int number)
        {
            return planes[number];
        }

        void Rotate(Vector<double> angle)
        {
            var matrix = Matrix<double>.Build.DenseDiagonal(dim, dim, 1.0);
            for (int n = 0; n < angle.Count; ++n)
            {
                var plane = GetPlane(n);
                matrix *= MakePlaneRotationMatrix(angle[n], plane[0], plane[1]);
            }

            int size = figure.polygons.Keys.Count;
            for (int c = 0; c < size; ++c)
            {
                var polygon = figure.polygons[c];
                for (int a = 0; a < polygon.Count; ++a)
                    polygon[a] *= matrix;
            }
        }

        void Update()
        {
            Vector<double> deltaAngle = movement * speed * direction * velocity * realDeltaTime;
            angle += deltaAngle;
            for (int i = 0; i < angle.Count; ++i)
                if (Math.Abs(angle[i]) >= 2.0 * Math.PI)
                    angle[i] -= Math.Floor(angle[i] / (2.0 * Math.PI)) * (2.0 * Math.PI);
            Rotate(deltaAngle);
        }

        void ScaleFigure(double aspect)
        {
            int size = figure.polygons.Keys.Count;
            for (int c = 0; c < size; ++c)
            {
                var polygon = figure.polygons[c];
                for (int a = 0; a < polygon.Count; ++a)
                    polygon[a] *= aspect;
            }
        }

        void RenderFigure()
        {
            int size = figure.polygons.Keys.Count;
            if (figure.Dimension() > 2)
            {
                var zBuffer = new SortedList<double, int>(new DuplicateKeyComparer<double>());
                for (int c = 0; c < size; ++c)
                {
                    var polygon = figure.polygons[c];
                    Vector<double> normal = polygon.Aggregate((s, v) => s + v).Normalize(1);
                    zBuffer.Add(normal[2], c);
                }

                for (int c = 0; c < size; ++c)
                {
                    int id = zBuffer.Values[c];
                    var polygon = figure.polygons[id];
                    Color color = originalShape.FillColor;

                    double zp = (zBuffer.Keys[c] + 1) * 0.5;
                    color.R = (byte)(color.R * zp);
                    color.G = (byte)(color.G * zp);
                    color.B = (byte)(color.B * zp);
                    color.A = (byte)(color.A * zp);

                    ConvexShape shape = new ConvexShape
                    {
                        Position = originalShape.Position,
                        FillColor = color
                    };
                    
                    shape.SetPointCount((uint)polygon.Count);
                    for (int i = 0; i < polygon.Count; ++i)
                        shape.SetPoint((uint)i, new Vector2f((float)polygon[i][0], -(float)polygon[i][1]));
                    
                    window.Draw(shape);
                }
            }
        }

        public void ScreenShot()
        {
            Texture texture = new Texture(window.Size.X, window.Size.Y);
            texture.Update(window);
            Image screenshot = texture.CopyToImage();
            var now = DateTime.Now;
            screenshot.SaveToFile($"{now.ToString("yyyy-MM-dd_HH-mm-ss")}.{now.Millisecond.ToString("D3")}.png");
        }

        public void Render()
        {
            window.Clear();
            RenderFigure();
            var text = $"Target FPS: {targetFPS.ToString("F1")}\n" +
                       $"Real FPS: {RealFPS().ToString("F1")}\n" +
                       $"Fixed Delta Time: {(fixedDeltaTime * 1000).ToString("F1")}ms\n" +
                       $"Real Delta Time: {(realDeltaTime * 1000).ToString("F1")}ms";
            for (int n = 0; n < angn; ++n)
            {
                var plane = GetPlane(n);
                text += $"\nAngle(x{plane[0] + 1},x{plane[1] + 1}) = {(angle[n] * 180 / Math.PI).ToString("F1")}";
            }
            label.DisplayedString = text;
            window.Draw(label);
            window.Display();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Scene scene = new Scene();
            scene.Run();
        }
    }
}
