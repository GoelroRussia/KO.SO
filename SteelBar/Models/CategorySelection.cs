using CommunityToolkit.Mvvm.ComponentModel;

namespace SteelBar.Models;

public partial class CategorySelection : ObservableObject
{
    // Tên của Category (Dầm, Cột, Sàn...)
    [ObservableProperty]
    private string _categoryName;

    // Trạng thái tick box (Mặc định cho tick sẵn)
    [ObservableProperty]
    private bool _isSelected = true;
}