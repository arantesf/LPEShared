using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;

namespace Revit.Common.Classes
{
    public static class GlobalVariables
    {
        public static Document Document { get; set; }
        public static Dictionary<MaterialClass, List<string>> materialsByClass { get; set; }
    }
}
