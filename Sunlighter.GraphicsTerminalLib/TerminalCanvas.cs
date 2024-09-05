namespace Sunlighter.GraphicsTerminalLib
{
    public partial class TerminalCanvas : Control
    {
        private Bitmap? bitmap;

        public TerminalCanvas()
        {
            InitializeComponent();
            bitmap = null;
        }

        public void SetBitmap(Bitmap b)
        {
            if (bitmap is not null)
            {
                bitmap.Dispose();
            }
            bitmap = b;
            Invalidate();
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            SetStyle(ControlStyles.Selectable | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.StandardDoubleClick, false);
            UpdateStyles();
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            if (bitmap is null)
            {
                base.OnPaintBackground(pevent);
            }
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            if (bitmap is not null)
            {
                pe.Graphics.DrawImage(bitmap, new Rectangle(Point.Empty, ClientSize));
            }
            else
            {
                base.OnPaint(pe);
            }
        }

        public event CanvasMouseEventHandler? CanvasMouseClick;

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (bitmap is not null)
            {
                float xScale = bitmap.Size.Width / (float)ClientSize.Width;
                float yScale = bitmap.Size.Height / (float)ClientSize.Height;

                CanvasMouseClick?.Invoke(this, new CanvasMouseEventArgs(new PointF(e.X * xScale, e.Y * yScale)));
            }

            base.OnMouseClick(e);
        }

#if false
        protected override void OnClientSizeChanged(EventArgs e)
        {
            Invalidate();
            base.OnClientSizeChanged(e);
        }
#endif

        public event CanvasKeyEventHandler? CanvasKeyDown;

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (bitmap is not null)
            {
                CanvasKeyDown?.Invoke(this, new CanvasKeyEventArgs(e.KeyData));
            }
            base.OnKeyDown(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Up || keyData == Keys.Down || keyData == Keys.Left || keyData == Keys.Right)
            {
                OnKeyDown(new KeyEventArgs(keyData));
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    public delegate void CanvasMouseEventHandler(object sender, CanvasMouseEventArgs e);

    public sealed class CanvasMouseEventArgs : EventArgs
    {
        private readonly PointF location;

        public CanvasMouseEventArgs(PointF location)
        {
            this.location = location;
        }

        public PointF Location => location;
    }

    public delegate void CanvasKeyEventHandler(object sender, CanvasKeyEventArgs e);

    public sealed class CanvasKeyEventArgs : EventArgs
    {
        private readonly Keys keyData;

        public CanvasKeyEventArgs(Keys keyData)
        {
            this.keyData = keyData;
        }

        public Keys KeyData => keyData;
    }
}
