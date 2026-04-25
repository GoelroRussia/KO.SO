using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Nice3point.Revit.Extensions.Runtime;
using Nice3point.Revit.Toolkit.External.Handlers;
using Serilog;
using SteelBar.Extensions;
using SteelBar.Models;
using SteelBar.Utils;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Data;

namespace SteelBar.ViewModels;

public partial class RebarCheckerViewModel : ObservableObject
{
    public Dictionary<string, List<ColumnFilterItem>> ColumnFilters { get; } = new();
    private readonly ActionEventHandler _actionEventHandler;
    private readonly UIDocument _uidoc;
    private readonly Document _doc;
    private List<Rebar> _allRebars = new();
    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]


    private static extern bool DeleteObject(IntPtr hObject);
    // --- CÁC THUỘC TÍNH BINDING VỚI GIAO DIỆN XAML ---
    [ObservableProperty] private double _roundingStep = 10.0;
    [ObservableProperty] private ObservableCollection<RebarInfo> _rebarList = new();
    [ObservableProperty] private ObservableCollection<CategorySelection> _hostCategories = new();
    [ObservableProperty] private int _totalRebars;
    [ObservableProperty] private int _totalAssemblies;
    [ObservableProperty] private double _progressValue;
    [ObservableProperty] private string _progressText = "Sẵn sàng";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private int _selectedItemsForDeletion;
    [ObservableProperty] private IList _selectedRebar;


    public RebarCheckerViewModel(UIDocument uidoc)
    {
        _uidoc = uidoc;
        _doc = uidoc.Document;
        _actionEventHandler = new ActionEventHandler();
        LoadInitialData();
    }
    private void InitializeFullFiltersIfNeeded(string propertyName)
    {
        if (!ColumnFilters.ContainsKey(propertyName))
        {
            PropertyInfo? propInfo = typeof(RebarInfo).GetProperty(propertyName);
            if (propInfo == null) return;

            var allDistinctValues = RebarList
                .Select(item => propInfo.GetValue(item)?.ToString() ?? "(Trống)")
                .Distinct()
                .OrderBy(v => v)
                .ToList();

            // Lưu toàn bộ vào bộ nhớ
            ColumnFilters[propertyName] = allDistinctValues.Select(val => new ColumnFilterItem
            {
                Value = val,
                IsSelected = true,
                ColumnName = propertyName
            }).ToList();
        }
    }
    public List<ColumnFilterItem> GetOrCreateFilterItems(string propertyName)
    {
        // Nếu cột này chưa được lấy dữ liệu filter bao giờ
        if (!ColumnFilters.ContainsKey(propertyName))
        {
            PropertyInfo? propInfo = typeof(RebarInfo).GetProperty(propertyName);
            if (propInfo == null) return new List<ColumnFilterItem>();

            // Lấy danh sách giá trị duy nhất từ RebarList
            var distinctValues = RebarList
                .Select(item => propInfo.GetValue(item)?.ToString() ?? "(Trống)")
                .Distinct()
                .OrderBy(v => v)
                .ToList();

            // Chuyển thành dạng Model để Bind lên UI (Mặc định chọn tất cả)
            ColumnFilters[propertyName] = distinctValues.Select(val => new ColumnFilterItem
            {
                Value = val,
                IsSelected = true,
                ColumnName = propertyName
            }).ToList();
        }

        return ColumnFilters[propertyName];
    }
    public List<ColumnFilterItem> GetVisibleFilterItems(string propertyName)
    {
        InitializeFullFiltersIfNeeded(propertyName);

        // Bước A: Lọc Data gốc qua TẤT CẢ các cột KHÁC (bỏ qua cột đang mở popup)
        var validItems = RebarList.Where(item =>
        {
            foreach (var filter in ColumnFilters)
            {
                if (filter.Key == propertyName) continue; // Bỏ qua cột hiện tại

                // Nếu cột khác đang có bộ lọc (có ô bị bỏ tick)
                if (filter.Value.Any(x => !x.IsSelected))
                {
                    var otherPropInfo = typeof(RebarInfo).GetProperty(filter.Key);
                    var otherVal = otherPropInfo?.GetValue(item)?.ToString() ?? "(Trống)";

                    var allowedValues = filter.Value.Where(x => x.IsSelected).Select(x => x.Value);

                    // Nếu giá trị dòng này không nằm trong danh sách cho phép của cột khác -> Loại
                    if (!allowedValues.Contains(otherVal)) return false;
                }
            }
            return true;
        });

        // Bước B: Lấy ra các giá trị duy nhất từ tập dữ liệu hợp lệ ở trên
        PropertyInfo? targetPropInfo = typeof(RebarInfo).GetProperty(propertyName);
        var visibleValues = validItems
            .Select(item => targetPropInfo?.GetValue(item)?.ToString() ?? "(Trống)")
            .Distinct()
            .ToHashSet();

        // Bước C: Trả về tham chiếu của các CheckBox nằm trong danh sách visibleValues
        return ColumnFilters[propertyName].Where(x => visibleValues.Contains(x.Value)).ToList();
    }
    public void ExecuteExcelFilter()
    {
        var activeFilters = new Dictionary<string, HashSet<string>>();

        foreach (var kvp in ColumnFilters)
        {
            // Chỉ đưa vào bộ lọc khi có ít nhất 1 CheckBox bị bỏ chọn (Untick)
            if (kvp.Value.Any(x => !x.IsSelected))
            {
                var selectedValues = kvp.Value.Where(x => x.IsSelected).Select(x => x.Value);
                activeFilters[kvp.Key] = new HashSet<string>(selectedValues);
            }
        }

        // Lấy View màng lọc của DataGrid hiện tại và gọi Extension
        ICollectionView view = CollectionViewSource.GetDefaultView(RebarList);
        view.ApplyExcelCheckboxFilter<RebarInfo>(activeFilters);
    }
    /// <summary>
    /// Hàm này chạy lúc mở form để lấy danh sách Host Category hiển thị lên các ô CheckBox
    /// </summary>
    private void LoadInitialData()
    {
        // Lấy toàn bộ Rebar trong mô hình
        _allRebars = new FilteredElementCollector(_doc)
            .OfCategory(BuiltInCategory.OST_Rebar)
            .WhereElementIsNotElementType()
            .Cast<Rebar>().ToList();

        // Lấy tên các Category chứa thép (Dầm, Cột, Sàn, Vách...) và loại bỏ trùng lặp
        var categories = _allRebars
            .Select(r => _doc.GetElement(r.GetHostId())?.Category?.Name)
            .Where(name => !string.IsNullOrEmpty(name))
            .Distinct();

        // Đưa vào danh sách hiển thị
        foreach (var cat in categories)
        {
            HostCategories.Add(new CategorySelection { CategoryName = cat! });
        }
    }

    /// <summary>
    /// Lệnh quét và kiểm tra thép lẻ số (Chạy bất đồng bộ để không treo giao diện)
    /// </summary>
    [RelayCommand]
    private async Task CheckRebars()
    {
        RebarList.Clear();
        if (IsBusy) return;
        IsBusy = true;
        RebarList.Clear();
        Log.Information("Bắt đầu kiểm tra thép lẻ với bước làm tròn: {Step}mm", RoundingStep);

        // Lấy các Category đang được tick trên giao diện
        var selectedCats = HostCategories.Where(x => x.IsSelected).Select(x => x.CategoryName).ToList();

        // TỪ KHÓA LOẠI TRỪ: Bỏ qua đường kính và các biến Max/Min Bar Length (Varying Rebar)
        var excludedKeywords = new[]
        {
            "diameter", "đường kính", "bar diameter", "bend diameter",
            "maximum barlength", "maximum bar length", "max bar length",
            "minimum barlength", "minimum bar length", "min bar length"
        };

        try
        {
            int total = _allRebars.Count;
            for (int i = 0; i < total; i++)
            {
                var rebar = _allRebars[i];

                // Cập nhật thanh tiến trình (Progress Bar) mỗi 20 cây thép để UI không bị treo
                // Nhường 1ms cho UI render lại đồ họa (Cực kỳ an toàn với Revit API vì không đổi Thread)
                if (i % 20 == 0 || i == total - 1)
                {
                    ProgressValue = (i + 1) * 100.0 / total;
                    ProgressText = $"Đang xử lý: {i + 1}/{total}";
                    await Task.Delay(1);
                }

                // Kiểm tra xem thép này có thuộc Host Category đang được tick không
                var hostCat = _doc.GetElement(rebar.GetHostId())?.Category?.Name;
                if (hostCat == null || !selectedCats.Contains(hostCat)) continue;

                // Bắt đầu duyệt các thông số của cây thép
                foreach (Parameter param in rebar.Parameters)
                {
                    // LỌC 1: Chỉ lấy thông số dạng Số thực (Double) và KHÔNG bị khóa (Not Read-Only)
                    if (param.IsReadOnly || param.StorageType != StorageType.Double) continue;

                    // LỌC 2: Chỉ lấy thông số thuộc nhóm Dimensions (Geometry)
                    bool isDim;
#if REVIT2024_OR_GREATER
                    isDim = param.Definition.GetGroupTypeId() == GroupTypeId.Geometry;
#else
                    isDim = param.Definition.ParameterGroup == BuiltInParameterGroup.PG_GEOMETRY;
#endif
                    if (!isDim) continue;

                    // LỌC 3: Loại bỏ các thông số chứa từ khóa bị cấm (Đường kính, Max/Min Length)
                    string pName = param.Definition.Name.ToLower();
                    if (excludedKeywords.Any(k => pName.Contains(k))) continue;

                    // TÍNH TOÁN LẺ SỐ
                    // Quy đổi giá trị từ hệ quy chiếu của Revit (Feet) sang Millimeters
                    double valMm = UnitUtils.ConvertFromInternalUnits(param.AsDouble(), UnitTypeId.Millimeters);

                    // Làm tròn theo thông số người dùng nhập (vd: chia 10, làm tròn, nhân 10)
                    double roundedValue = Math.Round(valMm / RoundingStep) * RoundingStep;
                    // Đọc tên Shape của thanh thép
                    string shapeNameStr = "None";
                    var shapeId = rebar.GetShapeId();
                    if (shapeId != ElementId.InvalidElementId)
                    {
                        var shapeElem = _doc.GetElement(shapeId);
                        shapeNameStr = shapeElem?.Name ?? "None";
                    }
                    // Nếu giá trị thực tế lệch với giá trị làm tròn (dung sai 0.001mm) -> Bắt lỗi
                    if (Math.Abs(valMm - roundedValue) > 0.01)
                    {
                        var asmParam = rebar.LookupParameter("Assembly Name");

                        // Vì không dùng Task.Run nên Add thẳng vào ObservableCollection không cần Dispatcher
                        RebarList.Add(new RebarInfo
                        {
#if REVIT2024_OR_GREATER
                            ElementId = rebar.Id.Value,
#else                            
                            ElementId = (int)rebar.Id.Value,
#endif
                            AssemblyName = asmParam?.AsString() ?? "None",
                            ParameterName = param.Definition.Name,
                            REBAR_TYPE = rebar.LookupParameter("REBAR_TYPE")?.AsString() ?? "None",
                            Value = Math.Round(valMm, 2),
                            OriginalValue = Math.Round(valMm, 2),
                            //ShapeGeometry = GetRebarShapeGeometry(rebar)
                            ShapeImage = SteelBar.Utils.RebarShapeGenerator.CreateRebarImage(rebar),
                            ShapeName = shapeNameStr
                        });
                    }
                }
            }

            // TỔNG KẾT SAU KHI QUÉT XONG
            TotalRebars = RebarList.Count;
            TotalAssemblies = RebarList.Select(x => x.AssemblyName).Distinct().Count();
            ProgressText = "Hoàn thành!";
            Log.Information("Hoàn tất quét thép. Tìm thấy {Count} thông số bị lẻ.", TotalRebars);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Lỗi bất ngờ xảy ra trong quá trình quét thép");
            ProgressText = "Có lỗi xảy ra, vui lòng xem file Log!";
        }
        finally
        {
            IsBusy = false; // Mở khóa nút bấm
        }
    }
    private void UpdateRebarParameter(RebarInfo info)
    {
        _actionEventHandler.Raise(app =>
        {
            using (Transaction t = new Transaction(_doc, "Cập nhật chiều dài thép từ Tool"))
            {
                t.Start();

#if REVIT2024_OR_GREATER
                ElementId id = new ElementId((long)info.ElementId);
#else
                ElementId id = new ElementId(info.ElementId);
#endif

                Element elem = _doc.GetElement(id);
                Parameter? p = elem?.LookupParameter(info.ParameterName);

                if (p != null && !p.IsReadOnly)
                {
                    double internalVal = UnitUtils.ConvertToInternalUnits(info.Value, UnitTypeId.Millimeters);
                    p.Set(internalVal);
                }

                t.Commit();
            }
        });
    }
    /// <summary>
    /// Lệnh nháy đúp chuột để chọn và Zoom màn hình tới thanh thép bị lỗi
    /// </summary>
    [RelayCommand]
    private void ZoomIn() // Không cần tham số nữa
    {
        // Lấy tất cả các thanh thép đang được bôi đen
        // Kiểm tra xem người dùng đã chọn gì trên DataGrid chưa
        if (SelectedRebar == null || SelectedRebar.Count == 0)
        {
            TaskDialog.Show("Thông báo", "Vui lòng chọn ít nhất một thanh thép trên bảng!");
            return;
        }

        // Ép kiểu (Cast) từ IList sang danh sách RebarInfo
        var danhSachCanChon = SelectedRebar.Cast<RebarInfo>().ToList();

        if (danhSachCanChon.Count == 0)
        {
            TaskDialog.Show("Thông báo", "Vui lòng chọn ít nhất một thanh thép!");
            return;
        }

        // Tái sử dụng lệnh Select
        SelectElements();

        _actionEventHandler.Raise(app =>
        {
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            View3D? view3D = doc.ActiveView as View3D;
            if (view3D == null || view3D.IsTemplate)
            {
                view3D = new FilteredElementCollector(doc)
                    .OfClass(typeof(View3D))
                    .Cast<View3D>()
                    .FirstOrDefault(v => !v.IsTemplate && v.Name.Contains("{3D}"));

                if (view3D != null)
                {
                    uidoc.ActiveView = view3D;
                }
            }

            var elementIds = new List<ElementId>();
            foreach (var rebarInfo in danhSachCanChon)
            {
#if REVIT2024_OR_GREATER
                elementIds.Add(new ElementId(rebarInfo.ElementId));
#else
            elementIds.Add(new ElementId((int)rebarInfo.ElementId));
#endif
            }

            if (elementIds.Any())
            {
                uidoc.ShowElements(elementIds);
            }
        });
    }

    private System.Windows.Media.Geometry GetRebarShapeGeometry(Rebar rebar)
    {
        if (rebar == null) return null!;

        // Lấy đường tâm thực tế của thép (bao gồm cả Hook)
        IList<Curve> curves = rebar.GetCenterlineCurves(false, false, false, MultiplanarOption.IncludeAllMultiplanarCurves, 0);
        if (curves == null || curves.Count == 0) return null!;

        // Tính toán hệ trục tọa độ 3D để chiếu về 2D
        XYZ normal = XYZ.BasisZ;
        if (rebar.IsRebarShapeDriven())
        {
            var accessor = rebar.GetShapeDrivenAccessor();
            normal = accessor.Normal;
        }

        XYZ xDir = normal.CrossProduct(XYZ.BasisZ);
        if (xDir.IsAlmostEqualTo(XYZ.Zero))
        {
            xDir = normal.CrossProduct(XYZ.BasisX);
        }
        xDir = xDir.Normalize();

        XYZ yDir = normal.CrossProduct(xDir).Normalize();

        var pathGeometry = new System.Windows.Media.PathGeometry();
        XYZ origin = curves[0].GetEndPoint(0); // Lấy điểm đầu làm mốc

        foreach (var curve in curves)
        {
            var pts = curve.Tessellate();
            if (pts.Count < 2) continue;

            var figure = new System.Windows.Media.PathFigure { IsClosed = false };

            // Chiếu điểm 3D lên mặt phẳng 2D
            System.Windows.Point ConvertTo2D(XYZ pt3d)
            {
                XYZ vec = pt3d - origin;
                double x = vec.DotProduct(xDir);
                double y = vec.DotProduct(yDir);
                return new System.Windows.Point(x, -y);
            }

            figure.StartPoint = ConvertTo2D(pts[0]);
            for (int i = 1; i < pts.Count; i++)
            {
                figure.Segments.Add(new System.Windows.Media.LineSegment(ConvertTo2D(pts[i]), true));
            }

            pathGeometry.Figures.Add(figure);
        }

        return pathGeometry;
    }
    [RelayCommand]
    private void ApplyChanges()
    {
        // Lọc ra các cây thép có Value bị sửa (khác với OriginalValue)
        var modifiedRebars = RebarList.Where(r => Math.Abs(r.Value - r.OriginalValue) > 0.1).ToList();

        if (!modifiedRebars.Any())
        {
            TaskDialog.Show("Thông báo", "Không có cấu kiện được cập nhật");
            return;
        }

        _actionEventHandler.Raise(app =>
        {
            using (Transaction t = new Transaction(_doc, "Áp dụng cập nhật chiều dài thép"))
            {
                t.Start();

                foreach (var info in modifiedRebars)
                {
#if REVIT2024_OR_GREATER
                    ElementId id = new ElementId((long)info.ElementId);
#else
                ElementId id = new ElementId(info.ElementId);
#endif
                    Element elem = _doc.GetElement(id);
                    Parameter? p = elem?.LookupParameter(info.ParameterName);

                    if (p != null && !p.IsReadOnly)
                    {
                        double internalVal = UnitUtils.ConvertToInternalUnits(info.Value, UnitTypeId.Millimeters);
                        p.Set(internalVal);
                    }

                    // Cập nhật lại giá trị gốc sau khi đã lưu thành công
                    info.OriginalValue = info.Value;
                }

                t.Commit();
            }
            TaskDialog.Show("Hoàn tất", $"Đã cập nhật chiều dài cho {modifiedRebars.Count} thanh thép thành công!");
        });
    }
    [RelayCommand]
    private void AutoRoundRebars(System.Collections.IList visibleItems)
    {
        if (visibleItems == null || visibleItems.Count == 0) return;

        // Đảm bảo hệ số làm tròn hợp lệ (tránh lỗi chia cho 0)
        if (RoundingStep <= 0) RoundingStep = 1;

        int count = 0;

        foreach (var item in visibleItems)
        {
            // Ép kiểu về RebarInfo
            if (item is RebarInfo info)
            {
                // Tính toán số làm tròn
                double nearestRoundedValue = Math.Round(info.OriginalValue / RoundingStep) * RoundingStep;

                // Nếu số mới khác số hiện tại thì cập nhật
                if (Math.Abs(info.Value - nearestRoundedValue) > 0.1)
                {
                    info.Value = nearestRoundedValue;
                    count++;
                }
            }
        }

        // Hiện thông báo rõ ràng cho người dùng
        if (count == 0)
        {
            Autodesk.Revit.UI.TaskDialog.Show("Thông báo", "Tất cả các thanh thép đã được làm tròn, không có đề xuất nào mới.");
        }
    }
    [RelayCommand]
    private void SelectElements()
    {
        // Kiểm tra xem người dùng đã chọn gì trên DataGrid chưa
        if (SelectedRebar == null || SelectedRebar.Count == 0)
        {
            TaskDialog.Show("Thông báo", "Vui lòng chọn ít nhất một thanh thép trên bảng!");
            return;
        }

        // Ép kiểu (Cast) từ IList sang danh sách RebarInfo
        var danhSachCanChon = SelectedRebar.Cast<RebarInfo>().ToList();

        if (danhSachCanChon.Count > 1500)
        {
            TaskDialog.Show("Cảnh báo", "Bạn đang chọn quá nhiều đối tượng cùng lúc! Revit có thể bị treo.");
            return;
        }

        // Đẩy lệnh tương tác vào Revit API context thông qua ActionEventHandler (Nice3point template)
        _actionEventHandler.Raise(app =>
        {
            var uidoc = app.ActiveUIDocument;
            if (uidoc == null) return;

            var elementIds = new List<ElementId>();

            // Convert sang ElementId (hỗ trợ version Revit như Building Coder thường nhắc)
            foreach (var rebarInfo in danhSachCanChon)
            {
#if REVIT2024_OR_GREATER
                elementIds.Add(new ElementId(rebarInfo.ElementId));
#else
                // Các bản Revit cũ sử dụng kiểu int
                elementIds.Add(new ElementId((int)rebarInfo.ElementId));
#endif
            }

            // Thực hiện highlight/chọn các đối tượng trong mô hình Revit
            uidoc.Selection.SetElementIds(elementIds);

            // (Tùy chọn bổ sung) Phóng to/đưa view về các thanh thép vừa chọn để user dễ thấy
            uidoc.ShowElements(elementIds);
        });
    }
}