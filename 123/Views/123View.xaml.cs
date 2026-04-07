using _123.ViewModels;

namespace _123.Views
{
    public sealed partial class _123View
    {
        public _123View(_123ViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}