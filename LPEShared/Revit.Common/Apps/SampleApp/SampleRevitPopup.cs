using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Revit.Common
{
    [Transaction(TransactionMode.Manual)]
    public class SampleRevitPopup : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                TaskDialog.Show("FCA Sample Addin", "Looks like this worked!");
                return Result.Succeeded;
            }
            catch (Exception e)
            {
                TaskDialog.Show("FCA Sample Addin", $"Exception found: {e.Message}");
                return Result.Failed;
            }
        }
    }
}
