using System.Windows;
using Layout.ViewModels;

namespace Layout.Views
{
    public partial class GridToolView : Window
    {
        public GridToolView(GridViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}