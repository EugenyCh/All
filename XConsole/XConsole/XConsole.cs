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
        protected int width = 1;
        protected int height = 1;
        protected XChar[,] buffer = new XChar[1, 1];

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
        public static Color DefaultBack = Color.FromArgb(255, 63, 63, 63);
        public static Color DefaultFore = Color.FromArgb(255, 132, 170, 217);
        public static Color InactiveFore = Color.FromArgb(255, 80, 96, 112);
        public static Color Lighting = Color.FromArgb(64, 255, 255, 255);
        public static Color Darking = Color.FromArgb(64, 0, 0, 0);
    }

    public class XRectangle
    {
        public static Point[] Generate(int left, int top, int width, int height, float border)
        {
            var halfDown = (int)border / 2;
            var halfUp = (int)border - halfDown;
            return new Point[]
            {
                new Point(left + halfDown, top + height - halfUp),
                new Point(left + halfDown, top + halfDown),
                new Point(left + width - halfUp, top + halfDown),
                new Point(left + width - halfUp, top + height - halfUp)
            };
        }
    }

    public class XTabControl : Control
    {
        protected List<XTabPage> pages = new List<XTabPage>();
        protected List<Rectangle> pageCaps;
        protected Font font;
        protected FontFamily fontFamily = FontFamily.GenericMonospace;
        protected float fontSize = 13;
        protected FontStyle fontStyle = FontStyle.Regular;
        protected float penWidth = 2;
        protected int firstX = 0;
        protected int first = 0;
        protected string toLeft = "<";
        protected string toRight = ">";
        protected string newTab = "[+]";

        public int ActiveTab = 0;
        public int CapMinWidth = 80;
        public int CapPadding = 6;

        public int ButtonWidth(string s)
        {
            var width = TextRenderer.MeasureText(s, CapFont).Width;
            return width + CapPadding * 2;
        }

        public int ToLeftWidth => ButtonWidth(toLeft);
        public int ToRightWidth => ButtonWidth(toRight);
        public int NewTabWidth => ButtonWidth(newTab);

        public int RightSpace => ToLeftWidth + ToRightWidth;

        public Font CapFont = new Font(FontFamily.GenericMonospace, 13, FontStyle.Bold);

        public int CapHeight => CapFont.Height + CapPadding * 2;

        public int FirstPage => first;

        public void NextPage()
        {
            first = (first + 1) % Count;
            firstX = 0;
            for (int i = 0; i < first; ++i)
                firstX -= pageCaps[i].Width;
            UpdateCaps();
            Refresh();
        }

        public void PrevPage()
        {
            first = (Count + first - 1) % Count;
            firstX = 0;
            for (int i = 0; i < first; ++i)
                firstX -= pageCaps[i].Width;
            UpdateCaps();
            Refresh();
        }

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

        protected void UpdateFont() => font = new Font(fontFamily, fontSize, fontStyle);

        protected void FillDefault() => pages.Add(new XTabPage());

        public XTabControl()
        {
            UpdateFont();
            FillDefault();
            UpdateCaps();
        }

        public XTabControl(XTabPage page)
        {
            UpdateFont();
            if (page == null)
                FillDefault();
            else
                pages.Add(page);
            UpdateCaps();
        }

        public XTabControl(string newTabTitle) : this()
        {
            UpdateFont();
            pages.Add(new XTabPage(newTabTitle));
            UpdateCaps();
        }

        public XTabControl(IEnumerable<XTabPage> list)
        {
            UpdateFont();
            pages.AddRange(list);
            if (pages.Count == 0)
                FillDefault();
            UpdateCaps();
        }

        public XTabControl(IEnumerable<string> list)
        {
            UpdateFont();
            foreach (var title in list)
                pages.Add(new XTabPage(title));
            if (pages.Count == 0)
                FillDefault();
            UpdateCaps();
        }

        public void Add() => pages.Add(new XTabPage());
        public void Add(XTabPage newPage) => pages.Add(newPage);
        public void Add(string newPageTitle) => pages.Add(new XTabPage(newPageTitle));
        public XTabPage this[int index] => pages[index];
        public void Remove(XTabPage page) => pages.Remove(page);
        public void Remove(int index) => pages.RemoveAt(index);

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SetClip(new Rectangle(0, 0, Width - RightSpace, CapHeight));
            var lastX = firstX;
            var penInactive = new Pen(XColor.InactiveFore, penWidth);
            var brushInactive = new SolidBrush(XColor.InactiveFore);
            var brushInactiveBack = new SolidBrush(XColor.Darking);
            var penActive = new Pen(XColor.DefaultFore, penWidth);
            var brushActive = new SolidBrush(XColor.DefaultFore);
            var half = (int)(penWidth * 0.25) + 1;
            Point[] points;
            for (int i = 0; i < Count; ++i)
            {
                var page = pages[i];
                var width = ButtonWidth(page.Title);
                width = Math.Max(CapMinWidth, width);
                points = new Point[] {
                    new Point(lastX, half),
                    new Point(lastX + width, half),
                    new Point(lastX + width, CapHeight)
                };
                if (ActiveTab == i)
                {
                    g.DrawLines(penActive, points);
                    g.DrawString(page.Title, CapFont, brushActive, points[0].X + CapPadding, points[0].Y + CapPadding);
                }
                else
                {
                    g.FillRectangle(brushInactiveBack, points[0].X, 0, width, CapHeight);
                    g.DrawLines(penInactive, points);
                    g.DrawString(page.Title, CapFont, brushInactive, points[0].X + CapPadding, points[0].Y + CapPadding);
                }
                lastX += width;
            }
            points = new Point[] {
                    new Point(lastX, half),
                    new Point(lastX + NewTabWidth, half),
                    new Point(lastX + NewTabWidth, CapHeight)
                };
            g.DrawLines(penActive, points);
            g.DrawString(newTab, CapFont, brushActive, points[0].X + CapPadding, points[0].Y + CapPadding);
            g.SetClip(new Rectangle(Width - RightSpace - half, 0, RightSpace + half, CapHeight));
            var bounds = g.ClipBounds;
            points = new Point[] {
                    new Point(Width - RightSpace, CapHeight),
                    new Point(Width - RightSpace, half),
                    new Point(Width - ToRightWidth, half)
                };
            g.DrawLines(penActive, points);
            g.DrawString(toLeft, CapFont, brushActive, points[1].X + CapPadding, points[1].Y + CapPadding);
            points = new Point[] {
                    new Point(Width - ToRightWidth, CapHeight),
                    new Point(Width - ToRightWidth, half),
                    new Point(Width, half)
                };
            g.DrawLines(penActive, points);
            g.DrawString(toRight, CapFont, brushActive, points[1].X + CapPadding, points[1].Y + CapPadding);
        }

        protected void UpdateCaps()
        {
            var lastX = firstX;
            pageCaps = new List<Rectangle>();
            foreach (var page in pages)
            {
                lastX += (int)(penWidth * 0.25);
                var width = ButtonWidth(page.Title);
                width = Math.Max(CapMinWidth, width);
                pageCaps.Add(new Rectangle(lastX, 0, width, CapHeight));
                lastX += width;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            UpdateCaps();
            if (e.Y > CapHeight)
                return;
            if (e.X < Width - RightSpace)
            {
                for (int i = 0; i < pages.Count; ++i)
                    if (pageCaps[i].Contains(e.Location))
                    {
                        ActiveTab = i;
                        Refresh();
                        return;
                    }
            }
            else
            {
                if (e.X < Width - ToRightWidth)
                    PrevPage();
                else
                    NextPage();
            }
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
        protected XTabControl control;
        protected int capHeight = 32;
        protected Pen pen = new Pen(XColor.DefaultFore, 2);

        protected Point? TouchBegin { get; set; }

        public XConsole()
        {
            FormBorderStyle = FormBorderStyle.None;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = XColor.DefaultBack;
            Opacity = 0.85;
            ClientSize = new Size(800, 600);

            var w = (int)pen.Width;
            control = new XTabControl();
            for (int n = 1; n <= 10; ++n)
                control.Add($"Tab {n}");
            control.Location = new Point(w, capHeight + w);
            control.Size = new Size(ClientSize.Width - 2 * w, ClientSize.Height - capHeight - 2 * w);
            Controls.Add(control);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var points = XRectangle.Generate(0, 0, ClientSize.Width, ClientSize.Height, pen.Width);
            var rect = new Rectangle(points[1], new Size(points[3].X - points[1].X, points[3].Y - points[1].Y));
            g.DrawRectangle(pen, rect);
        }

        protected override void OnMouseDown(MouseEventArgs e) => TouchBegin = e.Location;

        protected override void OnMouseUp(MouseEventArgs e) => TouchBegin = null;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (TouchBegin.HasValue)
                Location = new Point(Location.X + e.X - TouchBegin.Value.X, Location.Y + e.Y - TouchBegin.Value.Y);
        }

        public void Run()
        {
            Application.Run(new XConsole());
        }
    }
}
