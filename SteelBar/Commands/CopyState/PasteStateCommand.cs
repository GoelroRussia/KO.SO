using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using SteelBar.Models.CopyState;
using SteelBar.ViewModels.CopyState;
using SteelBar.Views.CopyState;
using Nice3point.Revit.Toolkit.External;

namespace SteelBar.Commands.CopyState
{
    [Transaction(TransactionMode.Manual)]
    public class PasteStateCommand : ExternalCommand
    {
        public override void Execute()
        {
            if (!StateClipboard.HasData)
            {
                TaskDialog.Show("Error", "No state copied! Please run Copy State first.");
                return;
            }

            var doc = Application.ActiveUIDocument.Document;
            var uidoc = Application.ActiveUIDocument;

            // 1. Lấy danh sách ElementId đang bôi đen trên Revit
            var selectedIds = uidoc.Selection.GetElementIds();

            // 2. Khởi tạo MVVM
            PasteStateView view = null!;
            var viewModel = new PasteStateViewModel(doc, () => view?.Close(), selectedIds);

            view = new PasteStateView(viewModel);

            // 3. Hiển thị UI
            view.ShowDialog();
        }
    }
}