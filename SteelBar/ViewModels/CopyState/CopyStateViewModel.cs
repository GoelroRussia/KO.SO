using Autodesk.Revit.UI;
using SteelBar.Models.CopyState;
using SteelBar.Utils.CopyState;

namespace SteelBar.ViewModels.CopyState
{
    public partial class CopyStateViewModel : ObservableObject
    {
        private readonly View _activeView;
        private readonly ViewStateService _viewStateService;
        private readonly Action _closeWindowAction = null!;
        // 1. Fiedls
        [ObservableProperty]
        private bool _isVisibilityGraphicsSelected ;
        [ObservableProperty]
        private bool _isSectionBoxSelected;
        [ObservableProperty]
        private bool _isCropRegionSelected ;
        [ObservableProperty]
        private bool _isFiltersSelected;
        public bool Is3DView => _activeView is View3D;

        // 2. Constructor
        public CopyStateViewModel(View activeView, Action closeWindowAction)
        {
            _activeView = activeView;
            _closeWindowAction = closeWindowAction; // Gán giá trị ở đây
            _viewStateService = new ViewStateService();

            if (Is3DView)
            {
                IsSectionBoxSelected = true;
            }
        }

        // 3. Methods
        [RelayCommand]
        private void Copy()
        {
            try
            {
                // 1. Gọi Service để lấy dữ liệu từ Revit
                var stateData = _viewStateService.CaptureState(
                    _activeView,
                    IsVisibilityGraphicsSelected,
                    IsSectionBoxSelected,
                    IsCropRegionSelected,
                    IsFiltersSelected
                );

                // 2. Lưu dữ liệu vào Clipboard tĩnh (Static)
                StateClipboard.CopiedState = stateData;

                // 3. Thông báo người dùng (Optional)
                TaskDialog.Show("Hoàn thầnh", "Đã Copy View Setting vào bộ nhớ tạm");

                // 4. Đóng cửa sổ
                _closeWindowAction();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Lỗi", $"Đã xảy ra lỗi: {ex.Message}");
            }
        }
        [RelayCommand]
        private void Close()
        {
            _closeWindowAction();
        }
    }
}