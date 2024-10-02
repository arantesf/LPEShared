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
using Revit.Common.Classes;
using Application = Autodesk.Revit.ApplicationServices.Application;
using System.Windows.Forms;

namespace Revit.Common
{
    [Transaction(TransactionMode.Manual)]
    public class TestCommand : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = commandData.Application.ActiveUIDocument.Document;

            Wall WallElem = (Wall)(doc.GetElement(new ElementId((2448))));
            CompoundStructure WallCompoundSt = WallElem.WallType.GetCompoundStructure();
            IList<int> ints = WallCompoundSt.GetRegionIds();
            IList<CompoundStructureLayer> layers = WallCompoundSt.GetLayers();
            Dictionary<CompoundStructureLayer, IList<int>> layersRegionsDict = new Dictionary<CompoundStructureLayer, IList<int>>();
            for (int i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];
                layersRegionsDict.Add(layer, WallCompoundSt.GetRegionsAssociatedToLayer(i));
            }

            string regionsMaxAndMin = "";
            double minU = 0;
            double minV = 0;
            for (int i = 0; i < ints.Count; i++)
            {
                int RegionId = ints[i];
                BoundingBoxUV BBUv = WallCompoundSt.GetRegionEnvelope(RegionId);
                if (i == 0)
                {
                    minU = BBUv.Min.U;
                    minV = BBUv.Min.V;
                }
                else
                {
                    if (BBUv.Min.U < minU) minU = BBUv.Min.U;
                    if (BBUv.Min.V < minV) minV = BBUv.Min.V;
                }
            }
            foreach (int RegionId in ints)
            {
                string material = "";
                foreach (var kvp in layersRegionsDict)
                {
                    if (kvp.Value.Contains(RegionId))
                    {
                        material = doc.GetElement(kvp.Key.MaterialId).Name;
                    }
                }
                BoundingBoxUV BBUv = WallCompoundSt.GetRegionEnvelope(RegionId);
                regionsMaxAndMin += $"Min: ({(BBUv.Min.U - minU)*304.8}, {(BBUv.Min.V - minV)*304.8}) | Max: ({(BBUv.Max.U - minU) * 304.8}, {(BBUv.Max.V - minV) * 304.8}) | Material: {material}\n";
            }

            MessageBox.Show(regionsMaxAndMin, "compoundStructure");

            return Result.Succeeded;
        }
    }
}
