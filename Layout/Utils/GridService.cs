using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace Layout.Utils
{ 
    public class GridService
    {
        /// <summary>
        /// Chuyển đổi trạng thái Grid Extent (2D hoặc 3D) cho tất cả Grid trong View hiện tại
        /// </summary>
        public void ToggleGridsExtent(Document doc, View view, bool is3D)
        {
            // Lấy tất cả Grids hiển thị trong View hiện tại
            var grids = new FilteredElementCollector(doc, view.Id)
                .OfClass(typeof(Grid))
                .WhereElementIsNotElementType()
                .Cast<Grid>()
                .ToList();

            if (!grids.Any()) return;

            // Chuyển đổi: True = 3D (Model), False = 2D (ViewSpecific)
            var extentType = is3D ? DatumExtentType.Model : DatumExtentType.ViewSpecific;

            using (Transaction t = new Transaction(doc, "Toggle Grids 2D/3D"))
            {
                t.Start();
                foreach (var grid in grids)
                {
                    // Thay đổi cả hai đầu của Grid (End0 và End1)
                    grid.SetDatumExtentType(DatumEnds.End0, view, extentType);
                    grid.SetDatumExtentType(DatumEnds.End1, view, extentType);
                }
                t.Commit();
            }
        }
    }
}