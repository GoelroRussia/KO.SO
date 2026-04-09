using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace SteelBar.Utils;

public static class RebarShapeGenerator
{
    public static ImageSource? CreateRebarImage(Rebar rebar)
    {
        // 1. LẤY ĐƯỜNG NÉT ĐỂ VẼ HÌNH
        var drawCurves = rebar.GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeOnlyPlanarCurves, 0);
        if (drawCurves == null || drawCurves.Count == 0) return null;

        // 2. LẤY CÁC ĐOẠN THẲNG CHÍNH ĐỂ ĐẶT CHỮ
        var mainSegments = rebar.GetCenterlineCurves(false, true, true, MultiplanarOption.IncludeOnlyPlanarCurves, 0);

        double barDiameterMm = 0;
        if (rebar.Document.GetElement(rebar.GetTypeId()) is RebarBarType barType)
        {
            barDiameterMm = barType.BarNominalDiameter * 304.8;
        }

        // 3. ĐỌC CÁC PARAMETER TỪ THANH THÉP
        string[] shapeParamNames = { "A", "B", "C", "D", "E", "F", "G", "H", "J", "K", "L1", "L2", "L3", "a", "b", "c", "d", "e", "f", "g" };
        List<Parameter> validParams = new List<Parameter>();
        foreach (string name in shapeParamNames)
        {
            Parameter p = rebar.LookupParameter(name);
            if (p != null && p.HasValue && p.AsDouble() > 0)
            {
                validParams.Add(p);
            }
        }

        // 4. THUẬT TOÁN AUTO-ORIENT (Ép nằm ngang và ngửa móc lên trên)
        XYZ normal = XYZ.BasisZ;
        try { normal = rebar.GetShapeDrivenAccessor().Normal; } catch { }

        Line? longestLine = null;
        double maxLen = -1;
        foreach (var curve in mainSegments)
        {
            if (curve is Line line && line.Length > maxLen)
            {
                maxLen = line.Length;
                longestLine = line;
            }
        }

        XYZ xAxis, yAxis;
        if (longestLine != null)
        {
            xAxis = longestLine.Direction.Normalize();
            yAxis = normal.CrossProduct(xAxis).Normalize();
        }
        else
        {
            if (Math.Abs(normal.Z) > 0.8) { xAxis = XYZ.BasisX; yAxis = XYZ.BasisY; }
            else if (Math.Abs(normal.X) > 0.8) { xAxis = XYZ.BasisY; yAxis = XYZ.BasisZ; }
            else { xAxis = XYZ.BasisX; yAxis = XYZ.BasisZ; }
            yAxis = normal.CrossProduct(xAxis).Normalize();
            xAxis = yAxis.CrossProduct(normal).Normalize();
        }

        XYZ p0 = drawCurves[0].GetEndPoint(0);

        // Đảm bảo thép luôn hướng lên
        if (longestLine != null)
        {
            double sumY = 0;
            int ptCount = 0;
            foreach (var curve in drawCurves)
            {
                var pts = curve.Tessellate();
                foreach (var pt in pts)
                {
                    sumY += -(pt - p0).DotProduct(yAxis);
                    ptCount++;
                }
            }
            double avgY = sumY / ptCount;
            double mainY = -(longestLine.GetEndPoint(0) - p0).DotProduct(yAxis);

            if (avgY > mainY + 0.1) yAxis = -yAxis;
        }

        // 5. CHIẾU CÁC NÉT VẼ SANG 2D
        var points2D = new List<System.Windows.Point>();
        var drawSegments2D = new List<Tuple<System.Windows.Point, System.Windows.Point>>();

        foreach (var curve in drawCurves)
        {
            var pts = curve.Tessellate();
            for (int i = 0; i < pts.Count - 1; i++)
            {
                var pA = pts[i];
                var pB = pts[i + 1];

                var wpA = new System.Windows.Point((pA - p0).DotProduct(xAxis), -(pA - p0).DotProduct(yAxis));
                var wpB = new System.Windows.Point((pB - p0).DotProduct(xAxis), -(pB - p0).DotProduct(yAxis));

                points2D.Add(wpA);
                points2D.Add(wpB);
                drawSegments2D.Add(new Tuple<System.Windows.Point, System.Windows.Point>(wpA, wpB));
            }
        }

        if (points2D.Count == 0) return null;

        // --- BƯỚC QUAN TRỌNG: TẠO GIAO CẮT ẢO (SHARP INTERSECTIONS) ---
        // Giả lập lại cách Revit kéo dài đường tâm để tính chiều dài A, B, C, D
        var segments2D = new List<Tuple<System.Windows.Point, System.Windows.Point>>();
        foreach (var curve in mainSegments)
        {
            if (curve is Line line)
            {
                var wpA = new System.Windows.Point((line.GetEndPoint(0) - p0).DotProduct(xAxis), -(line.GetEndPoint(0) - p0).DotProduct(yAxis));
                var wpB = new System.Windows.Point((line.GetEndPoint(1) - p0).DotProduct(xAxis), -(line.GetEndPoint(1) - p0).DotProduct(yAxis));
                segments2D.Add(new Tuple<System.Windows.Point, System.Windows.Point>(wpA, wpB));
            }
        }

        var extendedSegments2D = new List<Tuple<System.Windows.Point, System.Windows.Point>>();
        foreach (var seg in segments2D) extendedSegments2D.Add(seg);

        // Nối các điểm bằng cách giao cắt các đường thẳng kéo dài
        for (int i = 1; i < extendedSegments2D.Count; i++)
        {
            var prev = extendedSegments2D[i - 1];
            var curr = extendedSegments2D[i];
            var intersection = IntersectLines(prev.Item1, prev.Item2, curr.Item1, curr.Item2);

            // Nếu giao cắt nằm ở khoảng cách hợp lý (tránh lỗi nối nhầm cạnh)
            if (intersection.HasValue && Distance(intersection.Value, prev.Item2) < 2.0 && Distance(intersection.Value, curr.Item1) < 2.0)
            {
                extendedSegments2D[i - 1] = new Tuple<System.Windows.Point, System.Windows.Point>(prev.Item1, intersection.Value);
                extendedSegments2D[i] = new Tuple<System.Windows.Point, System.Windows.Point>(intersection.Value, curr.Item2);
            }
        }

        // Nối cho các thép đai kín khép vòng (Stirrups)
        if (extendedSegments2D.Count > 2)
        {
            var first = extendedSegments2D[0];
            var last = extendedSegments2D[extendedSegments2D.Count - 1];
            var intersection = IntersectLines(last.Item1, last.Item2, first.Item1, first.Item2);
            if (intersection.HasValue && Distance(intersection.Value, last.Item2) < 2.0 && Distance(intersection.Value, first.Item1) < 2.0)
            {
                extendedSegments2D[extendedSegments2D.Count - 1] = new Tuple<System.Windows.Point, System.Windows.Point>(last.Item1, intersection.Value);
                extendedSegments2D[0] = new Tuple<System.Windows.Point, System.Windows.Point>(intersection.Value, first.Item2);
            }
        }

        // 6. MAP PARAMETER VÀO CÁC ĐOẠN THẲNG MỞ RỘNG (A, B, C, D)
        var textLabels2D = new List<Tuple<System.Windows.Point, string>>();

        foreach (var seg in extendedSegments2D)
        {
            // Chiều dài lúc này đã bao hàm cả phần bù góc uốn (bend deduction)
            double physicalLen = Distance(seg.Item1, seg.Item2) * 304.8;
            if (physicalLen < 1.0) continue;

            Parameter? closestParam = null;
            double minDiff = double.MaxValue;

            foreach (var p in validParams)
            {
                double pVal = p.AsDouble() * 304.8;

                // Revit đo kích thước theo 3 chuẩn: Centerline (Tâm), Outer (Phủ bì), Inner (Lọt lòng)
                // Thuật toán kiểm tra và tự động bắt dính chuẩn sai số nhỏ nhất
                double diff1 = Math.Abs(pVal - physicalLen);
                double diff2 = Math.Abs(pVal - (physicalLen + barDiameterMm));
                double diff3 = Math.Abs(pVal - (physicalLen + barDiameterMm / 2));
                double diff4 = Math.Abs(pVal - (physicalLen - barDiameterMm));

                double diff = Math.Min(diff1, Math.Min(diff2, Math.Min(diff3, diff4)));

                if (diff < minDiff && diff < 150.0) // Dung sai an toàn
                {
                    minDiff = diff;
                    closestParam = p;
                }
            }

            string labelText;
            if (closestParam != null)
            {
                double exactVal = closestParam.AsDouble() * 304.8;
                labelText = $"{closestParam.Definition.Name}={Math.Round(exactVal, 1).ToString("0.0", CultureInfo.InvariantCulture)}";
            }
            else
            {
                labelText = Math.Round(physicalLen + barDiameterMm, 1).ToString("0.0", CultureInfo.InvariantCulture);
            }

            // Đặt chữ tại điểm giữa của đoạn thẳng GÓC NHỌN (Căn giữa hoàn hảo trên mặt trực quan)
            var mid2D = new System.Windows.Point((seg.Item1.X + seg.Item2.X) / 2, (seg.Item1.Y + seg.Item2.Y) / 2);
            textLabels2D.Add(new Tuple<System.Windows.Point, string>(mid2D, labelText));
        }

        // 7. CĂN CHỈNH TỶ LỆ HÌNH ẢNH VÀ LỰC ĐẨY CHỮ (CHỐNG TEXT ĂN VÀO NÉT VẼ)
        double minX = points2D.Min(p => p.X);
        double maxX = points2D.Max(p => p.X);
        double minY = points2D.Min(p => p.Y);
        double maxY = points2D.Max(p => p.Y);

        double centerX = (minX + maxX) / 2.0;
        double centerY = (minY + maxY) / 2.0;

        double width = maxX - minX == 0 ? 1 : maxX - minX;
        double height = maxY - minY == 0 ? 1 : maxY - minY;

        // Cố định vùng không gian lõi dành riêng cho nét vẽ thép là 250px
        double targetSize = 350.0;

        // Scale nét vẽ để luôn lấp đầy vùng 250px này
        double scale = Math.Min(targetSize / width, targetSize / height);

        double drawnWidth = width * scale;
        double drawnHeight = height * scale;

        // TĂNG VÙNG AN TOÀN (PADDING) LÊN 240px ĐỂ CHỨA NHỮNG CHỮ BỊ ĐẨY RA XA
        double padding = 310.0;
        double canvasSize = targetSize + padding; // Tổng diện tích khung = 490x490

        // Tính tâm hoàn hảo để nét vẽ luôn được neo ở chính giữa Canvas
        double offsetX = (canvasSize - drawnWidth) / 2.0;
        double offsetY = (canvasSize - drawnHeight) / 2.0;

        // 8. TIẾN HÀNH VẼ VÀ RENDER XUẤT ẢNH
        DrawingGroup drawingGroup = new DrawingGroup();
        using (DrawingContext dc = drawingGroup.Open())
        {
            // Đóng băng Canvas bằng nền trong suốt
            dc.DrawRectangle(System.Windows.Media.Brushes.Transparent, null, new Rect(0, 0, canvasSize, canvasSize));

            Pen linePen = new Pen(new SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 150, 243)), 10)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round
            };

            // VẼ NÉT THÉP TRƯỚC
            foreach (var seg in drawSegments2D)
            {
                var start = new System.Windows.Point((seg.Item1.X - minX) * scale + offsetX, (seg.Item1.Y - minY) * scale + offsetY);
                var end = new System.Windows.Point((seg.Item2.X - minX) * scale + offsetX, (seg.Item2.Y - minY) * scale + offsetY);
                dc.DrawLine(linePen, start, end);
            }

            // VẼ CHỮ VÀ NỀN CHỮ SAU
            foreach (var label in textLabels2D)
            {
                var mid = new System.Windows.Point((label.Item1.X - minX) * scale + offsetX, (label.Item1.Y - minY) * scale + offsetY);
                var centerPt = new System.Windows.Point((centerX - minX) * scale + offsetX, (centerY - minY) * scale + offsetY);

                // -> TẠO TEXT TRƯỚC ĐỂ ĐO KÍCH THƯỚC <-
#pragma warning disable CS0618
                System.Windows.Media.FormattedText text = new System.Windows.Media.FormattedText(
                    label.Item2,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
                    35, // Size chữ
                    System.Windows.Media.Brushes.Red, 1.0);
#pragma warning restore CS0618

                // Thuật toán đẩy ly tâm thông minh (Tính toán dựa trên size của box chữ)
                double dx = mid.X - centerPt.X;
                double dy = mid.Y - centerPt.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                if (dist > 1.0)
                {
                    double nx = dx / dist;
                    double ny = dy / dist;

                    // Đẩy văng ra = Một nửa bề rộng/cao của chữ + Khoảng cách an toàn (10 pixels)
                    double pushDistX = Math.Abs(nx) * (text.Width / 2.0 + 10.0);
                    double pushDistY = Math.Abs(ny) * (text.Height / 2.0 + 10.0);

                    // Tổng lực đẩy vector
                    double pushDist = pushDistX + pushDistY;

                    mid.X += nx * pushDist;
                    mid.Y += ny * pushDist;
                }

                // Căn chỉnh Text Center chính xác
                var textPos = new System.Windows.Point(mid.X - text.Width / 2, mid.Y - text.Height / 2);

                // Vẽ nền trắng bảo vệ chữ
                dc.DrawRectangle(System.Windows.Media.Brushes.White, null, new Rect(textPos.X - 1.5, textPos.Y - 1.5, text.Width + 3, text.Height + 3));
                // Vẽ chữ
                dc.DrawText(text, textPos);
            }
        }

        // Cắt khung chuẩn theo kích thước đã tính toán
        drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0, 0, canvasSize, canvasSize));
        var image = new DrawingImage(drawingGroup);
        image.Freeze();

        return image;
    }
    // Các hàm toán học bổ trợ tính toán giao cắt ảo
    private static System.Windows.Point? IntersectLines(System.Windows.Point p1, System.Windows.Point p2, System.Windows.Point p3, System.Windows.Point p4)
    {
        double denominator = (p1.X - p2.X) * (p3.Y - p4.Y) - (p1.Y - p2.Y) * (p3.X - p4.X);
        if (Math.Abs(denominator) < 1e-6) return null; // Nếu 2 đường song song thì bỏ qua

        double t = ((p1.X - p3.X) * (p3.Y - p4.Y) - (p1.Y - p3.Y) * (p3.X - p4.X)) / denominator;
        return new System.Windows.Point(p1.X + t * (p2.X - p1.X), p1.Y + t * (p2.Y - p1.Y));
    }

    private static double Distance(System.Windows.Point p1, System.Windows.Point p2)
    {
        return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
    }
}