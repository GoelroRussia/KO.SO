using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using SteelBar.Models;
using System.Collections.ObjectModel;
using System.Linq;
using Serilog;

namespace SteelBar.ViewModels;

public partial class RebarCheckerViewModel : ObservableObject
{
    private readonly UIDocument _uidoc;
    private readonly Document _doc;
    private List<Rebar> _allRebars = new();

    // Thông số làm tròn do người dùng nhập (Mặc định là 10 mm)
    [ObservableProperty] private double _roundingStep = 10.0;

    // Danh sách thép lẻ hiển thị lên DataGrid
    [ObservableProperty] private ObservableCollection<RebarInfo> _rebarList = new();

    // Danh sách các Host Category hiển thị lên giao diện (dạng Checkbox)
    [ObservableProperty] private ObservableCollection<CategorySelection> _hostCategories = new();

    [ObservableProperty] private int _totalRebars;
    [ObservableProperty] private RebarInfo _selectedRebar;

    [ObservableProperty] private int _totalAssemblies;

    public RebarCheckerViewModel(UIDocument uidoc)
    {
        _uidoc = uidoc;
        _doc = uidoc.Document; 
        LoadCategories();
    }

    private void LoadCategories()
    {
        _allRebars = new FilteredElementCollector(_doc)
            .OfCategory(BuiltInCategory.OST_Rebar)
            .WhereElementIsNotElementType()
            .Cast<Rebar>()
            .ToList();

        var uniqueCategories = new HashSet<string>();

        foreach (var rebar in _allRebars)
        {
            ElementId hostId = rebar.GetHostId();
            if (hostId != ElementId.InvalidElementId)
            {
                Element host = _doc.GetElement(hostId);
                if (host?.Category != null)
                {
                    uniqueCategories.Add(host.Category.Name);
                }
            }
        }

        foreach (var cat in uniqueCategories)
        {
            // Thêm vào danh sách và tích chọn sẵn, không cần gán sự kiện PropertyChanged nữa
            HostCategories.Add(new CategorySelection { CategoryName = cat, IsSelected = true });
        }
    }

    // MVVM Toolkit sẽ tự sinh ra CheckRebarsCommand từ hàm này
    [RelayCommand]
    private void CheckRebars()
    {
        // Xóa DataGrid hiện tại trước khi check lại
        RebarList.Clear();

        // Kiểm tra tránh lỗi chia cho 0 nếu người dùng nhập linh tinh
        if (RoundingStep <= 0) RoundingStep = 1;

        var selectedCategories = HostCategories
            .Where(c => c.IsSelected)
            .Select(c => c.CategoryName)
            .ToList();

        foreach (var rebar in _allRebars)
        {
            ElementId hostId = rebar.GetHostId();
            string hostCategoryName = string.Empty;

            if (hostId != ElementId.InvalidElementId)
            {
                Element host = _doc.GetElement(hostId);
                if (host?.Category != null)
                {
                    hostCategoryName = host.Category.Name;
                }
            }

            // Lọc theo Category đã tick
            if (!selectedCategories.Contains(hostCategoryName)) continue;

            foreach (Parameter param in rebar.Parameters)
            {
                bool isDimensionGroup;
#if REVIT2024_OR_GREATER
                isDimensionGroup = param.Definition.GetGroupTypeId() == GroupTypeId.Geometry;
#else
                isDimensionGroup = param.Definition.ParameterGroup == BuiltInParameterGroup.PG_GEOMETRY;
#endif

                if (isDimensionGroup && param.StorageType == StorageType.Double)
                {
                    double internalValue = param.AsDouble();
                    double valueInMm = UnitUtils.ConvertFromInternalUnits(internalValue, UnitTypeId.Millimeters);
                    // LOGIC TÌM LẺ SỐ MỚI:
                    // 1. Lấy giá trị thép chia cho hệ số làm tròn, làm tròn nó, rồi nhân ngược lại.
                    // VD: Thép dài 1025mm, làm tròn tới 10mm -> 1025/10 = 102.5 -> Làm tròn thành 103 -> 103 * 10 = 1030mm.
                    double nearestRoundedValue = Math.Round(valueInMm / RoundingStep) * RoundingStep;

                    // 2. Nếu giá trị thực tế khác với giá trị đã được làm tròn (dung sai 0.001) thì đây là thép lẻ số
                    if (Math.Abs(valueInMm - nearestRoundedValue) > 0.001)
                    {
                        // Lấy giá trị của parameter "Assembly Name"
                        Parameter assemblyParam = rebar.LookupParameter("Assembly Name");
                        // Lưu ý: Nếu Assembly Name của bạn là parameter mặc định của tính năng Create Assembly trong Revit, 
                        // hãy dùng: rebar.get_Parameter(BuiltInParameter.ASSEMBLY_NAME);

                        RebarList.Add(new RebarInfo
                        {
                            ElementId = rebar.Id.IntegerValue,
                            AssemblyName = assemblyParam?.AsString() ?? "",
                            ParameterName = param.Definition.Name,
                            Value = Math.Round(valueInMm, 2)
                        });
                    }
                }
            }
        }

        TotalRebars = RebarList.Count;

        // Lấy danh sách các Assembly Name, lọc các tên trùng lặp (Distinct) và đếm
        TotalAssemblies = RebarList.Select(x => x.AssemblyName).Distinct().Count();


        Log.Information("Bắt đầu kiểm tra thép lẻ số với hệ số làm tròn: {RoundingStep}", RoundingStep);

        try
        {
            // Logic kiểm tra thép của bạn...
            Log.Information("Tìm thấy {Count} thanh thép lẻ số.", RebarList.Count);
        }
        catch (Exception ex)
        {
            // Ghi lại lỗi chi tiết vào file log nếu chương trình bị crash
            Log.Error(ex, "Có lỗi xảy ra khi đang kiểm tra thép!");
        }
    }

    [RelayCommand]
    private void ZoomToRebar()
    {
        if (SelectedRebar == null) return;

        // Khởi tạo ElementId (Hỗ trợ tương thích ngược cho các bản Revit khác nhau)
#if REVIT2024_OR_GREATER
        ElementId rebarId = new ElementId((long)SelectedRebar.ElementId);
#else
        ElementId rebarId = new ElementId(SelectedRebar.ElementId);
#endif

        // 1. Highlight (Chọn) cây thép đó trên mô hình
        _uidoc.Selection.SetElementIds(new List<ElementId> { rebarId });

        // 2. Tự động chuyển view và Zoom focus vào cây thép đó
        _uidoc.ShowElements(rebarId);
    }
}