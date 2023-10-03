using System;
using System.Windows;
using System.Windows.Media;

namespace AnakinRaW.CommonUtilities.Wpf.Controls;

internal class SimpleCurvedBorder : CurvedBorderBase
{
    protected override Size GetBackgroundSize(Size originalSize, Thickness border)
    {
        return new Size(originalSize.Width - border.Left - border.Right,
            originalSize.Height - border.Top - border.Bottom);
    }

    protected override Vector GetBackgroundOffset(Thickness border)
    {
        return new Vector(border.Left, border.Top);
    }

    protected override CurveInfo GetCurveInfo(Size size, Thickness border, Vector offset, bool isBorder)
    {
        var num1 = 0.5 * border.Left;
        var num2 = 0.5 * border.Top;
        var num3 = 0.5 * border.Right;
        var num4 = 0.5 * border.Bottom;
        CurveInfo curveInfo;
        if (isBorder)
        {
            curveInfo = new CurveInfo(size, border, offset);
            ref var local1 = ref curveInfo;
            var cornerRadius = CornerRadius;
            var num5 = cornerRadius.TopLeft + num1;
            local1.LeftTop = num5;
            ref var local2 = ref curveInfo;
            cornerRadius = CornerRadius;
            var num6 = cornerRadius.TopLeft + num2;
            local2.TopLeft = num6;
            ref var local3 = ref curveInfo;
            cornerRadius = CornerRadius;
            var num7 = cornerRadius.TopRight + num2;
            local3.TopRight = num7;
            ref var local4 = ref curveInfo;
            cornerRadius = CornerRadius;
            var num8 = cornerRadius.TopRight + num3;
            local4.RightTop = num8;
            ref var local5 = ref curveInfo;
            cornerRadius = CornerRadius;
            var num9 = cornerRadius.BottomRight + num3;
            local5.RightBottom = num9;
            ref var local6 = ref curveInfo;
            cornerRadius = CornerRadius;
            var num10 = cornerRadius.BottomRight + num4;
            local6.BottomRight = num10;
            ref var local7 = ref curveInfo;
            cornerRadius = CornerRadius;
            var num11 = cornerRadius.BottomLeft + num4;
            local7.BottomLeft = num11;
            ref var local8 = ref curveInfo;
            cornerRadius = CornerRadius;
            var num12 = cornerRadius.BottomLeft + num1;
            local8.LeftBottom = num12;
        }
        else
        {
            curveInfo = new CurveInfo(size, new Thickness(border.Left, 0.0, border.Right, border.Bottom), offset)
            {
                LeftTop = Math.Max(0.0, CornerRadius.TopLeft - num1),
                TopLeft = Math.Max(0.0, CornerRadius.TopLeft - num2),
                TopRight = Math.Max(0.0, CornerRadius.TopRight - num2),
                RightTop = Math.Max(0.0, CornerRadius.TopRight - num3),
                RightBottom = Math.Max(0.0, CornerRadius.BottomRight - num3),
                BottomRight = Math.Max(0.0, CornerRadius.BottomRight - num4),
                BottomLeft = Math.Max(0.0, CornerRadius.BottomLeft - num4),
                LeftBottom = Math.Max(0.0, CornerRadius.BottomLeft - num1)
            };
        }
        return curveInfo;
    }

    protected override void GenerateGeometry(StreamGeometryContext context, CurveInfo info)
    {
        var size = info.Size;
        var offset = info.Offset;
        var point = new Point(info.LeftTop, 0.0) + offset;
        var endPoint1 = new Point(size.Width - info.RightTop, 0.0) + offset;
        var endPoint2 = new Point(size.Width, info.TopRight) + offset;
        var endPoint3 = new Point(size.Width, size.Height - info.BottomRight) + offset;
        var endPoint4 = new Point(size.Width - info.RightBottom, size.Height) + offset;
        var endPoint5 = new Point(info.LeftBottom, size.Height) + offset;
        var endPoint6 = new Point(0.0, size.Height - info.BottomLeft) + offset;
        var endPoint7 = new Point(0.0, info.TopLeft) + offset;
        context.BeginFigure(point, true, true);
        DrawLineTo(context, endPoint1);
        DrawArcTo(context, endPoint2, info.RightTop, info.TopRight, SweepDirection.Clockwise);
        DrawLineTo(context, endPoint3);
        DrawArcTo(context, endPoint4, info.RightBottom, info.BottomRight, SweepDirection.Clockwise);
        DrawLineTo(context, endPoint5);
        DrawArcTo(context, endPoint6, info.LeftBottom, info.BottomLeft, SweepDirection.Clockwise);
        DrawLineTo(context, endPoint7);
        DrawArcTo(context, point, info.LeftTop, info.TopLeft, SweepDirection.Clockwise);
    }
}