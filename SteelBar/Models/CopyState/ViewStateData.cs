namespace SteelBar.Models.CopyState
{
    public class ViewStateData
    {
        // 1. Visibility Graphics (Categories)
        public Dictionary<ElementId, OverrideGraphicSettings> CategoryOverrides { get; set; } = new();
        public Dictionary<ElementId, bool> CategoryVisibility { get; set; } = new();

        // 2. Filters
        public List<ElementId> ActiveFilters { get; set; } = [];
        public Dictionary<ElementId, OverrideGraphicSettings> FilterOverrides { get; set; } = new();
        public Dictionary<ElementId, bool> FilterVisibility { get; set; } = new();

        // 3. Section Box (Only for 3D)
        public bool IsSectionBoxActive { get; set; }
        public BoundingBoxXYZ SectionBox { get; set; } = null!;

        // 4. Crop Region
        public bool CropBoxActive { get; set; }
        public bool CropBoxVisible { get; set; }
        public BoundingBoxXYZ? CropBox { get; set; }

        // Flags to know what data was captured
        public bool HasVisGraphics { get; set; }
        public bool HasSectionBox { get; set; }
        public bool HasCropRegion { get; set; }
        public bool HasFilters { get; set; }
    }
}