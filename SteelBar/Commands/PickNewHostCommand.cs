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
                Reference assemblyRef = UiDocument.Selection.PickObject(
                    ObjectType.Element,
                    new AssemblySelectionFilter(),
                    "Vui lòng chọn 1 Assembly chứa cốt thép");

                AssemblyInstance assembly = Document.GetElement(assemblyRef) as AssemblyInstance;

                // Lấy toàn bộ thép trong Assembly
                var rebars = RebarHelper.GetRebarsInAssembly(Document, assembly);

                if (rebars.Count == 0)
                {
                    TaskDialog.Show("Thông báo", "Assembly được chọn không chứa thanh thép nào!");
                    return;
                }

                // B2. Yêu cầu người dùng Pick Host mới (Dầm, Cột, Sàn...)
                Reference hostRef = UiDocument.Selection.PickObject(
                    ObjectType.Element,
                    new RebarHostSelectionFilter(),
                    $"Chọn đối tượng Host mới cho {rebars.Count} thanh thép");

                Element newHost = Document.GetElement(hostRef);

                // Thực thi đổi Host
                RebarHelper.ChangeRebarsHost(Document, rebars, newHost);

                TaskDialog.Show("Thành công", $"Đã gán thành công {rebars.Count} thanh thép vào host mới: {newHost.Name}");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // Bắt lỗi khi người dùng nhấn ESC để thoát lệnh PickObject một cách êm đẹp
            }
        }
    }
}