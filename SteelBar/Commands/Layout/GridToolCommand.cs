using Autodesk.Revit.Attributes;
using SteelBar.ViewModels.Layout;
using SteelBar.Views.Layout;
using SteelBar.Utils.Layout;
using Nice3point.Revit.Toolkit.External;
namespace SteelBar.Commands.Layout
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