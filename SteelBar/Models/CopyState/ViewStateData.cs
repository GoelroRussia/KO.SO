using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace SteelBar.Models.CopyState
{
    public class ViewStateData
    {
        // 1. Visibility Graphics (Dùng BuiltInCategory thay vì ElementId)
        public Dictionary<BuiltInCategory, OverrideGraphicSettings> CategoryOverrides { get; set; } = new();
        public Dictionary<BuiltInCategory, bool> CategoryVisibility { get; set; } = new();

        // 2. Filters (Lưu bằng Tên - string)
        public List<string> ActiveFilters { get; set; } = [];
        public Document SourceDocument { get; set; } = null!;
        public List<ElementId> SourceFilterIds { get; set; } = new();
        public Dictionary<string, OverrideGraphicSettings> FilterOverrides { get; set; } = new();
        public Dictionary<string, bool> FilterVisibility { get; set; } = new();

        // 3. Section Box (Giữ nguyên)
        public bool IsSectionBoxActive { get; set; }
        public BoundingBoxXYZ SectionBox { get; set; } = null!;

        // 4. Crop Region (Giữ nguyên)
        public bool CropBoxActive { get; set; }
        public bool CropBoxVisible { get; set; }
        public BoundingBoxXYZ? CropBox { get; set; }

        public bool HasVisGraphics { get; set; }
        public bool HasSectionBox { get; set; }
        public bool HasCropRegion { get; set; }
        public bool HasFilters { get; set; }
    }
}