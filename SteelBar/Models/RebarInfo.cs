using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Windows.Media;

namespace SteelBar.Models;

public partial class RebarInfo : ObservableObject
{
    public long ElementId { get; set; }
    public string? AssemblyName { get; set; }
    public string? ParameterName { get; set; }

    [ObservableProperty] private double _value;
    public Geometry? ShapeGeometry { get; set; }
    public double OriginalValue { get; set; }
    [ObservableProperty] private ImageSource? _shapeImage;
    [ObservableProperty] private bool _isSelected;
}

public partial class CategorySelection : ObservableObject
{
    [ObservableProperty] private string? _categoryName;
    [ObservableProperty] private bool _isSelected = true;
}