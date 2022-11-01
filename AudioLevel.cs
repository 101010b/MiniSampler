using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace MiniSampler
{
    // class AudioLevel : Canvas
    class AudioLevel : UserControl
    {

        private SolidColorBrush backgroundBrush = new SolidColorBrush(Colors.Black);
        public Color backgroundColor
        {
            set { backgroundBrush.Color = value; InvalidateVisual(); }
            get { return backgroundBrush.Color; }
        }

        private SolidColorBrush lowLevelBrush = new SolidColorBrush(Colors.Green);
        private SolidColorBrush midLevelBrush = new SolidColorBrush(Colors.Yellow);
        private SolidColorBrush hiLevelBrush = new SolidColorBrush(Colors.Red);
        private SolidColorBrush levelBrushOff = new SolidColorBrush(Color.FromArgb(255, 10, 10, 10));

        private int _levels = 64;
        public int levels
        {
            get { return _levels; }
            set { _levels = value; InvalidateVisual(); }
        }

        private double _minLevel = -120;
        public double minLevel
        {
            set { _minLevel = value; InvalidateVisual(); }
            get { return _minLevel; }
        }
        private double _maxLevel = 0;
        public double maxLevel
        {
            set { _maxLevel = value; InvalidateVisual(); }
            get { return _maxLevel; }
        }

        private double _greenYellowLevel = -20;
        public double greenYellowLevel
        {
            set { _greenYellowLevel = value; InvalidateVisual(); }
            get { return _greenYellowLevel; }
        }

        private double _yellowRedLevel = -6;
        public double yellowRedLevel
        {
            set { _yellowRedLevel = value; InvalidateVisual(); }
            get { return _yellowRedLevel; }
        }

        private double level1 = -120;
        private double level2 = -120;
        private double max1 = -120;
        private double max2 = -120;
        public void setLevel(double l1, double l2) { level1 = l1; level2 = l2; }
        public void setMax(double m1, double m2) { max1 = m1; max2 = m2; }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            Rect myRect = new Rect(0, 0, ActualWidth - 1, ActualHeight - 1);
            drawingContext.DrawRectangle(backgroundBrush, null, myRect);
            double h = ActualHeight;
            double w = ActualWidth;
            double blockhgeight = h / _levels;
            double rh = blockhgeight * 0.9;
            int imax1 = (int)Math.Floor((max1 - minLevel) / (maxLevel - minLevel) * _levels + 0.5);
            int imax2 = (int)Math.Floor((max2 - minLevel) / (maxLevel - minLevel) * _levels + 0.5);
            for (int i=0;i<_levels;i++)
            {
                double l = _minLevel + (_maxLevel - _minLevel) * i / (_levels - 1);
                Rect r1 = new Rect(w * 0.1, h - (i+1) * blockhgeight - 0.05 * blockhgeight, w * 0.35, blockhgeight * 0.9);
                Rect r2 = new Rect(w * 0.55, h - (i+1) * blockhgeight - 0.05 * blockhgeight, w * 0.35, blockhgeight * 0.9);
                if ((level1 >= l) || (imax1 == i))
                {
                    // Is on
                    if (l >= _yellowRedLevel)
                        drawingContext.DrawRectangle(hiLevelBrush, null, r1);
                    else if (l >= _greenYellowLevel)
                        drawingContext.DrawRectangle(midLevelBrush, null, r1);
                    else
                        drawingContext.DrawRectangle(lowLevelBrush, null, r1);
                }
                else
                {
                    // Off
                    drawingContext.DrawRectangle(levelBrushOff, null, r1);
                }
                if ((level2 >= l) || (imax2 == i))
                {
                    // Is on
                    if (l >= _yellowRedLevel)
                        drawingContext.DrawRectangle(hiLevelBrush, null, r2);
                    else if (l >= _greenYellowLevel)
                        drawingContext.DrawRectangle(midLevelBrush, null, r2);
                    else
                        drawingContext.DrawRectangle(lowLevelBrush, null, r2);
                }
                else
                {
                    // Off
                    drawingContext.DrawRectangle(levelBrushOff, null, r2);
                }
            }
        }

        public AudioLevel():base()
        {
            DefaultStyleKey = typeof(AudioLevel);
        }

    }
}
