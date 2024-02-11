#region Namespaces
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
using Microsoft.SqlServer.Server;

#endregion

namespace Revit.Common
{
    [Transaction(TransactionMode.Manual)]
    public class RestoreJoints : IExternalCommand
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
            Dictionary<string, DateTime> dateTimes = new Dictionary<string, DateTime>();

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
                TaskDialog.Show("ATENÇÃO!", $"Não foi possível executar o comando por não existir no modelo os seguintes parâmetros:\n {error}");
                return Result.Cancelled;
            }


            //try
            //{

            List<string> jointAmbientes = new FilteredElementCollector(doc, uidoc.ActiveView.Id)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .GroupBy(a => a.LookupParameter("Ambiente").AsString())
                .Select(a => a.First().LookupParameter("Ambiente").AsString())
                .ToList();

            List<string> checkedAmbientes = new List<string>();
            var window = new SelectAmbienteMVVM(uidoc, SelectAmbientMVVMExecuteCommand.RestoreJoints);
            //var window = new SelectAmbienteMVVM(ambientes, "RESTAURAR PISOS", System.Windows.Visibility.Visible);
            window.ShowDialog();
            if (!window.Execute)
            {
                return Result.Cancelled;
            }
            else
            {
                if (window.Select)
                {
                    foreach (var reference in uidoc.Selection.PickObjects(ObjectType.Element, new FloorSelectionFilter(), "Selecione os pisos que deseja dividir"))
                    {
                        checkedAmbientes.Add(doc.GetElement(reference).LookupParameter("Ambiente").AsString());
                    }
                }
                else
                {
                    foreach (string floor in window.AmbienteViewModels.Where(x => x.IsChecked).Select(x => x.Name))
                    {
                        checkedAmbientes.Add(floor);
                    }
                }
            }
            using (TransactionGroup tg = new TransactionGroup(doc, "Restaurar Pisos"))
            {
                tg.Start();

                foreach (var stringAmbiente in checkedAmbientes)
                {
                    #region Floors

                    //dateTimes.Add("inicio", DateTime.Now);

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
                        //dateTimes.Add("get curves", DateTime.Now);

                        curves = Utils.SplitCurvesByOtherCurves(curves, curves);
                        //dateTimes.Add("split curves", DateTime.Now);

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

                        List<Curve> curvesWithoutDuplicates = new List<Curve>();
                        for (int i = 0; i < curves.Count; i++)
                        {
                            if (!duplicateCurvesIndex.Contains(i))
                            {
                                curvesWithoutDuplicates.Add(curves[i]);
                            }
                        }

                        //dateTimes.Add("remove duplicates", DateTime.Now);


                        //goto End;

                        List<CurveLoop> curveLoops = Utils.OrderCurvesToCurveLoops(curvesWithoutDuplicates);
                        curveLoops.Reverse();
                        Dictionary<CurveLoop, Solid> curveLoopSolids = new Dictionary<CurveLoop, Solid>();
                        //goto End;
                        foreach (var curveLoop in curveLoops)
                        {
                            curveLoopSolids.Add(curveLoop, GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>() { curveLoop }, XYZ.BasisZ, 100));
                        }
                        Dictionary<CurveLoop, Solid> floorCurveLoops = new Dictionary<CurveLoop, Solid>();
                        Dictionary<CurveLoop, int> openingCurveLoops = new Dictionary<CurveLoop, int>();
                        //floorCurveLoops.Add(curveLoopSolids.Last().Key,curveLoopSolids.Last().Value);
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
                        //foreach (var curve in curveLoops.Last())
                        //{
                        //    curveArray.Append(curve);
                        //}
                        //dateTimes.Add("getloops", DateTime.Now);


                        //tx.Start("Restaurar Piso");
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
                            CurveArray curveArray = new CurveArray();
                            foreach (var curve in floorCurveLoops.ElementAt(i).Key)
                            {
                                curveArray.Append(curve);
                            }


                            ////2021/////
#if Revit2021
                                Floor newFloor = doc.Create.NewFloor(curveArray, fType, level, false);
#else
                            ////2022/////
                            Floor newFloor = Floor.Create(doc, new List<CurveLoop>() { Utils.ArrayToLoop(curveArray) }, fType.Id, level.Id);
                            newFloor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).Set(firstHeight);
#endif


                            newFloors.Add(i, newFloor);

                        }
                        tx.Commit();

                        //Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);

                        //SketchPlane sPlane = SketchPlane.Create(doc, plane);
                        //foreach (var curve in curves)
                        //{
                        //    doc.Create.NewModelCurve(Utils.GetCurveInPlane(curve, plane), sPlane);
                        //}
                        //tx.Commit();
                        //dateTimes.Add("criar piso", DateTime.Now);
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
                        //dateTimes.Add("openings", DateTime.Now);
                        //for (int i = 0; i < curveLoops.Count - 1; i++)
                        //{
                        //    if (curveLoops[i].Count() > 2)
                        //    {
                        //        curveArray = new CurveArray();
                        //        foreach (var curve in curveLoops[i])
                        //        {
                        //            curveArray.Append(curve);
                        //        }
                        //        doc.Create.NewOpening(newFloor, curveArray, true);
                        //    }
                        //}
                        tx.Commit();


                        //tx.Start();
                        foreach (Floor newFloor in newFloors.Select(a => a.Value))
                        {

                            //newFloor.SlabShapeEditor.ResetSlabShape();
                            //tx.Commit();
                            //tx.Start();

                            //tx.Commit();

                            foreach (Floor floor in pair.Value.Cast<Floor>())
                            {
                                Utils.SetPointsElevationsOfNewFloor(floor as Floor, newFloor, 0);
                                //Utils.SetPreviousPointsElevationsToNewFloor(floor as Floor, newFloor);
                            }
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
                            //newFloor.LookupParameter("Tela Superior").Set("");
                            //newFloor.LookupParameter("(s/n) Tela Superior").Set(0);
                            //newFloor.LookupParameter("(s/n) Espaçador Sup.").Set(0);
                            //newFloor.LookupParameter("Emenda - Tela Superior (\"0,xx\")").Set(0);
                            //newFloor.LookupParameter("H-Espaçador Superior (cm)").Set(0);
                            //newFloor.LookupParameter("Fator de Forma").Set(0);
                            //newFloor.LookupParameter("Ambiente").Set(stringAmbiente);
                            tx.Commit();

                        }
                        //tx.Commit();
                        //dateTimes.Add("Reset", DateTime.Now);

                        tx.Start("Delete Other Floors");
                        //ambienteFloors[0].get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).Set(-100);
                        List<ElementId> list = pair.Value.Select(a => a.Id).ToList();
                        //list.RemoveAt(0);
                        doc.Delete(list);
                        tx.Commit();
                        //dateTimes.Add("Delete", DateTime.Now);

                        //tx.Start("Restore Height");
                        //foreach (Floor newFloor in newFloors.Select(a => a.Value))
                        //{
                        //    newFloor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).Set(firstHeight);
                        //}
                        //tx.Commit();
                        //dateTimes.Add("newheight", DateTime.Now);
                        //End:;

                        ////    //cleanLoopCurves = cleanLoopCurves.Where(a => a.Length > 0.01).ToList();
                        //Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
                        //tx.Start("aaa");
                        //SketchPlane sPlane = SketchPlane.Create(doc, plane);
                        //foreach (var curve in curvesWithoutDuplicates)
                        //{
                        //    doc.Create.NewModelCurve(Utils.GetCurveInPlane(curve, plane), sPlane);
                        //}
                        ////    //for (int i = 0; i < curveLoops.Count; i++)
                        ////    //{
                        ////    //    foreach (var curve in curveLoops[i])
                        ////    //    {
                        ////    //        doc.Create.NewModelCurve(Utils.GetCurveInPlane(curve, plane), sPlane);
                        ////    //    }
                        ////    //}
                        //tx.Commit();
                    }



                    #endregion

                    #region Joints

                    Dictionary<string, List<Element>> GUIDGroupedJoints = new Dictionary<string, List<Element>>();
                    List<Element> joints = new FilteredElementCollector(doc)
                        .WhereElementIsNotElementType()
                        .OfCategory(BuiltInCategory.OST_StructuralFraming)
                        .Where(a => a.LookupParameter("Ambiente").AsString() == stringAmbiente)
                        .ToList();

                    foreach (Element joint in joints)
                    {
                        string guid = joint.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString();
                        if (string.IsNullOrEmpty(guid))
                        {
                            continue;
                        }
                        if (GUIDGroupedJoints.ContainsKey(guid))
                        {
                            GUIDGroupedJoints[guid].Add(joint);
                        }
                        else
                        {
                            GUIDGroupedJoints.Add(guid, new List<Element>() { joint });
                        }
                    }

                    tx.Start();
                    foreach (string guid in GUIDGroupedJoints.Keys)
                    {
                        Curve curve = Utils.JoinCurves(GUIDGroupedJoints[guid].Select(a => (a.Location as LocationCurve).Curve).ToList());
                        try
                        {
                            FamilyInstance newJoint = doc.Create.NewFamilyInstance(curve, (GUIDGroupedJoints[guid][0] as FamilyInstance).Symbol, doc.GetElement((GUIDGroupedJoints[guid][0] as FamilyInstance).LevelId) as Level, Autodesk.Revit.DB.Structure.StructuralType.Beam);

                        }
                        catch (Exception)
                        {

                        }
                        //newJoint.LookupParameter("Ambiente").Set(stringAmbiente);
                        doc.Delete(GUIDGroupedJoints[guid].Select(a => a.Id).ToList());
                    }
                    tx.Commit();


                    #endregion
                }

                tg.Assimilate();
                //tg.Assimilate();
            }


            //dateTimes.Add("fim", DateTime.Now);
            //string log = "";
            //log += "0" + dateTimes.ElementAt(0).Key + ": " + dateTimes.ElementAt(0).Value.ToLongTimeString() + "\n";
            //for (int i = 1; i < dateTimes.Count; i++)
            //{
            //    log += dateTimes.ElementAt(i).Value.Subtract(dateTimes.ElementAt(i - 1).Value).ToString() + " - " + dateTimes.ElementAt(i).Key + ": " + dateTimes.ElementAt(i).Value.ToLongTimeString() + "\n";
            //}
            //log += "\n\n";
            //foreach (var item in arrayTimeSpans)
            //{
            //    log += item.Value.ToString() + " - " + item.Key + "\n";
            //}
            //TaskDialog.Show("Tempo", log);

            return Result.Succeeded;

            //}
            //catch (Exception e)
            //{
            //    TaskDialog.Show("ERRO", $"Houve um erro não mapeado na execução do plug-in, contate os desenvolvedores.\n\n{e.Message}");
            //    return Result.Cancelled;
            //}
        }
    }
}
