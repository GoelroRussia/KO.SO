using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External;
using SteelBar.Models.CopyState;
using SteelBar.Utils.CopyState;

namespace SteelBar.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class PasteStateCommand : ExternalCommand
    {
        public override void Execute()
        {
            if (!StateClipboard.HasData)
            {
                TaskDialog.Show("Error", "No state copied!");
                return;
            }
            using var t = new Transaction(Context.ActiveDocument, "Paste View State");
            t.Start();

            try
            {
                var service = new ViewStateService();
                service.ApplyState(Context.ActiveView, StateClipboard.CopiedState);

                t.Commit();
            }
            catch (Exception ex)
            {
                t.RollBack();
                TaskDialog.Show("Error", "Failed to paste state: " + ex.Message);
            }
        }
    }
}