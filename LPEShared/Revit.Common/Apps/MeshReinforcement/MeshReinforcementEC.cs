using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.DB.Events;
using System.Reflection;
using System.IO;
using System.Runtime;
using Autodesk.Revit.DB.Architecture;
using Microsoft.SqlServer.Server;
using System.Xml.Linq;
using System.Diagnostics.Eventing.Reader;

namespace Revit.Common
{
    [Transaction(TransactionMode.Manual)]
    public class MeshReinforcementEC : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            if (uidoc.ActiveView is View3D)
            {
                TaskDialog.Show("Aten��o!", "Abra uma vista de planta para rodar o plug-in!");
                return Result.Cancelled;
            }

            List<DimensionType> cotaLPETypeList = new FilteredElementCollector(doc)
                .OfClass(typeof(DimensionType))
                .Where(a => a.Name == "Cota_LPE")
                .Cast<DimensionType>()
                .ToList();

            List<Element> tagOKList = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_MultiCategoryTags)
                .Where(a => a.Name == "Fator de Forma - Ok!")
                .ToList();

            List<Element> tagNaoList = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_MultiCategoryTags)
                .Where(a => a.Name == "Fator de Forma - N�o!")
                .ToList();

            bool OK = true;
            string error = "";
            if (!Utils.VerifyIfProjectParameterExists(doc, "Ambiente"))
            {
                error += "\n- Ambiente";
                OK = false;
            }
            if (!Utils.VerifyIfProjectParameterExists(doc, "Piso em placas"))
            {
                error += "\n- Piso em placas";
                OK = false;
            }
            if (!OK)
            {
                error = $"N�o foi poss�vel executar o comando por n�o existir no modelo os seguintes par�metros:\n {error}";
            }

            bool OKfamily = true;
            string errorFamily = "";
            if (!cotaLPETypeList.Any())
            {
                errorFamily += "\n- (Dimension) Cota_LPE";
                OKfamily = false;
            }
            if (!tagOKList.Any())
            {
                errorFamily += "\n- (Tag) Fator de Forma - OK!";
                OKfamily = false;
            }
            if (!tagNaoList.Any())
            {
                errorFamily += "\n- (Tag) Fator de Forma - N�o!";
                OKfamily = false;
            }
            if (!OKfamily)
            {
                errorFamily = $"N�o foi poss�vel executar o comando por n�o existir no modelo as seguites fam�lias:\n {errorFamily}";
            }

            string errorFinal = "";
            if (!OK)
            {
                errorFinal = error;
            }
            if (!OKfamily)
            {
                errorFinal += $"\n\n {errorFamily}";
            }
            if (!OK || !OKfamily)
            {
                TaskDialog.Show("ATEN��O!", errorFinal);
                return Result.Cancelled;
            }

            ExternalApplication.LPEApp.uiApp = uiapp;
            ExternalApplication.LPEApp.ShowMeshReinforcementUI();

            return Result.Succeeded;
        }
    }
}
