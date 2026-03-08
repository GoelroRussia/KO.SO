using Autodesk.Revit.DB;

namespace SteelBar.Models;

public class RebarInfo
{
    public int ElementId { get; set; }
    public string AssemblyName { get; set; }
    public string ParameterName { get; set; }
    public double Value { get; set; }
}