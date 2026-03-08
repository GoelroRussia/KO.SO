using SteelBar.ViewModels;
using System.Windows;

namespace SteelBar.Views;

public partial class RebarCheckerView : Window
{
    public RebarCheckerView(RebarCheckerViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}