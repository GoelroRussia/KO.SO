using System.Windows;
using SteelBar.ViewModels.Layout;

namespace SteelBar.Views.Layout
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