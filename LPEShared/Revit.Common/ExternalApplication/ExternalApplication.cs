using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.IO;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit.Resources;

namespace Revit.Common
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ExternalApplication : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            AddRibbonButtons(application);

            return Result.Succeeded;
        }

        private void AddRibbonButtons(UIControlledApplication application)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string executingAssemblyPath = assembly.Location;
            Debug.Print(executingAssemblyPath);
            string executingAssemblyName = assembly.GetName().Name;
            Console.WriteLine(executingAssemblyName);
            string tabName = "LPE";

            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                // tab already exists
            }

            PushButtonData pbd = new PushButtonData("DimensionFloors", "Cotar Pisos", executingAssemblyPath, typeof(DimensionFloors).FullName);
            RibbonPanel panel = application.CreateRibbonPanel(tabName, "Pisos e Juntas");

            // Create the main button.
            PushButton pb = panel.AddItem(pbd) as PushButton;

            pb.ToolTip = "Cota os pisos divididos em suas duas direções principais.";
            pb.LongDescription = "O comando utilizará o comando Aligned Dimension para criar 2 cotas em cada piso do modelo.";
            pb.LargeImage = ResourceImage.GetIcon("CotarPisosLPE.png");
        }
    }
}