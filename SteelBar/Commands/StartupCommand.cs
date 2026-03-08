using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External;
using SteelBar.ViewModels;
using SteelBar.Views;

namespace SteelBar.Commands;

[Transaction(TransactionMode.Manual)]
public class StartupCommand : ExternalCommand
{
    public override void Execute()
    {
        var viewModel = new RebarCheckerViewModel(UiDocument);

        var view = new RebarCheckerView(viewModel);
        System.Windows.Interop.WindowInteropHelper helper = new(view)
        {
            Owner = UiApplication.MainWindowHandle
        };
        view.Show();
    }
}