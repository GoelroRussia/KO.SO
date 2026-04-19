using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SteelBar.Models.CopyState
{
    public partial class ViewItem : ObservableObject
    {
        public View RevitView { get; }
        public string Name { get; }
        public string ViewType { get; }

        // Biến này sẽ bind với Checkbox trên giao diện
        [ObservableProperty]
        private bool _isSelected;

        public ViewItem(View view, bool isSelected = false)
        {
            RevitView = view;
            Name = view.Name;
            ViewType = view.ViewType.ToString();
            IsSelected = isSelected;
        }
    }
}