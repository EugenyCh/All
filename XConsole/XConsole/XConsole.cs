using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace XConsole
{
    public class XChar
    {
        public char Value = '+';
        public Color Back = XColor.DefaultBack;
        public Color Fore = XColor.DefaultFore;
        public FontStyle Style;
    }

    public class XBuffer
    {
        private int width = 1;
        private int height = 1;
        private XChar[,] buffer = new XChar[1, 1];

        public XBuffer(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width
        {
            get
            {
                return width;
            }
            set
            {
                if (value > 0)
                {
                    XChar[,] clone = new XChar[height, value];
                    if (value < width)
                    {
                        for (int y = 0; y < height; ++y)
                            for (int x = 0; x < value; ++x)
                                clone[y, x] = buffer[y, x];
                    }
                    else
                    {
                        for (int y = 0; y < height; ++y)
                        {
                            for (int x = 0; x < width; ++x)
                                clone[y, x] = buffer[y, x];
                            for (int x = width; x < value; ++x)
                                clone[y, x] = new XChar();
                        }
                    }
                    buffer = clone;
                    width = value;
                }
            }
        }

        public int Height
        {
            get
            {
                return height;
            }
            set
            {
                if (value > 0)
                {
                    XChar[,] clone = new XChar[value, width];
                    if (value < height)
                    {
                        for (int y = 0; y < value; ++y)
                            for (int x = 0; x < width; ++x)
                                clone[y, x] = buffer[y, x];
                    }
                    else
                    {
                        for (int y = 0; y < height; ++y)
                            for (int x = 0; x < width; ++x)
                                clone[y, x] = buffer[y, x];
                        for (int y = height; y < value; ++y)
                            for (int x = 0; x < width; ++x)
                                clone[y, x] = new XChar();
                    }
                    buffer = clone;
                    height = value;
                }
            }
        }

        public XChar this[int y, int x]
        {
            get
            {
                if (0 <= x && x < Width && 0 <= y && y < Height)
                    return buffer[y, x];
                return null;
            }
            set
            {
                if (value != null && 0 <= x && x < Width && 0 <= y && y < Height)
                    buffer[y, x] = value;
            }
        }
    }

    public class XColor
    {
        public static Color Dark = Color.FromArgb(255, 27, 38, 49);
        public static Color Border = Color.FromArgb(255, 52, 73, 94);
        public static Color Lighting = Color.FromArgb(64, 255, 255, 255);
        public static Color Darking = Color.FromArgb(64, 0, 0, 0);
        public static Color DefaultBack = Color.Black;
        public static Color DefaultFore = Color.LightGray;
        public static Color Intersection(Color a, Color b)
        {
            var alpha = (float)b.A / 255;
            var alpha0 = (float)a.A / 255;
            return Color.FromArgb(
                (int)((alpha + alpha0 - alpha * alpha0) * 255),
                (int)((1.0 - alpha) * a.R + alpha * b.R),
                (int)((1.0 - alpha) * a.G + alpha * b.G),
                (int)((1.0 - alpha) * a.B + alpha * b.B));
        }
    }

    public class XRectangle
    {
        public static Point[] Generate(int left, int top, int width, int height, float border)
        {
            var halfDown = (int)border / 2;
            var halfUp = (int)border - halfDown;
            return new Point[]
            {
                new Point(halfDown, height),
                new Point(halfDown, halfDown),
                new Point(width - halfUp, halfDown),
                new Point(width - halfUp, height)
            };
        }
    }

    public class XTabControl : Control
    {
        private List<XTabPage> pages = new List<XTabPage>();
        private Font font;
        private FontFamily fontFamily = FontFamily.GenericMonospace;
        private float fontSize = 13;
        private FontStyle fontStyle = FontStyle.Regular;

        public int ActiveTab = 0;
        public int CapMinHeight = 24;
        public int CapMinWidth = 80;
        public int CapPadding = 6;

        public Font CapFont = new Font(FontFamily.GenericMonospace, 16, FontStyle.Bold);

        public FontFamily Family
        {
            get => fontFamily;
            set
            {
                fontFamily = value;
                UpdateFont();
            }
        }

        public float FontSize
        {
            get => fontSize;
            set
            {
                fontSize = value;
                UpdateFont();
            }
        }

        public FontStyle Style
        {
            get => fontStyle;
            set
            {
                fontStyle = value;
                UpdateFont();
            }
        }

        public int Count => pages.Count;

        private void UpdateFont() => font = new Font(fontFamily, fontSize, fontStyle);

        private void Initialize()
        {
            font = new Font(FontFamily.GenericMonospace, FontSize);
            Paint += XPaint;
        }

        private void FillDefault() => pages.Add(new XTabPage());

        public XTabControl()
        {
            Initialize();
            FillDefault();
        }

        public XTabControl(XTabPage page)
        {
            Initialize();
            if (page == null)
                FillDefault();
            else
                pages.Add(page);
        }

        public XTabControl(string newTabTitle) : this()
        {
            Initialize();
            pages.Add(new XTabPage(newTabTitle));
        }

        public XTabControl(IEnumerable<XTabPage> list)
        {
            Initialize();
            pages.AddRange(list);
            if (pages.Count == 0)
                FillDefault();
        }

        public XTabControl(IEnumerable<string> list)
        {
            Initialize();
            foreach (var title in list)
                pages.Add(new XTabPage(title));
            if (pages.Count == 0)
                FillDefault();
        }

        public void Add() => pages.Add(new XTabPage());
        public void Add(XTabPage newPage) => pages.Add(newPage);
        public void Add(string newPageTitle) => pages.Add(new XTabPage(newPageTitle));
        public XTabPage this[int index] => pages[index];
        public void Remove(XTabPage page) => pages.Remove(page);
        public void Remove(int index) => pages.RemoveAt(index);

        private void XPaint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(XColor.Dark);
            var lastX = 0;
            var pen = new Pen(XColor.Dark, 2);
            var brush = new SolidBrush(XColor.Border);
            /*foreach (var page in pages)
            {
                var size = TextRenderer.MeasureText(page.Title, CapFont);
                size.Width += CapPadding * 2;
                size.Height += CapPadding * 2;
                size.Width = Math.Max(CapMinWidth, size.Width);
                size.Height = Math.Max(CapMinHeight, size.Height);
                var lines = XRectangle.Generate(lastX, 0, size.Width, size.Height, pen.Width);
                g.FillRectangle(brush, lastX, 0, size.Width, size.Height);
            }*/
        }
    }

    public class XTabPage
    {
        public string Title = "New page";
        public XBuffer Buffer = new XBuffer(80, 120);

        public XTabPage() { }
        public XTabPage(string title) => Title = title;
    }

    public class XConsole : Form
    {
        XTabControl control;

        public XConsole()
        {
            FormBorderStyle = FormBorderStyle.Fixed3D;
            control = new XTabControl(new string[] { "Tab 1", "Tab 2", "Tab 3" });
            control.Location = new Point(32, 32);
            control.Size = new Size(ClientSize.Width - 64, ClientSize.Height - 64);
            control.Add();
            control.Add("2");
            control.Add("3");
            Controls.Add(control);
        }

        public void Run()
        {
            Application.Run(new XConsole());
        }
    }
}
