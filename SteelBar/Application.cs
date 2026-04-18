using Nice3point.Revit.Extensions;
using Nice3point.Revit.Extensions.UI;
using Nice3point.Revit.Toolkit.External;
using Serilog;
using Serilog.Events;
using SteelBar.Commands;
using SteelBar.Commands.CopyState;

namespace SteelBar
{
    /// <summary>
    ///     Application entry point
    /// </summary>
    [UsedImplicitly]
    public class Application : ExternalApplication
    {
        public override void OnStartup()
        {
            CreateLogger();
            CreateRibbon();
        }

        public override void OnShutdown()
        {
            Log.CloseAndFlush();
        }

        private void CreateRibbon()
        {

            var panel = Application.CreatePanel("REBAR", "KO.SO");
            var stackPanel = panel.AddStackPanel();
            stackPanel.AddPushButton<StartupCommand>("Check Round Rebar")
                .SetImage("/SteelBar;component/Resources/Icons/RibbonIcon16.png")
                .SetLargeImage("/SteelBar;component/Resources/Icons/RibbonIcon32.png");
            stackPanel.AddPushButton<PickNewHostCommand>("Pick New Host Assembly")
                .SetImage("/SteelBar;component/Resources/Icons/RibbonIcon16.png")
                .SetLargeImage("/SteelBar;component/Resources/Icons/RibbonIcon32.png");
            stackPanel.AddComboBox();


            var panel1 = Application.CreatePanel("Template", "KO.SO");
            var stackPanel1 = panel1.AddStackPanel();
            stackPanel1.AddPushButton<CopyStateCommand>("Copy View State")
                .SetImage("/SteelBar;component/Resources/Icons/CopyStateIcon16.png")
                .SetLargeImage("/SteelBar;component/Resources/Icons/RibbonIcon32.png");
            stackPanel1.AddPushButton<PasteStateCommand>("Paste View State")
                .SetImage("/SteelBar;component/Resources/Icons/CopyStateIcon16.png")
                .SetLargeImage("/SteelBar;component/Resources/Icons/RibbonIcon32.png");
            stackPanel1.AddComboBox();
        }

        private static void CreateLogger()
        {
            const string outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Debug(LogEventLevel.Debug, outputTemplate)
                .MinimumLevel.Debug()
                .CreateLogger();

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                var exception = (Exception)args.ExceptionObject;
                Log.Fatal(exception, "Domain unhandled exception");
            };
        }
    }
}