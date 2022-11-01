using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;
using System.Windows.Shapes;

namespace MiniSampler
{

    public class SamplePlayButton : Button
    {

        private bool _empty = true;
        public bool empty
        {
            get { return _empty; }
            set { _empty = value; InvalidateVisual(); }
        }

        private Key _hotkey = Key.None;
        public Key hotKey
        {
            get { return _hotkey; }
            set { _hotkey = value; InvalidateVisual(); }
        }

        private bool buttonState = false;


        private bool _buttonStayDown = false;
        public bool buttonStayDown
        {
            get { return _buttonStayDown; }
            set { _buttonStayDown = value; InvalidateVisual(); }
        }

        private bool _isToggleSwitch = false;
        public bool isToggleSwitch
        {
            set { _isToggleSwitch = value; InvalidateVisual(); }
            get { return _isToggleSwitch; }
        }


        private string _sampleName = "";
        public string sampleName
        {
            get { return _sampleName; }
            set { _sampleName = value; InvalidateVisual(); }
        }

        public delegate void downStateChangedEvent(bool down);
        public event downStateChangedEvent downStateChanged;

        public delegate void configRequestEvent();
        public event configRequestEvent configRequest;

        public void release()
        {
            if (buttonState)
            {
                buttonState = false;
                InvalidateVisual();
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            // base.OnMouseLeftButtonDown(e);
            if (_isToggleSwitch)
            {
                buttonState = !buttonState;
                InvalidateVisual();
                downStateChanged.Invoke(buttonState);
            }
            else
            {
                if (buttonState) return;
                buttonState = true;
                InvalidateVisual();
                downStateChanged.Invoke(buttonState);
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            // base.OnMouseLeftButtonUp(e);
            if (_isToggleSwitch) return;
            if (_buttonStayDown) return;
            if (!buttonState) return;
            buttonState = false;
            InvalidateVisual();
            downStateChanged.Invoke(false);
        }
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            // base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            // base.OnMouseUp(e);
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            // base.OnMouseRightButtonDown(e);
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            // base.OnMouseRightButtonUp(e);
            configRequest.Invoke();
        }


        private SolidColorBrush backgroundBrush = new SolidColorBrush(Colors.LightGray);
        public Color backgroundColor
        {
            get { return backgroundBrush.Color; }
            set { backgroundBrush.Color = value; InvalidateVisual(); }
        }
        private Pen lightFramePen = new Pen(Brushes.LightGray, 1);
        private Pen framePen = new Pen(Brushes.DarkGray, 1);
        private Pen frameDownPen = new Pen(Brushes.DarkRed, 1);
        public Color lightFrameColor
        {
            get { return ((SolidColorBrush)lightFramePen.Brush).Color; }
            set { ((SolidColorBrush)lightFramePen.Brush).Color = value; InvalidateVisual(); }
        }
        public Color frameColor
        {
            get { return ((SolidColorBrush)framePen.Brush).Color; }
            set { ((SolidColorBrush)framePen.Brush).Color = value; InvalidateVisual(); }
        }
        public Color frameDownColor
        {
            get { return ((SolidColorBrush)frameDownPen.Brush).Color; }
            set { ((SolidColorBrush)frameDownPen.Brush).Color = value; InvalidateVisual(); }
        }
        private SolidColorBrush centerTextBrush = new SolidColorBrush(Colors.Black);
        public Color centerTextColor
        {
            get { return centerTextBrush.Color; }
            set { centerTextBrush.Color = value; InvalidateVisual(); }
        }
        private SolidColorBrush hotKeyBrush = new SolidColorBrush(Colors.DarkBlue);
        public Color hotKeyTextColor
        {
            get { return hotKeyBrush.Color; }
            set { hotKeyBrush.Color = value; InvalidateVisual(); }
        }

        private Polygon createTriangle(double x, double y, double a)
        {
            Polygon p = new Polygon();
            double h = Math.Sqrt(3) / 2.0 * a;
            p.Points.Add(new Point(x - h / 3, y - a / 2));
            p.Points.Add(new Point(x - h / 3, y + a / 2));
            p.Points.Add(new Point(x + h * 2 / 3, y));
            return p;
        }

        private void drawPolygon(DrawingContext dc, Pen pe, Polygon p)
        {
            if ((p == null) || (p.Points.Count < 2)) 
                return;
            for (int i=0;i<p.Points.Count;i++)
            {
                int j = i + 1;
                if (j >= p.Points.Count) j = 0;
                dc.DrawLine(pe, p.Points[i], p.Points[j]);
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            Rect myRect = new Rect(0, 0, ActualWidth - 1, ActualHeight - 1);
            double minl = (ActualWidth < ActualHeight) ? ActualWidth : ActualHeight;
            double cx = ActualWidth / 2;
            double cy = ActualHeight / 2;
            if (empty)
            {
                drawingContext.DrawRoundedRectangle(backgroundBrush, lightFramePen, myRect, minl / 10, minl / 10);
                // drawingContext.DrawRectangle(Background, lightFramePen, myRect);
                return;
            }
            drawingContext.DrawRoundedRectangle(backgroundBrush, framePen, myRect,minl/10,minl/10);
            if (buttonState == false)
            {
                drawingContext.DrawRectangle(null, framePen, new Rect(cx - 1.5 * minl / 10, cy - minl / 3, minl / 10, 2 * minl / 3));
                drawingContext.DrawRectangle(null, framePen, new Rect(cx + 0.5 * minl / 10, cy - minl / 3, minl / 10, 2 * minl / 3));
            } else
            {
                Polygon p = createTriangle(cx, cy, 2 * minl / 3);
                drawPolygon(drawingContext, frameDownPen, p);
            }
            FormattedText fmt = new FormattedText(_sampleName, 
                System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface("Times"), 16*minl/75, centerTextBrush, 1.24);
            drawingContext.DrawText(fmt, new Point(ActualWidth / 2 - fmt.Width / 2, ActualHeight / 2 - fmt.Height / 2));
            if (hotKey != Key.None)
            {
                fmt = new FormattedText(hotKey.ToString(),
                    System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    new Typeface("Times"), 10*minl/75, hotKeyBrush, 1.24);
                drawingContext.DrawText(fmt, new Point(minl/30, minl/30));
            }
        }

        public void manualKeyDown()
        {
            // base.OnMouseLeftButtonDown(e);
            if (_isToggleSwitch)
            {
                buttonState = !buttonState;
                InvalidateVisual();
                downStateChanged.Invoke(buttonState);
            }
            else
            {
                if (buttonState) return;
                buttonState = true;
                InvalidateVisual();
                downStateChanged.Invoke(buttonState);
            }
        }

        public void manualKeyUp()
        {
            // base.OnMouseLeftButtonUp(e);
            if (_isToggleSwitch) return;
            if (_buttonStayDown) return;
            if (!buttonState) return;
            buttonState = false;
            InvalidateVisual();
            downStateChanged.Invoke(false);
        }

        public SamplePlayButton() : base()
        {
            DefaultStyleKey = typeof(SamplePlayButton);
        }
    }


}
