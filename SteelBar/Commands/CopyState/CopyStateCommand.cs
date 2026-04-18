using Autodesk.Revit.Attributes;
using SteelBar.ViewModels.CopyState;
using SteelBar.Views.CopyState;
using Nice3point.Revit.Toolkit.External;
namespace SteelBar.Commands.CopyState
{
    /// <summary>   
    ///     External command entry point
    /// </summary>
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class CopyStateCommand : ExternalCommand
    {

        public override void Execute()
        {
            CopyStateView copyStateView = null!;
            var copyStateViewModel = new CopyStateViewModel(Context.ActiveView!, () => copyStateView?.Close());
            copyStateView = new CopyStateView(copyStateViewModel);
            copyStateView.ShowDialog();
        }
    }
}