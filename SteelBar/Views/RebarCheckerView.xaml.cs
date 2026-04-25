using SteelBar.Models;
using SteelBar.Extensions;
using SteelBar.ViewModels;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using static SteelBar.Extensions.DataGridFilterExtension;

namespace SteelBar.Views;

public partial class RebarCheckerView : Window
{
    private readonly RebarCheckerViewModel _viewModel;
    private readonly Dictionary<string, List<ColumnFilterItem>> _columnFilters = new();
    public RebarCheckerView(RebarCheckerViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel; // Gắn data để XAML hiểu
    }
    private void MyDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is DataGrid dataGrid)
        {
            // Truyền thẳng danh sách các dòng đang bôi đen vào property SelectedRebar của ViewModel
            _viewModel.SelectedRebar = dataGrid.SelectedItems;
        }
    }
    // Khi người dùng bấm mở Popup
    private void FilterBtn_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton toggleBtn && _viewModel != null)
        {
            string propertyName = toggleBtn.Tag as string;
            if (string.IsNullOrEmpty(propertyName)) return;

            if (toggleBtn.Parent is StackPanel panel)
            {
                var popup = panel.Children.OfType<Popup>().FirstOrDefault();
                if (popup?.Child is Border border && border.Child is DockPanel dockPanel)
                {
                    var scroll = dockPanel.Children.OfType<ScrollViewer>().FirstOrDefault();
                    if (scroll?.Content is ItemsControl itemsControl)
                    {
                        // SỬA DÒNG NÀY: Gọi hàm GetVisibleFilterItems thay vì hàm cũ
                        itemsControl.ItemsSource = _viewModel.GetVisibleFilterItems(propertyName);
                    }
                }
            }
        }
    }

    // Sự kiện kích hoạt khi User bấm Tick/Untick 1 CheckBox bất kỳ
    /// <summary>
    /// Xử lý khi bấm nút (Chọn tất cả)
    /// </summary>
    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox selectAllCb && _viewModel != null)
        {
            string propertyName = selectAllCb.Tag as string;
            if (string.IsNullOrEmpty(propertyName)) return;

            bool isChecked = selectAllCb.IsChecked == true;

            // SỬA Ở ĐÂY: Chỉ đổi trạng thái các item ĐANG HIỂN THỊ trong popup
            var visibleItems = _viewModel.GetVisibleFilterItems(propertyName);
            foreach (var item in visibleItems)
            {
                item.IsSelected = isChecked;
            }
        }
    }

    /// <summary>
    /// Chạy Filter 1 LẦN DUY NHẤT khi người dùng click chuột ra ngoài (Popup bị đóng)
    /// </summary>
    private void FilterPopup_Closed(object sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.ExecuteExcelFilter();
        }
    }

    private void ApplyExcelFilter()
    {
        var activeFilters = new Dictionary<string, HashSet<string>>();

        foreach (var kvp in _columnFilters)
        {
            // Chỉ thêm vào bộ lọc NẾU cột đó có item bị Untick (để tăng tốc độ lọc)
            if (kvp.Value.Any(x => !x.IsSelected))
            {
                var selectedValues = kvp.Value.Where(x => x.IsSelected).Select(x => x.Value);
                activeFilters[kvp.Key] = new HashSet<string>(selectedValues);
            }
        }

        // Gọi Extension lọc DataGrid
        var view = CollectionViewSource.GetDefaultView(_viewModel.RebarList);
        view.ApplyExcelCheckboxFilter<RebarInfo>(activeFilters);
    }
}