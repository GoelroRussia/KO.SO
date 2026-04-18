using SteelBar.ViewModels;
using System.Windows.Controls;
using System.Windows;

namespace SteelBar.Views;

public partial class RebarCheckerView : Window
{
    private readonly RebarCheckerViewModel _viewModel;
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
}