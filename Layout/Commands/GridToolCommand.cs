using Autodesk.Revit.Attributes;
using Layout.ViewModels;
using Layout.Views;
using Layout.Utils;
using Nice3point.Revit.Toolkit.External;
namespace Layout.Commands
{
    /// <summary>   
    ///     External command entry point
    /// </summary>
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class GridToolCommand : ExternalCommand
    {

        public override void Execute()
        {
            var gridService = new GridService();
            var viewModel = new GridViewModel(UiApplication, gridService);
            var view = new GridToolView(viewModel);
            view.ShowDialog();
        }
    }
}