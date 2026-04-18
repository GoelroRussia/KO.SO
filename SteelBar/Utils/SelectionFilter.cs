using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI.Selection;

namespace SteelBar.Utils
{
    public class AssemblySelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem) => elem is AssemblyInstance;
        public bool AllowReference(Reference reference, XYZ position) => false;
    }

    public class RebarHostSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem.Category == null) return false;

            // Kiểm tra xem Element có hỗ trợ làm Host cho thép hay không
            RebarHostData hostData = RebarHostData.GetRebarHostData(elem);
            return hostData != null && hostData.IsValidObject;
        }
        public bool AllowReference(Reference reference, XYZ position) => false;
    }
}