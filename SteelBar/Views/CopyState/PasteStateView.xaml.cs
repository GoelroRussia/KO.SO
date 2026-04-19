using SteelBar.ViewModels.CopyState;
using System.Windows;

namespace SteelBar.Views.CopyState
{
    public sealed partial class PasteStateView
    {
        public PasteStateView(PasteStateViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}