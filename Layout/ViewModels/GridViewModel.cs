using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Autodesk.Revit.DB;
using Layout.Utils;
using Autodesk.Revit.UI;

namespace Layout.ViewModels
{
    public partial class GridViewModel : ObservableObject
    {
        private readonly UIApplication _uiApp;
        private readonly GridService _gridService;

        [ObservableProperty]
        private bool _is3DSelected; // Mặc định là false (2D)

        public GridViewModel(UIApplication uiApp, GridService gridService)
        {
            _uiApp = uiApp;
            _gridService = gridService;
        }

        // Lệnh được gọi mỗi khi cần gạt thay đổi trạng thái
        [RelayCommand]
        private void ApplyGridState()
        {
            var doc = _uiApp.ActiveUIDocument.Document;
            var view = doc.ActiveView;

            // Xử lý External Event hoặc Transaction nếu dùng Modeless Window. 
            // Dưới đây giả định chạy đồng bộ (Modal) hoặc đã nằm trong context của Revit
            _gridService.ToggleGridsExtent(doc, view, Is3DSelected);
        }
    }
}