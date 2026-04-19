using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Nice3point.Revit.Toolkit.External;
using SteelBar.Utils;

namespace SteelBar.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class PickNewHostCommand : ExternalCommand
    {
        public override void Execute()
        {
            try
            {
                // B1. Yêu cầu người dùng Pick vào 1 Assembly
                Reference assemblyRef = Application.ActiveUIDocument.Selection.PickObject(
                    ObjectType.Element,
                    new AssemblySelectionFilter(),
                    "Vui lòng chọn 1 Assembly chứa cốt thép");

                AssemblyInstance? assembly = Application.ActiveUIDocument.Document.GetElement(assemblyRef) as AssemblyInstance;

                // Lấy toàn bộ thép trong Assembly
                List<Rebar> rebars = RebarHelper.GetRebarsInAssembly(Application.ActiveUIDocument.Document, assembly);

                if (rebars.Count == 0)
                {
                    TaskDialog.Show("Thông báo", "Assembly được chọn không chứa thanh thép nào!");
                    return;
                }

                // B2. Yêu cầu người dùng Pick Host mới (Dầm, Cột, Sàn...)
                Reference hostRef = Application.ActiveUIDocument.Selection.PickObject(
                    ObjectType.Element,
                    new RebarHostSelectionFilter(),
                    $"Chọn đối tượng Host mới cho {rebars.Count} thanh thép");

                Element newHost = Application.ActiveUIDocument.Document.GetElement(hostRef);

                // Thực thi đổi Host
                RebarHelper.ChangeRebarsHost(Application.ActiveUIDocument.Document, rebars, newHost);

                TaskDialog.Show("Thành công", $"Đã gán thành công {rebars.Count} thanh thép vào host mới: {newHost.Name}");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // Bắt lỗi khi người dùng nhấn ESC để thoát lệnh PickObject một cách êm đẹp
            }
        }
    }
}