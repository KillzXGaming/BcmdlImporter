﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SPICA.WinForms.GUI
{
    class SUIAnimSeekBar : Control
    {
        private Color _CursorColor = Color.Orange;

        private float _Maximum;
        private float _Value;

        [Category("Appearance"), Description("Color of the animation cursor.")]
        public Color CursorColor
        {
            get
            {
                return _CursorColor;
            }
            set
            {
                _CursorColor = value;

                Invalidate();
            }
        }

        [Category("Behavior"), Description("Total number of frames that the animation have.")]
        public float Maximum
        {
            get
            {
                return _Maximum;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException(MaxTooLowEx);
                }

                _Maximum = value;

                _Value = Math.Min(_Value, _Maximum);

                Invalidate();
            }
        }

        [Category("Behavior"), Description("Current animation frame.")]
        public float Value
        {
            get
            {
                return _Value;
            }
            set
            {
                if (value < 0 || value > Maximum)
                {
                    throw new ArgumentOutOfRangeException(string.Format(ValueOutOfRangeEx, _Maximum));
                }

                _Value = value;

                Invalidate();
            }
        }

        public event EventHandler Seek;

        private const int BarMarginX = 4;
        private const int RulerMarginX = 6;
        private const int RulerMinDist = 64;

        private const string MaxTooLowEx = "Invalid maximum value! Expected a value >= 0!";
        private const string ValueOutOfRangeEx = "Invalid value! Expected >= 0 and <= {0}!";

        public SUIAnimSeekBar()
        {
            SetStyle(
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            e.Graphics.Clear(Parent.BackColor);

            Brush TxtBrush = new SolidBrush(ForeColor);
            Rectangle Rect = new Rectangle(BarMarginX, 3, Width - BarMarginX * 2, Height - 6);

            e.Graphics.FillRectangle(new SolidBrush(BackColor), Rect);

            int HalfH = Rect.Height >> 1;

            int RulerW = Rect.Width - RulerMarginX * 2;
            int RulerSX = Rect.X + RulerMarginX;

            if (_Maximum > 0)
            {
                float PartStep  = RulerW / _Maximum;
                float FrameStep = RulerMinDist / PartStep;

                if (FrameStep < 1) FrameStep = 1;
                if (FrameStep > 1) PartStep = RulerMinDist;

                float RulerX   = PartStep;
                float HalfStep = PartStep * 0.5f;

                for (float
                    Frame = FrameStep;
                    RulerX - HalfStep < RulerW;
                    Frame += FrameStep, RulerX += PartStep)
                {
                    int RX  = (int)(RulerX + RulerSX);
                    int HRX = (int)(RulerX + RulerSX - HalfStep);

                    //Short line
                    e.Graphics.DrawLine(
                        new Pen(ForeColor),
                        new Point(HRX, Rect.Y + 1),
                        new Point(HRX, Rect.Y + 1 + HalfH >> 1));

                    //Longer line
                    if (RulerX <= RulerW)
                    {
                        e.Graphics.DrawLine(
                            new Pen(ForeColor),
                            new Point(RX, Rect.Y + 1),
                            new Point(RX, Rect.Y + 1 + HalfH));

                        //Frame number
                        string Text = Frame.ToString("0.0");

                        int TextWidth = (int)e.Graphics.MeasureString(Text, Font).Width;

                        Point TextPt = new Point(RX - TextWidth, Rect.Y);

                        e.Graphics.DrawString(Text, Font, TxtBrush, TextPt);
                    }
                }
            }

            //Draw inset box shade effect
            int L = Rect.X + 1; //Left
            int T = Rect.Y + 1; //Top

            int R = L + Rect.Width  - 2; //Right
            int B = T + Rect.Height - 2; //Bottom

            Pen BlackShade = new Pen(Color.FromArgb(0x3f, Color.Black), 2);
            Pen WhiteShade = new Pen(Color.FromArgb(0x3f, Color.White), 2);

            e.Graphics.DrawLine(BlackShade, new Point(L, T), new Point(R, T));
            e.Graphics.DrawLine(BlackShade, new Point(L, T), new Point(L, B));
            e.Graphics.DrawLine(WhiteShade, new Point(R, B), new Point(R, T));
            e.Graphics.DrawLine(WhiteShade, new Point(R, B), new Point(L, B));

            //Frame number text
            int CurX = (int)(_Maximum > 0 ? ((_Value / _Maximum) * RulerW) : 0) + RulerSX;

            string FrmTxt = _Value.ToString("0.0");

            SizeF FrmTxtSz = e.Graphics.MeasureString(FrmTxt, Font);

            Rectangle FrmTxtRect = new Rectangle(
                Math.Max(CurX - (int)FrmTxtSz.Width, RulerSX), Rect.Y + 1,
                (int)FrmTxtSz.Width,
                (int)FrmTxtSz.Height);

            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(0xbf, Color.Black)), FrmTxtRect);
            e.Graphics.DrawString(FrmTxt, Font, TxtBrush, FrmTxtRect.Location);

            //Cursor line
            e.Graphics.DrawLine(
                new Pen(CursorColor),
                new Point(CurX, Rect.Y + 1),
                new Point(CurX, Rect.Y + 4 + HalfH));

            //Draw a triangle on the bottom side of the cursor
            Point[] Points = new Point[3];

            Points[0] = new Point(CurX - 4, Rect.Y - 2 + Rect.Height); //Left
            Points[1] = new Point(CurX + 4, Rect.Y - 2 + Rect.Height); //Right
            Points[2] = new Point(CurX,     Rect.Y + 1 + HalfH); //Middle

            e.Graphics.FillPolygon(new SolidBrush(CursorColor), Points);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) MoveTo(e.X);

            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) MoveTo(e.X);

            base.OnMouseMove(e);
        }

        private void MoveTo(int X)
        {
            int MX = BarMarginX + RulerMarginX;

            _Value = Math.Max(Math.Min(((float)(X - MX) / (Width - MX * 2)) * _Maximum, _Maximum), 0);

            Seek?.Invoke(this, EventArgs.Empty);

            Invalidate();
        }
    }
}
