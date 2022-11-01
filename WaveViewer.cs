using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MiniSampler
{
    class WaveViewer:UserControl
    {

        private SampleData data;
        private int viewstart;
        private int viewstop;
        private int _loopstart;
        private int _loopstop;

        private bool _loop = false;
        public bool loop
        {
            set { _loop = value;InvalidateVisual(); }
            get { return _loop; }
        }

        public int loopstart
        {
            set { _loopstart = value; if (_loop) InvalidateVisual(); }
            get { return _loopstart; }
        }
        public int loopstop
        {
            set { _loopstop = value; if (_loop) InvalidateVisual(); }
            get { return _loopstop; }
        }

        private SolidColorBrush backgroundBrush = new SolidColorBrush(Color.FromRgb(0,30,0));
        public Color backgroundColor 
        { 
            get { return backgroundBrush.Color; }
            set { backgroundBrush.Color = value; InvalidateVisual(); }
        }

        private Pen framePen = new Pen(Brushes.Black, 1.0);
        public Color frameColor
        {
            get { return ((SolidColorBrush)framePen.Brush).Color; }
            set { ((SolidColorBrush)framePen.Brush).Color = value; InvalidateVisual(); }
        }
        private Pen leftPen = new Pen(Brushes.Red, 1.0);
        private Pen rightPen = new Pen(Brushes.Green, 1.0);
        public Color leftColor
        {
            get { return ((SolidColorBrush)leftPen.Brush).Color; }
            set { ((SolidColorBrush)leftPen.Brush).Color = value; InvalidateVisual(); }
        }
        public Color rightColor
        {
            get { return ((SolidColorBrush)rightPen.Brush).Color; }
            set { ((SolidColorBrush)rightPen.Brush).Color = value; InvalidateVisual(); }
        }
        private Pen gridPen = new Pen(Brushes.DarkGreen, 1.0);
        public Color gridColor
        {
            get { return ((SolidColorBrush)gridPen.Brush).Color; }
            set { ((SolidColorBrush)gridPen.Brush).Color = value; InvalidateVisual(); }
        }
        private Pen loopPen = new Pen(Brushes.White, 2.0);
        public Color loopColor
        {
            get { return ((SolidColorBrush)loopPen.Brush).Color; }
            set { ((SolidColorBrush)loopPen.Brush).Color = value; InvalidateVisual(); }
        }

        public WaveViewer(SampleData _data):base()
        {
            DefaultStyleKey = typeof(WaveViewer);
            data = _data;
            if (data != null)
            {
                viewstart = 0;
                viewstop = data.samples - 1;
                _loopstart = 0;
                _loopstop = data.samples - 1;
            } else
            {
                viewstart = viewstop = _loopstart = _loopstop = 0;
            }
        }

        public delegate void loopPosChangedEvent();
        public event loopPosChangedEvent loopPosChanged;

        public WaveViewer():this(null) { }

        public void updateWaveData(SampleData _data)
        {
            data = _data;
            if (data != null)
            {
                viewstart = 0;
                viewstop = data.samples - 1;
                _loopstart = 0;
                _loopstop = data.samples - 1;
            }
            else
            {
                viewstart = viewstop = _loopstart = _loopstop = 0;
            }
            InvalidateVisual();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            // base.OnMouseWheel(e);
            if (data == null)
                return;
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                // Shift
                int viewlen = viewstop - viewstart+1;
                int shiftf = e.Delta * viewlen / 120 /10;
                if (shiftf < 0)
                {
                    if (viewstart < -shiftf)
                    {
                        viewstart = 0;
                        viewstop = viewstart + viewlen - 1;
                    } else
                    {
                        viewstart += shiftf;
                        viewstop += shiftf;
                    }
                } else
                {
                    if (viewstop >= data.samples-shiftf)
                    {
                        viewstop = data.samples - 1;
                        viewstart = viewstop - viewlen + 1;
                    } else
                    {
                        viewstart += shiftf;
                        viewstop += shiftf;
                    }
                }
                if (viewstart < 0) viewstart = 0;
                if (viewstop >= data.samples) viewstop = data.samples - 1;
            }
            else
            {
                // Scale
                double scalef = Math.Exp(((double)-e.Delta / 120) / 10);
                double pos = (double)viewstart + e.GetPosition(this).X / ActualWidth * (viewstop - viewstart);
                double newstart = pos - (pos - viewstart) * scalef;
                double newstop = pos + (viewstop - pos) * scalef;
                if (newstart < 0) newstart = 0;
                if (newstop >= data.samples) newstop = data.samples - 1;
                viewstart = (int)newstart;
                viewstop = (int)newstop;
            }
            InvalidateVisual();
        }

        private enum DragMode { off, lstart, lstop};
        private DragMode dragMode = DragMode.off;

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            // base.OnMouseLeftButtonDown(e);
            if (data == null) return;
            if (!loop) return;
            double w = ActualWidth;
            double ls = w * (_loopstart - viewstart) / (viewstop - viewstart);
            double lp = w * (_loopstop - viewstart) / (viewstop - viewstart);
            Point pos = e.GetPosition(this);
            if ((pos.X >= ls - 5) && (pos.X <= ls + 5))
            {
                // Match start
                dragMode = DragMode.lstart;
                _loopstart = (int)Math.Floor(viewstart + pos.X * (viewstop - viewstart + 1) / w + 0.5);
                if (_loopstart < 0) _loopstart = 0;
                if (_loopstart >= data.samples) _loopstart = data.samples - 1;
                InvalidateVisual();
                CaptureMouse();
            }
            else if ((pos.X >= lp - 5) && (pos.X <= lp + 5))
            {
                // Match stop
                dragMode = DragMode.lstop;
                _loopstop = (int)Math.Floor(viewstart + pos.X * (viewstop - viewstart + 1) / w + 0.5);
                if (_loopstop <= _loopstart) _loopstop = _loopstart + 1;
                if (_loopstop >= data.samples) _loopstop = data.samples - 1;
                InvalidateVisual();
                CaptureMouse();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            // base.OnMouseMove(e);
            if (data == null) return;
            if (!loop) return;
            if (dragMode == DragMode.off)
                return;
            double w = ActualWidth;
            Point pos = e.GetPosition(this);
            switch (dragMode)
            {
                case DragMode.lstart:
                    _loopstart = (int)Math.Floor(viewstart + pos.X * (viewstop - viewstart + 1) / w + 0.5);
                    if (_loopstart < 0) _loopstart = 0;
                    if (_loopstart >= data.samples) _loopstart = data.samples - 1;
                    break;
                case DragMode.lstop:
                    _loopstop = (int)Math.Floor(viewstart + pos.X * (viewstop - viewstart + 1) / w + 0.5);
                    if (_loopstop <= _loopstart) _loopstop = _loopstart + 1;
                    if (_loopstop >= data.samples) _loopstop = data.samples - 1;
                    break;
            }
            InvalidateVisual();
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            // base.OnMouseLeftButtonUp(e);
            if (data == null) return;
            if (!loop) return;
            if (dragMode == DragMode.off)
                return;
            double w = ActualWidth;
            Point pos = e.GetPosition(this);
            switch (dragMode)
            {
                case DragMode.lstart:
                    _loopstart = (int)Math.Floor(viewstart + pos.X * (viewstop - viewstart + 1) / w + 0.5);
                    if (_loopstart < 0) _loopstart = 0;
                    if (_loopstart >= data.samples) _loopstart = data.samples - 1;
                    break;
                case DragMode.lstop:
                    _loopstop = (int)Math.Floor(viewstart + pos.X * (viewstop - viewstart + 1) / w + 0.5);
                    if (_loopstop <= _loopstart) _loopstop = _loopstart + 1;
                    if (_loopstop >= data.samples) _loopstop = data.samples - 1;
                    break;
            }
            dragMode = DragMode.off;
            InvalidateVisual();
            ReleaseMouseCapture();
        }

        private void findMinMax(float[] data, int s, int e, out float lmin, out float lmax, out float rmin, out float rmax)
        {
            lmin = lmax = data[s * 2 + 0];
            rmin = rmax = data[s*2+1];
            for (int i=s+1;i<=e;i++)
            {
                float l = data[i * 2 + 0];
                float r = data[i * 2 + 1];
                if (l < lmin) lmin = l;
                if (l > lmax) lmax = l;
                if (r < rmin) rmin = r;
                if (r > rmax) rmax = r;
            }
        }

        private float[] downsample(float[] data, int start, int len, int factor)
        {
            int pts = len / factor;
            float[] r = new float[pts*4];
            for (int i=0;i<pts;i++)
            {
                int s = i * factor + start;
                int e = (i+1) * factor + start - 1;
                float lmin, lmax, rmin, rmax;
                findMinMax(data, s, e, out lmin, out lmax, out rmin, out rmax);
                r[i * 4 + 0] = lmin;
                r[i * 4 + 1] = lmax;
                r[i * 4 + 2] = rmin;
                r[i * 4 + 3] = rmax;
            }
            return r;
        }

        private void drawArrow(DrawingContext dc, Pen p, Point start, Point stop, double headlen)
        {
            Point PA = new Point();
            Point PB = new Point();
            double vx = stop.X - start.X;
            double vy = stop.Y - start.Y;
            double l = Math.Sqrt(vx * vx + vy * vy);
            if (l <= 0) return;
            vx /= l;
            vy /= l;
            double ax = -vy;
            double ay = vx;
            headlen = headlen / Math.Sqrt(2);
            PA.X = stop.X - vx * headlen + ax * headlen;
            PA.Y = stop.Y - vy * headlen + ay * headlen;
            PB.X = stop.X - vx * headlen - ax * headlen;
            PB.Y = stop.Y - vy * headlen - ay * headlen;
            dc.DrawLine(p, start, stop);
            dc.DrawLine(p, stop, PA);
            dc.DrawLine(p, stop, PB);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            // base.OnRender(drawingContext);
            double w = ActualWidth;
            double h = ActualHeight;
            Rect frame = new Rect(0, 0, w, h);
            drawingContext.DrawRectangle(backgroundBrush, framePen, frame);
            drawingContext.DrawLine(gridPen, new Point(0, h / 2), new Point(w - 1, h / 2));
            if (data == null) return;
            if (loop)
            {
                double ls = w * (_loopstart - viewstart) / (viewstop - viewstart);
                double lp = w * (_loopstop - viewstart) / (viewstop - viewstart);
                if ((ls >= -30) && (ls < w + 30))
                {
                    Point p1 = new Point(ls, 0);
                    Point p2 = new Point(ls, h - 1);
                    drawingContext.DrawLine(loopPen, p1, p2);
                    p1.Y = p2.Y = h / 10;
                    p2.X = p1.X + 30;
                    drawArrow(drawingContext, loopPen, p1, p2, 10);
                }
                if ((lp >= -30) && (lp < w + 30))
                {
                    Point p1 = new Point(lp, 0);
                    Point p2 = new Point(lp, h - 1);
                    drawingContext.DrawLine(loopPen, p1, p2);
                    p1.Y = p2.Y = h - h / 10;
                    p2.X = p1.X - 30;
                    drawArrow(drawingContext, loopPen, p1, p2, 10);
                }
            }
            int sls = viewstop - viewstart + 1;
            if (sls > w)
            {
                // Must downsample
                int df = (int)Math.Ceiling(sls / w);
                float[] r = downsample(data.data, viewstart, sls, df);
                int pts = r.Length / 4;
                Point p1l = new Point();
                Point p2l = new Point();
                Point p1r = new Point();
                Point p2r = new Point();
                p1l.X = p2l.X = p1r.X = p2r.X = 0;
                p1l.Y = r[4 * 0 + 0] * h / 2 + h / 2;
                p2l.Y = r[4 * 0 + 1] * h / 2 + h / 2;
                p1r.Y = r[4 * 0 + 2] * h / 2 + h / 2;
                p2r.Y = r[4 * 0 + 3] * h / 2 + h / 2;
                Point p3l = new Point();
                Point p4l = new Point();
                Point p3r = new Point();
                Point p4r = new Point();
                for (int i=1;i<pts;i++)
                {
                    p3l.X = p4l.X = p3r.X = p4r.X = (float)i / (pts - 1) * (w - 1);
                    p3l.Y = r[4 * i + 0] * h / 2 + h / 2;
                    p4l.Y = r[4 * i + 1] * h / 2 + h / 2;
                    p3r.Y = r[4 * i + 2] * h / 2 + h / 2;
                    p4r.Y = r[4 * i + 3] * h / 2 + h / 2;
                    drawingContext.DrawLine(leftPen, p1l, p3l);
                    drawingContext.DrawLine(leftPen, p2l, p4l);
                    drawingContext.DrawLine(rightPen, p1r, p3r);
                    drawingContext.DrawLine(rightPen, p2r, p4r);
                    p1l = p3l;
                    p2l = p4l;
                    p1r = p3r;
                    p2r = p4r;
                }
            } else
            {
                // Direct Draw
                Point p1l = new Point();
                Point p1r = new Point();
                p1l.X = 0;
                p1l.Y = data.data[2 * viewstart + 0] * h / 2 + h / 2;
                p1r.X = 0;
                p1r.Y = data.data[2 * viewstart + 1] * h / 2 + h / 2;
                Point p2l = new Point();
                Point p2r = new Point();
                for (int s = viewstart+1;s <= viewstop;s++)
                {
                    p2l.X = (s - viewstart) * (w - 1) / (viewstop - viewstart);
                    p2l.Y = data.data[2 * s + 0] * h / 2 + h / 2;
                    p2r.X = p2l.X;
                    p2r.Y = data.data[2 * s + 1] * h / 2 + h / 2;
                    drawingContext.DrawLine(leftPen, p1l, p2l);
                    drawingContext.DrawLine(rightPen, p1r, p2r);
                    p1l = p2l;
                    p1r = p2r;
                }
            }

        }


    }
}
