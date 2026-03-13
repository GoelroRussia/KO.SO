using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Windows.Media;

namespace SteelBar.Models;

public partial class RebarInfo : ObservableObject
{
    public int ElementId { get; set; }
    public string AssemblyName { get; set; }
    public string ParameterName { get; set; }

    [ObservableProperty] private double _value;
    public System.Windows.Media.Geometry ShapeGeometry { get; set; }
    public double OriginalValue { get; set; }
}

public partial class CategorySelection : ObservableObject
{
    [ObservableProperty] private string _categoryName;
    [ObservableProperty] private bool _isSelected = true;
}