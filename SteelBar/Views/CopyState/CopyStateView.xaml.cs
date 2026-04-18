using SteelBar.ViewModels.CopyState;
using System.Windows;

namespace SteelBar.Views.CopyState
{
    public sealed partial class CopyStateView
    {
        public CopyStateView(CopyStateViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
        }
    }
}