using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace XConsole
{
    public class XChar
    {
        public char Value = '+';
        public Color Back = XColor.LightBack;
        public Color Fore = XColor.LightFore;
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
        public static Color LightBack = Color.FromArgb(0, 0, 0, 0);
        public static Color LightFore = Color.FromArgb(255, 132, 170, 217);
        public static Color DarkBack = Color.FromArgb(255, 63, 63, 63);
        public static Color DarkFore = Color.FromArgb(255, 80, 96, 112);
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

    public class XButton : Control
    {
        public string Text = "Button";
        public Color DefaultBack = XColor.DarkBack;
        public Color DefaultFore = XColor.DarkFore;
        public Color OverBack = XColor.LightBack;
        public Color OverFore = XColor.LightFore;
        public byte Mask = 0x0F;
        public float BorderWidth = 2.0f;
        public Font TextFont = new Font(FontFamily.GenericMonospace, 13, FontStyle.Bold);
        public int TextPadding = 6;
        public int MinWidth = 48;
        public Action ActivateAction;

        public int GetWidth() => Math.Max(TextRenderer.MeasureText(Text, TextFont).Width + (TextPadding << 1) + (int)(BorderWidth * 2), MinWidth);
        public int GetHeight() => TextRenderer.MeasureText(Text, TextFont).Height + (TextPadding << 1) + (int)(BorderWidth * 2);

        public XButton()
        {
            Width = GetWidth();
            Height = GetHeight();
        }

        public XButton(string text)
        {
            Text = text;
            Width = GetWidth();
            Height = GetHeight();
        }

        public XButton(string text, byte mask)
        {
            Text = text;
            Mask = mask;
            Width = GetWidth();
            Height = GetHeight();
        }

        protected bool over = false;

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var brushBack = new SolidBrush(over ? OverBack : DefaultBack);
            var penFore = new Pen(over ? OverFore : DefaultFore, BorderWidth);
            var brushFore = new SolidBrush(over ? OverFore : DefaultFore);
            var halfDown = (int)BorderWidth / 2;
            var halfUp = (int)BorderWidth - halfDown;
            g.FillRectangle(brushBack, 0, 0, Width, Height);
            if ((Mask & 0x08) > 0)
                g.DrawLine(penFore, halfDown, halfDown, halfDown, Height - halfUp);
            if ((Mask & 0x04) > 0)
                g.DrawLine(penFore, halfDown, halfDown, Width - halfUp, halfDown);
            if ((Mask & 0x02) > 0)
                g.DrawLine(penFore, Width - halfUp, halfDown, Width - halfUp, Height - halfUp);
            if ((Mask & 0x01) > 0)
                g.DrawLine(penFore, halfDown, Height - halfUp, Width - halfUp, Height - halfUp);
            g.DrawString(Text, TextFont, brushFore, (int)BorderWidth + TextPadding, (int)BorderWidth + TextPadding);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            over = true;
            Refresh();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            over = false;
            Refresh();
        }
        
        protected override void OnMouseClick(MouseEventArgs e) => ActivateAction?.Invoke();
    }

    public class XHorizontalList<T> : Control where T : Control
    {
        protected Panel panel = new Panel();
        protected List<T> elements = new List<T>();

        public void UpdateMetrics()
        {
            Console.WriteLine(Count);
            Controls.Clear();
            var lastLocation = Location;
            foreach (var control in elements)
            {
                var next = control;
                next.Location = lastLocation;
                lastLocation.X += next.Width;
                Controls.Add(next);
            }
        }

        public int Count => elements.Count;
        public T this[int index] => elements[index];

        public void Add(T control) => elements.Add(control);
        public void AddRange(IEnumerable<T> controls) => elements.AddRange(controls);
        public void Insert(int index, T control) => elements.Insert(index, control);
        public void InsertRange(int index, IEnumerable<T> controls) => elements.InsertRange(index, controls);
        public bool Remove(T control) => elements.Remove(control);
        public void RemoveAt(int index) => elements.RemoveAt(index);
        public void RemoveRange(int index, int count) => elements.RemoveRange(index, count);
        public void Clear() => elements.Clear();
    }

    public class XTabControl : Control
    {
        protected XHorizontalList<XButton> buttons = new XHorizontalList<XButton>();
        protected List<XTabPage> pages = new List<XTabPage>();
        protected Font font;
        protected FontFamily fontFamily = FontFamily.GenericMonospace;
        protected float fontSize = 13;
        protected FontStyle fontStyle = FontStyle.Regular;
        protected float penWidth = 2.0f;
        protected int firstX = 0;
        protected XButton ToLeft = new XButton("<");
        protected XButton ToRight = new XButton(">");
        protected XButton NewTab = new XButton("[+]");
        protected XButton RemoveTab = new XButton("\u00D7");
        protected int activeTab = 0;
        protected int firstPage = 0;
        protected static string GenTitle(int id, string title) => $"<{id + 1}> {title}";

        protected void UpdateFont() => font = new Font(fontFamily, fontSize, fontStyle);
        protected void FillDefault() => Add();

        public int ActiveTab
        {
            get => activeTab;
            set
            {
                activeTab = (Count + value) % Count;
                Refresh();
            }
        }

        public int FirstPage
        {
            get => firstPage;
            set
            {
                firstPage = (Count + value) % Count;
                firstX = 0;
                for (int i = 0; i < firstPage; ++i)
                    firstX -= buttons[i].Width;
                for (int i = 0; i < Count; ++i)
                    buttons[i].Left += firstX;
                Refresh();
            }
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

        public void NextPage() => ++FirstPage;
        public void PrevPage() => --FirstPage;
        public int Count => pages.Count;

        protected void Initialize()
        {
            Controls.Add(buttons);
            buttons.Add(NewTab);
            Controls.Add(ToLeft);
            Controls.Add(ToRight);
        }

        public XTabControl()
        {
            Initialize();
            UpdateFont();
            FillDefault();
        }

        public XTabControl(XTabPage page)
        {
            Initialize();
            UpdateFont();
            if (page == null)
                FillDefault();
            else
                Add(page);
        }

        public XTabControl(string newTabTitle)
        {
            Initialize();
            UpdateFont();
            Add(new XTabPage(newTabTitle));
        }

        public XTabControl(IEnumerable<XTabPage> list)
        {
            Initialize();
            UpdateFont();
            foreach (var title in list)
                Add(title);
            if (pages.Count == 0)
                FillDefault();
        }

        public XTabControl(IEnumerable<string> list)
        {
            Initialize();
            UpdateFont();
            foreach (var title in list)
                Add(new XTabPage(title));
            if (pages.Count == 0)
                FillDefault();
        }

        public void Add()
        {
            var next = new XTabPage();
            pages.Add(next);
            buttons.Insert(buttons.Count - 1, new XButton
            {
                Text = GenTitle(Count - 1, next.Title),
                Mask = 0x0E
            });
            Refresh();
        }

        public void Add(XTabPage newPage)
        {
            pages.Add(newPage);
            buttons.Insert(buttons.Count - 1, new XButton
            {
                Text = GenTitle(Count - 1, newPage.Title),
                Mask = 0x0E
            });
            Refresh();
        }

        public void Add(string newPageTitle)
        {
            pages.Add(new XTabPage(newPageTitle));
            buttons.Insert(buttons.Count - 1, new XButton
            {
                Text = GenTitle(Count - 1, newPageTitle),
                Mask = 0x0E
            });
            Refresh();
        }

        public XTabPage this[int index] => pages[index];

        public void Remove(XTabPage page)
        {
            var pageId = pages.IndexOf(page);
            buttons.RemoveAt(pageId);
            pages.RemoveAt(pageId);
            if (Count == 0)
                Add();
            Refresh();
        }

        public void RemoveAt(int index)
        {
            buttons.RemoveAt(index);
            pages.RemoveAt(index);
            if (Count == 0)
                Add();
            Refresh();
        }

        public void UpdateMetrics()
        {
            buttons.UpdateMetrics();
            buttons.Width = Width - ToLeft.Width - ToRight.Width;
            buttons.Height = NewTab.Height;
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
        protected Pen pen = new Pen(XColor.LightFore, 2);

        protected Point? TouchBegin { get; set; }

        public XConsole()
        {
            FormBorderStyle = FormBorderStyle.None;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = XColor.DarkBack;
            Opacity = 0.85;
            ClientSize = new Size(800, 600);
            KeyPreview = true;

            var w = (int)pen.Width;
            control = new XTabControl("---");
            for (int n = 1; n <= 10; ++n)
                control.Add($"Tab {n}");
            control.Location = new Point(w, capHeight + w);
            control.Size = new Size(ClientSize.Width - 2 * w, ClientSize.Height - capHeight - 2 * w);
            control.UpdateMetrics();
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

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Tab:
                    if (e.Modifiers.HasFlag(Keys.Control))
                        control.FirstPage = e.Modifiers.HasFlag(Keys.Shift) ? --control.ActiveTab : ++control.ActiveTab;
                    break;
                case Keys.N:
                    if (e.Modifiers.HasFlag(Keys.Control))
                        control.Add();
                    break;
            }
        }

        public void Run()
        {
            Application.Run(new XConsole());
        }
    }
}
