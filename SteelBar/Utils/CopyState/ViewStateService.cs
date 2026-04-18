
using SteelBar.Models.CopyState;

namespace SteelBar.Utils.CopyState
{
    public class ViewStateService
    {
        public ViewStateData CaptureState(View sourceView, bool copyVg, bool copySectionBox, bool copyCrop, bool copyFilters)
        {
            var state = new ViewStateData();
            var doc = sourceView.Document;

            // 1. Visibility Graphics
            if (copyVg)
            {
                state.HasVisGraphics = true;
                foreach (Category cat in doc.Settings.Categories)
                {
                    try
                    {
                        if (sourceView.GetCategoryHidden(cat.Id))
                            state.CategoryVisibility[cat.Id] = false;

                        var overrideSettings = sourceView.GetCategoryOverrides(cat.Id);
                        // Chỉ lưu nếu có override thực sự để tiết kiệm bộ nhớ
                        /* Logic kiểm tra override mặc định có thể thêm ở đây */
                        state.CategoryOverrides[cat.Id] = overrideSettings;
                    }
                    catch { /* Bỏ qua category không hỗ trợ view override */ }
                }
            }

            // 2. Section Box (Chỉ View 3D)
            if (copySectionBox && sourceView is View3D view3d)
            {
                state.HasSectionBox = true;
                state.IsSectionBoxActive = view3d.IsSectionBoxActive;
                if (view3d.IsSectionBoxActive)
                {
                    state.SectionBox = view3d.GetSectionBox();
                }
            }

            // 3. Crop Region
            if (copyCrop)
            {
                state.HasCropRegion = true;
                state.CropBoxActive = sourceView.CropBoxActive;
                state.CropBoxVisible = sourceView.CropBoxVisible;
                state.CropBox = sourceView.CropBox;
            }

            // 4. Filters
            if (copyFilters)
            {
                state.HasFilters = true;
                var filters = sourceView.GetFilters();
                foreach (var filterId in filters)
                {
                    state.ActiveFilters.Add(filterId);
                    state.FilterOverrides[filterId] = sourceView.GetFilterOverrides(filterId);
                    state.FilterVisibility[filterId] = sourceView.GetFilterVisibility(filterId);
                }
            }

            return state;
        }

        public void ApplyState(View? targetView, ViewStateData state)
        {
            if (state.HasVisGraphics)
            {
                foreach (var kvp in state.CategoryOverrides)
                {
                    if (targetView!.CanCategoryBeHidden(kvp.Key))
                        targetView.SetCategoryOverrides(kvp.Key, kvp.Value);
                }
                foreach (var kvp in state.CategoryVisibility)
                {
                    if (targetView!.CanCategoryBeHidden(kvp.Key))
                        targetView.SetCategoryHidden(kvp.Key, !kvp.Value);
                }
            }

            // Apply Section Box
            if (state.HasSectionBox && targetView is View3D target3d)
            {
                target3d.IsSectionBoxActive = state.IsSectionBoxActive;
                if (state.IsSectionBoxActive)
                {
                    target3d.SetSectionBox(state.SectionBox);
                }
            }

            // Apply Crop Region
            if (state.HasCropRegion)
            {
                targetView!.CropBoxActive = state.CropBoxActive;
                targetView.CropBoxVisible = state.CropBoxVisible;
                if (state.CropBox != null) targetView.CropBox = state.CropBox;
            }

            // --- LOGIC XỬ LÝ FILTER & VIEW TEMPLATE ---
            if (state.HasFilters)
            {
                // 1. Xác định View đích thực sự (View hiện tại hay View Template?)
                View viewToApply = targetView!;
                bool isControlledByTemplate = false;

                // Kiểm tra xem View có đang gán Template không
                if (targetView!.ViewTemplateId != ElementId.InvalidElementId)
                {
                    // Lấy đối tượng View Template
                    View templateView = (targetView.Document.GetElement(targetView.ViewTemplateId) as View)!;

                    {
                        // Kiểm tra xem View con có bị khóa Filter bởi Template không?
                        targetView.GetNonControlledTemplateParameterIds();
                        viewToApply = templateView;
                        isControlledByTemplate = true;
                    }
                }

                // 2. Thực hiện Xóa cũ - Thêm mới trên View đã xác định (viewToApply)

                // Mở try-catch để an toàn
                try
                {
                    // BƯỚC A: Xóa các Filter cũ đang tồn tại trên viewToApply
                    var existingFilters = viewToApply.GetFilters();
                    foreach (var existingId in existingFilters)
                    {
                        viewToApply.RemoveFilter(existingId);
                    }

                    // BƯỚC B: Thêm các Filter mới từ Clipboard (state)
                    foreach (var filterId in state.ActiveFilters)
                    {
                        if (!viewToApply.IsFilterApplied(filterId))
                        {
                            viewToApply.AddFilter(filterId);

                            // Khôi phục Override (Màu sắc, nét...)
                            if (state.FilterOverrides.ContainsKey(filterId))
                            {
                                viewToApply.SetFilterOverrides(filterId, state.FilterOverrides[filterId]);
                            }

                            // Khôi phục Visibility (Ẩn/Hiện)
                            if (state.FilterVisibility.ContainsKey(filterId))
                            {
                                viewToApply.SetFilterVisibility(filterId, state.FilterVisibility[filterId]);
                            }
                        }
                    }

                    // (Tùy chọn) Thông báo nhỏ nếu đã paste vào Template
                    if (isControlledByTemplate)
                    {
                        // Debug.WriteLine("Đã Paste vào View Template thay vì View hiện tại.");
                    }
                }
                catch (Exception)
                {
                    // Xử lý lỗi (ví dụ: filter không hợp lệ với loại view này)
                }
            }
        }
    }
}