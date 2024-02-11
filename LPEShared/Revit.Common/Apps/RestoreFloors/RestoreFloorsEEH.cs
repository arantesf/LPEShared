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
using System.Windows;
using System.Windows.Controls;

namespace Revit.Common
{
    [Transaction(TransactionMode.Manual)]
    public class RestoreFloorsEEH : IExternalEventHandler
    {
        public void Execute(UIApplication uiapp)
        {
            try
            {
                SelectAmbienteMVVM.MainView.Ambientes_ListBox.IsEnabled = false;
                SelectAmbienteMVVM.MainView.SelectAll_CheckBox.IsEnabled = false;
                SelectAmbienteMVVM.MainView.SelectPisos_Button.IsEnabled = false;
                SelectAmbienteMVVM.MainView.Execute_Button.IsEnabled = false;

                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;

                List<string> checkedAmbientesNames = SelectAmbienteMVVM.MainView.AmbienteViewModels.Where(x => x.IsChecked).Select(x => x.Name).ToList();
                //List<ElementId> selectedFloorsIds = SelectAmbienteMVVM.MainView.SelectedFloorsIds;
                //if (selectedFloorsIds.Any())
                //{
                //    foreach (var id in selectedFloorsIds)
                //    {
                //        checkedAmbientesNames.Add(doc.GetElement(id).LookupParameter("Ambiente").AsString());
                //    }
                //}
                checkedAmbientesNames = checkedAmbientesNames.Distinct().ToList();

                using (TransactionGroup tg = new TransactionGroup(doc, "Restaurar Pisos"))
                {
                    tg.Start();

                    foreach (var stringAmbiente in checkedAmbientesNames)
                    {
                        List<Element> ambienteFloors = new FilteredElementCollector(doc)
                            .WhereElementIsNotElementType()
                            .OfCategory(BuiltInCategory.OST_Floors)
                            .Where(a => a.LookupParameter("Ambiente").AsString() == stringAmbiente)
                            .Where(a => a.LookupParameter("Reforço de Tela").AsInteger() == 0)
                            .ToList();

                        Dictionary<XYZ, List<Element>> sameSlopeFloors = new Dictionary<XYZ, List<Element>>();
                        for (int i = 0; i < ambienteFloors.Count; i++)
                        {
                            Face face = ambienteFloors[i].GetGeometryObjectFromReference(HostObjectUtils.GetTopFaces(ambienteFloors[i] as HostObject)[0]) as Face;
                            XYZ normal = face.ComputeNormal(new UV(0.5, 0.5));
                            if (i == 0)
                            {
                                sameSlopeFloors.Add(normal, new List<Element>() { ambienteFloors[i] });
                            }
                            else
                            {
                                bool found = false;
                                for (int j = 0; j < sameSlopeFloors.Count; j++)
                                {
                                    if (sameSlopeFloors.ElementAt(j).Key.IsAlmostEqualTo(normal))
                                    {
                                        sameSlopeFloors[sameSlopeFloors.ElementAt(j).Key].Add(ambienteFloors[i]);
                                        found = true;
                                        break;
                                    }
                                }
                                if (!found)
                                {
                                    sameSlopeFloors.Add(normal, new List<Element>() { ambienteFloors[i] });
                                }
                            }
                        }

                        SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 0;
                        SelectAmbienteMVVM.MainView.ProgressBar.Maximum = 8 * sameSlopeFloors.Count;
                        ExternalApplication.LPEApp.SelectAmbienteMVVM.ProgressBar_TextBlock.Text = $"Restaurando pisos do ambiente \"{stringAmbiente}\"...";


                        Transaction tx = new Transaction(doc);
                        foreach (var pair in sameSlopeFloors)
                        {
                            List<Curve> curves = new List<Curve>();
                            ElementCategoryFilter linesFilter = new ElementCategoryFilter(BuiltInCategory.OST_SketchLines);
                            foreach (var floor in pair.Value)
                            {
                                foreach (var item in floor.GetDependentElements(linesFilter))
                                {
#if Revit2021 || Revit2022 || Revit2023
                                    int categoryId = doc.GetElement(item).Category.Id.IntegerValue;
#else
                                    int categoryId = ((int)doc.GetElement(item).Category.Id.Value);
#endif
                                    if (categoryId == (int)BuiltInCategory.OST_SketchLines && !doc.GetElement(item).Name.Contains("Span ") && !doc.GetElement(item).Name.Contains("direção "))
                                    {
                                        Curve curve = null;
                                        if (doc.GetElement(item) is ModelArc)
                                        {
                                            if (!(doc.GetElement(item) as ModelArc).GeometryCurve.IsBound)
                                            {
                                                curves.AddRange(Utils.GetUnBoundArcHalfProjections((doc.GetElement(item) as ModelArc).GeometryCurve));
                                                continue;
                                            }
                                            curve = (doc.GetElement(item) as ModelArc).GeometryCurve;

                                        }
                                        else
                                        {
                                            curve = (doc.GetElement(item) as ModelLine).GeometryCurve;
                                        }

                                        curves.Add(Utils.GetCurveProjection(curve));
                                    }
                                }
                            }
                            SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue += 1;

                            curves = Utils.SplitCurvesByOtherCurves(curves, curves);

                            SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue += 1;

                            List<int> duplicateCurvesIndex = new List<int>();
                            for (int i = 0; i < curves.Count; i++)
                            {
                                Curve ci = curves[i];
                                XYZ epi0 = ci.GetEndPoint(0);
                                XYZ epi1 = ci.GetEndPoint(1);
                                XYZ mpi = ci.Evaluate(0.5, true);


                                for (int j = 0; j < curves.Count; j++)
                                {
                                    if (i != j)
                                    {
                                        Curve cj = curves[j];
                                        XYZ epj0 = cj.GetEndPoint(0);
                                        XYZ epj1 = cj.GetEndPoint(1);
                                        XYZ mpj = cj.Evaluate(0.5, true);

                                        if ((mpi.DistanceTo(mpj) < 0.0001) && ((epi0.DistanceTo(epj0) < 0.0001 && epi1.DistanceTo(epj1) < 0.0001) || epi0.DistanceTo(epj1) < 0.0001 && epi1.DistanceTo(epj0) < 0.0001))
                                        {
                                            duplicateCurvesIndex.Add(i);
                                            duplicateCurvesIndex.Add(j);
                                        }
                                    }
                                }
                            }
                            SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue += 1;

                            List<Curve> curvesWithoutDuplicates = new List<Curve>();
                            for (int i = 0; i < curves.Count; i++)
                            {
                                if (!duplicateCurvesIndex.Contains(i))
                                {
                                    curvesWithoutDuplicates.Add(curves[i]);
                                }
                            }


                            List<CurveLoop> curveLoops = Utils.OrderCurvesToCurveLoops(curvesWithoutDuplicates);
                            curveLoops.Reverse();
                            Dictionary<CurveLoop, Solid> curveLoopSolids = new Dictionary<CurveLoop, Solid>();
                            foreach (var curveLoop in curveLoops)
                            {
                                curveLoopSolids.Add(curveLoop, GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>() { curveLoop }, XYZ.BasisZ, 100));
                            }
                            Dictionary<CurveLoop, Solid> floorCurveLoops = new Dictionary<CurveLoop, Solid>();
                            Dictionary<CurveLoop, int> openingCurveLoops = new Dictionary<CurveLoop, int>();
                            for (int i = 0; i < curveLoopSolids.Count; i++)
                            {
                                bool opening = false;
                                for (int j = 0; j < floorCurveLoops.Count; j++)
                                {
                                    if (curveLoopSolids.ElementAt(i).Value.Volume != floorCurveLoops.ElementAt(j).Value.Volume)
                                    {
                                        double intersectionVolume = BooleanOperationsUtils.ExecuteBooleanOperation(curveLoopSolids.ElementAt(i).Value, floorCurveLoops.ElementAt(j).Value, BooleanOperationsType.Intersect).Volume;
                                        if (intersectionVolume == curveLoopSolids.ElementAt(i).Value.Volume)
                                        {
                                            openingCurveLoops.Add(curveLoopSolids.ElementAt(i).Key, j);
                                            opening = true;
                                            continue;
                                        }
                                    }
                                }
                                if (!opening)
                                {
                                    floorCurveLoops.Add(curveLoopSolids.ElementAt(i).Key, curveLoopSolids.ElementAt(i).Value);
                                }
                            }
                            SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue += 1;


                            FailureHandlingOptions failOpt = tx.GetFailureHandlingOptions();
                            failOpt.SetFailuresPreprocessor(new Utils.FloorWarningsSwallower());
                            tx.SetFailureHandlingOptions(failOpt);

                            Dictionary<int, Floor> newFloors = new Dictionary<int, Floor>();


                            Level level = doc.GetElement((pair.Value[0] as Floor).LevelId) as Level;
                            double firstHeight = pair.Value[0].get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).AsDouble();
                            FloorType fType = (pair.Value[0] as Floor).FloorType;

                            tx.Start("Delete Other Floors");


                            for (int i = 0; i < floorCurveLoops.Count; i++)
                            {
                                CurveLoop joinedCurveLoop = Utils.JoinColinearCurves(doc, floorCurveLoops.ElementAt(i).Key, out bool _);
                                CurveArray curveArray = new CurveArray();
                                foreach (var curve in joinedCurveLoop)
                                {
                                    curveArray.Append(curve);
                                }


#if Revit2021
                                Floor newFloor = doc.Create.NewFloor(curveArray, fType, level, false);
#else
                                Floor newFloor = Floor.Create(doc, new List<CurveLoop>() { Utils.ArrayToLoop(curveArray) }, fType.Id, level.Id);
                                newFloor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).Set(firstHeight);
#endif


                                newFloors.Add(i, newFloor);

                            }
                            tx.Commit();
                            SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue += 1;

                            tx.Start("Recriar Openings");

                            foreach (var curveLoop in openingCurveLoops)
                            {
                                CurveArray curveArray = new CurveArray();
                                foreach (var curve in curveLoop.Key)
                                {
                                    curveArray.Append(curve);
                                }
                                doc.Create.NewOpening(newFloors.ElementAt(curveLoop.Value).Value, curveArray, false);
                            }
                            tx.Commit();

                            SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue += 1;

                            foreach (Floor newFloor in newFloors.Select(a => a.Value))
                            {
                                Utils.SetPointsElevationsOfNewFloor(pair.Value.Cast<Floor>().ToList(), newFloor, 0);
                                //foreach (Floor floor in pair.Value.Cast<Floor>())
                                //{
                                //    Utils.SetPointsElevationsOfNewFloor(floor, newFloor, 0);
                                //}
                                tx.Start();
                                foreach (Floor floor1 in pair.Value.Cast<Floor>())
                                {
                                    foreach (Solid item in floor1.get_Geometry(new Options()).Cast<Solid>())
                                    {
                                        foreach (Face face in item.Faces)
                                        {
                                            double angle = face.ComputeNormal(new UV(0, 0)).AngleTo(XYZ.BasisZ);
                                            if (angle < Math.PI / 4 && angle > 0.001)
                                            {
                                                goto dontReset;
                                            }
                                        }
                                    }
                                }
#if Revit2021 || Revit2022 || Revit2023
                                newFloor.SlabShapeEditor.ResetSlabShape();
#else
                                newFloor.GetSlabShapeEditor().ResetSlabShape();
#endif
                            dontReset:
                                Utils.CopyAllParametersWithoutTransaction(pair.Value[0], newFloor, new List<BuiltInParameter>() { BuiltInParameter.ALL_MODEL_MARK, BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM });
                                newFloor.LookupParameter("Piso em placas").Set(0);
                                tx.Commit();

                            }
                            SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue += 1;

                            tx.Start("Delete Other Floors");
                            List<ElementId> list = pair.Value.Select(a => a.Id).ToList();
                            doc.Delete(list);
                            tx.Commit();
                            SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue += 1;

                        }
                    }

                    tg.Assimilate();
                    SelectAmbienteMVVM.MainView.Dispose();
                }
            }
            catch (Exception ex)
            {
                SelectAmbienteMVVM.MainView.Dispose();
                TaskDialog.Show("ATENÇÃO!", "Erro não mapeado, contate os desenvolvedores.\n\n" + ex.StackTrace);
                throw;
            }
        }

        public string GetName()
        {
            return this.GetType().Name;
        }
    }


}
