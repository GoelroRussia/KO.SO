
using SteelBar.Models.CopyState;

namespace SteelBar.Utils.CopyState
{
    public class ViewStateService
    {

        public ViewStateData CaptureState(View sourceView, bool copyVg, bool copySectionBox, bool copyCrop, bool copyFilters)
        {
            var state = new ViewStateData();
            var doc = sourceView.Document;
            state.SourceDocument = doc;

            // 1. Visibility Graphics
            if (copyVg)
            {
                state.HasVisGraphics = true;
                foreach (Category cat in doc.Settings.Categories)
                {
                    if (!sourceView.CanCategoryBeHidden(cat.Id)) continue;

                    // Chỉ bắt các Category mặc định của hệ thống (BuiltInCategory luôn có giá trị < 0)
                    if (cat.Id.IntegerValue > 0) continue;

                    var builtInCat = (BuiltInCategory)cat.Id.IntegerValue;

                    if (sourceView.GetCategoryHidden(cat.Id))
                        state.CategoryVisibility[builtInCat] = false;

                    var overrideSettings = sourceView.GetCategoryOverrides(cat.Id);
                    state.CategoryOverrides[builtInCat] = overrideSettings;
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
                    // Lấy Element từ Document để đọc được cái Tên (Name) của Filter
                    if (doc.GetElement(filterId) is ParameterFilterElement filterElem)
                    {
                        string filterName = filterElem.Name;
                        state.ActiveFilters.Add(filterName);
                        state.SourceFilterIds.Add(filterId);
                        state.FilterOverrides[filterName] = sourceView.GetFilterOverrides(filterId);
                        state.FilterVisibility[filterName] = sourceView.GetFilterVisibility(filterId);
                    }
                }
            }

            return state;
        }

        public void ApplyState(View? targetView, ViewStateData state)
        {
            var targetDoc = targetView!.Document;

            // 1. Visibility Graphics
            if (state.HasVisGraphics)
            {
                foreach (var kvp in state.CategoryOverrides)
                {
                    var catId = new ElementId((int)kvp.Key);
                    // Kiểm tra xem Category này có tồn tại và hỗ trợ ẩn/hiện trong View đích không
                    if (Category.GetCategory(targetDoc, kvp.Key) != null && targetView.CanCategoryBeHidden(catId))
                    {
                        targetView.SetCategoryOverrides(catId, kvp.Value);
                    }
                }
                foreach (var kvp in state.CategoryVisibility)
                {
                    var catId = new ElementId((int)kvp.Key);
                    if (Category.GetCategory(targetDoc, kvp.Key) != null && targetView.CanCategoryBeHidden(catId))
                    {
                        targetView.SetCategoryHidden(catId, !kvp.Value);
                    }
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
                // 4.1. Lấy danh sách Filter đang có ở file đích
                var targetDocFilters = new FilteredElementCollector(targetDoc)
                    .OfClass(typeof(ParameterFilterElement))
                    .Cast<ParameterFilterElement>()
                    .ToDictionary(f => f.Name, f => f.Id);

                // 4.2. TÌM VÀ COPY NHỮNG FILTER CÒN THIẾU TỪ FILE GỐC SANG FILE ĐÍCH
                // (Điều kiện: File gốc vẫn đang được mở trong Revit - IsValidObject)
                if (state.SourceDocument != null && state.SourceDocument.IsValidObject && state.SourceFilterIds.Any())
                {
                    var missingFilterIds = new List<ElementId>();

                    foreach (var sourceFilterId in state.SourceFilterIds)
                    {
                        if (state.SourceDocument.GetElement(sourceFilterId) is ParameterFilterElement sourceFilter)
                        {
                            // Nếu file đích chưa có Filter này (so sánh theo Tên)
                            if (!targetDocFilters.ContainsKey(sourceFilter.Name))
                            {
                                missingFilterIds.Add(sourceFilterId);
                            }
                        }
                    }

                    // Dùng API của Revit để copy hàng loạt Filter bị thiếu sang file đích
                    if (missingFilterIds.Any())
                    {
                        try
                        {
                            ElementTransformUtils.CopyElements(
                                state.SourceDocument,
                                missingFilterIds,
                                targetDoc,
                                Transform.Identity,
                                new CopyPasteOptions()
                            );

                            // Sau khi Copy xong, phải lấy lại danh sách Filter ở file đích để cập nhật ID mới
                            targetDocFilters = new FilteredElementCollector(targetDoc)
                                .OfClass(typeof(ParameterFilterElement))
                                .Cast<ParameterFilterElement>()
                                .ToDictionary(f => f.Name, f => f.Id);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Không thể copy filter từ file gốc: {ex.Message}");
                        }
                    }
                }

                // 4.3. TIẾN HÀNH APPLY CÁC FILTER VÀO VIEW
                try
                {
                    // Xóa filter cũ trên View
                    foreach (var oldId in targetView.GetFilters())
                    {
                        targetView.RemoveFilter(oldId);
                    }

                    // Áp dụng filter từ bộ nhớ dựa trên mapping tên
                    foreach (var filterName in state.ActiveFilters)
                    {
                        // Lúc này targetDocFilters chắc chắn đã có đủ Filter (kể cả những cái vừa được copy sang)
                        if (targetDocFilters.TryGetValue(filterName, out ElementId newFilterId))
                        {
                            targetView.AddFilter(newFilterId);

                            if (state.FilterOverrides.ContainsKey(filterName))
                                targetView.SetFilterOverrides(newFilterId, state.FilterOverrides[filterName]);

                            if (state.FilterVisibility.ContainsKey(filterName))
                                targetView.SetFilterVisibility(newFilterId, state.FilterVisibility[filterName]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi apply filter vào view: {ex.Message}");
                }
            }
        }
    }
}