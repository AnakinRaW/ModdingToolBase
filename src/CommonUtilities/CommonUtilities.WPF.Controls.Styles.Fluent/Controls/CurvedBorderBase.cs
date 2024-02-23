using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace AnakinRaW.CommonUtilities.Wpf.Controls;

internal abstract class CurvedBorderBase : Border
{
    private static readonly Vector NoOffset = new(0.0, 0.0);
    protected static readonly bool DoNotRoundBorders;

    public Geometry? BackgroundGeometry { get; private set; }

    public Geometry? BorderGeometry { get; private set; }

    static CurvedBorderBase()
    {
        AppContext.TryGetSwitch("Switch.MS.Internal.DoNotApplyLayoutRoundingToMarginsAndBorderThickness", out DoNotRoundBorders);
    }

    protected abstract Size GetBackgroundSize(Size originalSize, Thickness border);

    protected abstract Vector GetBackgroundOffset(Thickness border);

    protected abstract CurveInfo GetCurveInfo(Size size, Thickness border, Vector offset, bool isBorder);

    protected abstract void GenerateGeometry(StreamGeometryContext context, CurveInfo info);

    protected override Size ArrangeOverride(Size finalSize)
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        Thickness border = default;
        ref var local = ref border;
        var borderThickness = BorderThickness;
        var left = RoundValue(borderThickness.Left, dpi.DpiScaleX);
        borderThickness = BorderThickness;
        var top = RoundValue(borderThickness.Top, dpi.DpiScaleY);
        borderThickness = BorderThickness;
        var right = RoundValue(borderThickness.Right, dpi.DpiScaleX);
        borderThickness = BorderThickness;
        var bottom = RoundValue(borderThickness.Bottom, dpi.DpiScaleY);
        local = new Thickness(left, top, right, bottom);
        var backgroundSize = GetBackgroundSize(finalSize, border);
        var backgroundOffset = GetBackgroundOffset(border);
        BorderGeometry = CreateGeometry(GetCurveInfo(finalSize, border, NoOffset, true));
        BackgroundGeometry = CreateGeometry(GetCurveInfo(backgroundSize, border, backgroundOffset, false));
        return base.ArrangeOverride(finalSize);

        static double RoundValue(double value, double dpiScale)
        {
            return DoNotRoundBorders ? value : Math.Round(value * dpiScale) / dpiScale;
        }

        Geometry CreateGeometry(CurveInfo info)
        {
            var geometry = new StreamGeometry();
            using var context = geometry.Open();
            GenerateGeometry(context, info);
            geometry.Freeze();
            return geometry;
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (BorderGeometry == null || BackgroundGeometry == null)
        {
            base.OnRender(dc);
        }
        else
        {
            if (BorderBrush != null)
                dc.DrawGeometry(BorderBrush, null, BorderGeometry);
            if (Background == null)
                return;
            dc.DrawGeometry(Background, null, BackgroundGeometry);
        }
    }

    protected void DrawArcTo(StreamGeometryContext context, Point endPoint, double radiusX, double radiusY, SweepDirection direction)
    {
        context.ArcTo(endPoint, new Size(radiusX, radiusY), 0.0, false, direction, true, false);
    }

    protected void DrawLineTo(StreamGeometryContext context, Point endPoint)
    {
        context.LineTo(endPoint, true, false);
    }

    protected struct CurveInfo
    {
        public CurveInfo(Size size, Thickness border, Vector offset)
        {
            Size = size;
            Border = border;
            Offset = offset;
            LeftTop = 0.0;
            TopLeft = 0.0;
            TopRight = 0.0;
            RightTop = 0.0;
            RightBottom = 0.0;
            BottomRight = 0.0;
            BottomLeft = 0.0;
            LeftBottom = 0.0;
        }

        public Size Size { get; }

        public Thickness Border { get; }

        public Vector Offset { get; }

        public double LeftTop { get; set; }

        public double TopLeft { get; set; }

        public double TopRight { get; set; }

        public double RightTop { get; set; }

        public double RightBottom { get; set; }

        public double BottomRight { get; set; }

        public double BottomLeft { get; set; }

        public double LeftBottom { get; set; }
    }
}