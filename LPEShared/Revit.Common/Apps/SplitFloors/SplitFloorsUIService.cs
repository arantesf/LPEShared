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
using static Revit.Common.SelectAmbienteMVVM;

namespace Revit.Common
{
    [Transaction(TransactionMode.Manual)]
    public class SplitFloorsUIService
    {
        public static void SplitFloors(UIDocument uidoc)
        {
            Document doc = uidoc.Document;

            List<string> checkedAmbientesNames = SelectAmbienteMVVM.MainView.AmbienteViewModels.Where(x => x.IsChecked).Select(x => x.Name).ToList();
            SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 0;
            SelectAmbienteMVVM.MainView.ProgressBar.Maximum = 1;
            TransactionGroup transactionGroupMaster = new TransactionGroup(doc);
            transactionGroupMaster.Start("Dividir Pisos");
            foreach (var ambiente in checkedAmbientesNames)
            {
                List<Element> pisofloors = new FilteredElementCollector(doc, uidoc.ActiveView.Id)
                    .WhereElementIsNotElementType()
                    .OfCategory(BuiltInCategory.OST_Floors)
                    .Where(floor => floor.LookupParameter("Ambiente").HasValue && floor.LookupParameter("Piso em placas").AsInteger() == 0 && floor.LookupParameter("Reforço de Tela").AsInteger() == 0 && floor.LookupParameter("Ambiente").AsString() == ambiente)
                    .ToList();

                List<Element> openings = new FilteredElementCollector(doc, uidoc.ActiveView.Id)
                   .WhereElementIsNotElementType()
                   .OfCategory(BuiltInCategory.OST_FloorOpening)
                   .Where(opening => (opening as Opening).Host.LookupParameter("Ambiente").HasValue && (opening as Opening).Host.LookupParameter("Piso em placas").AsInteger() == 0 && (opening as Opening).Host.LookupParameter("Ambiente").AsString() == ambiente)
                   .ToList();

                List<Element> joints = new FilteredElementCollector(doc, uidoc.ActiveView.Id)
                    .WhereElementIsNotElementType()
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .Where(a => a.LookupParameter("Ambiente").HasValue && a.LookupParameter("Ambiente").AsString() == ambiente)
                    .ToList();

                List<Element> reforcoOrPlacafloors = new FilteredElementCollector(doc, uidoc.ActiveView.Id)
                    .WhereElementIsNotElementType()
                    .OfCategory(BuiltInCategory.OST_Floors)
                    .Where(floor => floor.LookupParameter("Ambiente").HasValue && floor.LookupParameter("Ambiente").AsString() == ambiente && (floor.LookupParameter("Reforço de Tela").AsInteger() == 1 || floor.LookupParameter("Piso em placas").AsInteger() == 1))
                    .ToList();

                CurveArray openingsCurveArray = new CurveArray();
                Solid openingsSolid = null;
                if (openings.Any())
                {
                    foreach (Opening opening in openings.Cast<Opening>())
                    {
                        CurveArray curveArray = opening.BoundaryCurves;
                        foreach (Curve curve in curveArray)
                        {
                            openingsCurveArray.Append(curve);
                        }
                        List<CurveLoop> openingCurveLoops = Utils.GetCurveLoopsByCurveArray(curveArray);
                        if (openingCurveLoops.Any())
                        {
                            Solid openingSolidDown = GeometryCreationUtilities.CreateExtrusionGeometry(openingCurveLoops, -XYZ.BasisZ, 1000);
                            if (openingsSolid == null)
                            {
                                openingsSolid = openingSolidDown;
                            }
                            else
                            {
                                openingsSolid = BooleanOperationsUtils.ExecuteBooleanOperation(openingsSolid, openingSolidDown, BooleanOperationsType.Union);
                            }
                        }
                    }
                }

                List<List<Element>> pisosAndReforços = new List<List<Element>>() { pisofloors, reforcoOrPlacafloors };

                


                SelectAmbienteMVVM.MainView.ProgressBar.Maximum = pisosAndReforços.Count;
                for (int i = 0; i < pisosAndReforços.Count; i++)
                {
                    Solid floorSolids = null;

                    List<Element> floors = pisosAndReforços[i];
                    if (!floors.Any())
                    {
                        continue;
                    }
                    foreach (Floor floor in floors.Cast<Floor>())
                    {
#if Revit2021
                        CurveArrArray floorArrays = new CurveArrArray() { };
                        CurveArray curveArrayy = new CurveArray();
                        foreach (var curve in floor.GetDependentElements(new ElementClassFilter(typeof(CurveElement))).Select(id => (doc.GetElement(id) as CurveElement).GeometryCurve))
                        {
                            curveArrayy.Append(curve);
                        }
                        floorArrays.Append(curveArrayy);
#else
                            CurveArrArray floorArrays = (doc.GetElement(floor.SketchId) as Sketch).Profile;
#endif

                        List<CurveLoop> curveLoops = new List<CurveLoop>();
                        foreach (CurveArray floorArray in floorArrays)
                        {
                            curveLoops.AddRange(Utils.GetCurveLoopsByCurveArray(floorArray));
                        }
                        Solid floorSolid = GeometryCreationUtilities.CreateExtrusionGeometry(curveLoops, -XYZ.BasisZ, 1000);

                        if (floorSolids == null)
                        {
                            floorSolids = floorSolid;
                        }
                        else
                        {
                            floorSolids = BooleanOperationsUtils.ExecuteBooleanOperation(floorSolids, floorSolid, BooleanOperationsType.Union);
                        }
                    }

                    List<ElementId> selectedFloors = SelectAmbienteMVVM.MainView.SelectedFloorsIds;
                    if (selectedFloors.Any())
                    {
                        floors = floors.Where(floor => selectedFloors.Contains(floor.Id)).ToList();
                    }





                    Solid finalSolid = null;
                    if (openingsSolid != null)
                    {
                        finalSolid = BooleanOperationsUtils.ExecuteBooleanOperation(floorSolids, openingsSolid, BooleanOperationsType.Difference);
                    }
                    else
                    {
                        finalSolid = floorSolids;
                    }



                    if (floors.Any() && joints.Any())
                    {
                        SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 0;
                        SelectAmbienteMVVM.MainView.ProgressBar.Maximum = 3;
                        ExternalApplication.LPEApp.SelectAmbienteMVVM.ProgressBar_TextBlock.Text = $"Preparando para dividir os pisos do ambiente \"{ambiente}\"";

                        CurveArray curveArrayToCreateRoomBoundaries = new CurveArray();
                        TransactionGroup transactionGroup = new TransactionGroup(doc);
                        transactionGroup.Start("transactionToRollBack");

                        //Delete All Rooms And RoomBoundaries
                        List<Element> roomsToDelete = new FilteredElementCollector(doc, uidoc.ActiveView.Id)
                            .WhereElementIsNotElementType()
                            .WherePasses(new ElementMulticategoryFilter(new List<BuiltInCategory>() { BuiltInCategory.OST_Rooms, BuiltInCategory.OST_RoomSeparationLines }))
                            .ToList();

                        Utils.DeleteElements(doc, roomsToDelete);

                        foreach (Floor floor in floors.Cast<Floor>())
                        {
#if Revit2021
                            CurveArrArray floorArrays = new CurveArrArray() { };
                            CurveArray curveArrayy = new CurveArray();
                            foreach (var curve in floor.GetDependentElements(new ElementClassFilter(typeof(CurveElement))).Select(id => (doc.GetElement(id) as CurveElement).GeometryCurve))
                            {
                                curveArrayy.Append(curve);
                            }
                            floorArrays.Append(curveArrayy);
#else
                            CurveArrArray floorArrays = (doc.GetElement(floor.SketchId) as Sketch).Profile;
#endif
                            foreach (CurveArray curveArray in floorArrays)
                            {
                                foreach (Curve curve in curveArray)
                                {
                                    curveArrayToCreateRoomBoundaries.Append(curve);
                                }
                            }
                        }

                        foreach (Curve curve in openingsCurveArray)
                        {
                            curveArrayToCreateRoomBoundaries.Append(curve);
                        }

                        //Utils.AddFloorTopFacesCurvesToCurveArray(doc, curveArrayToCreateRoomBoundaries, floors);
                        Utils.AddJointCurvesToCurveArray(doc, curveArrayToCreateRoomBoundaries, joints);

                        //Level level = doc.GetElement(floors[0].LevelId) as Level;
                        Level level = Utils.CreateLevel(doc, 10000);
                        View deletarView = new FilteredElementCollector(doc)
                            .WhereElementIsNotElementType()
                            .OfClass(typeof(ViewPlan))
                            .Where(view => view.Name == "Deletar")
                            .Cast<View>()
                            .FirstOrDefault();
                        Utils.DeleteElements(doc, new List<Element>() { deletarView });

                        deletarView = Utils.CreateTemporaryViewInLevel(doc, level);

                        SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 1;


                        CurveArray curveArrayToCreateRoomBoundariesScaled = new CurveArray();
                        Transform transform10 = Transform.CreateTranslation(new XYZ(0, 0, 0)).ScaleBasis(10);
                        Transform transform1 = Transform.CreateTranslation(new XYZ(0, 0, 0)).ScaleBasis(0.1);
                        foreach (Curve curve in curveArrayToCreateRoomBoundaries)
                        {
                            curveArrayToCreateRoomBoundariesScaled.Append(curve.CreateTransformed(transform10));
                        }
                        ModelCurveArray modelCurveArray = Utils.CreateRoomBoundaryLines(doc, level.Id, curveArrayToCreateRoomBoundariesScaled, deletarView);
                        List<Room> createdRooms = Utils.CreateRooms(doc, level);

                        //goto watchRooms;

                        SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 2;


                        List<CurveArray> dividedFloorsCurveArrays = new List<CurveArray>();
                        List<(List<CurveLoop> CurveLoops,Curve CurveToIntersect)> dividedFloorsCurveLoops = new List<(List<CurveLoop>, Curve)>();
                        SolidCurveIntersectionOptions solidCurveIntersectionOptions = new SolidCurveIntersectionOptions();
                        List<Solid> recreatedSolids = new List<Solid>();
                        foreach (Room createdRoom in createdRooms)
                        {
                            XYZ roomPoint = (createdRoom.Location as LocationPoint).Point;
                            XYZ roomPointTrasnformed = transform1.OfPoint(roomPoint);
                            Curve curveToIntersectSolid = Line.CreateBound(roomPointTrasnformed - XYZ.BasisZ * 10000, roomPointTrasnformed + XYZ.BasisZ * 10000);
                            if (finalSolid.IntersectWithCurve(curveToIntersectSolid, solidCurveIntersectionOptions).Any())
                            {
                                List<CurveLoop> curveLoops = new List<CurveLoop>();
                                CurveArray curveArray = new CurveArray();
                                SpatialElementBoundaryOptions opt = new SpatialElementBoundaryOptions();
                                //opt.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.;
                                //opt.StoreFreeBoundaryFaces = true;
                                IList<IList<BoundarySegment>> boundarySegmentArray = createdRoom.GetBoundarySegments(opt);
                                foreach (IList<BoundarySegment> list in boundarySegmentArray)
                                {
                                    CurveLoop cLoop = new CurveLoop();
                                    foreach (BoundarySegment boundarySegment in list)
                                    {
                                        Curve curve = boundarySegment.GetCurve().CreateTransformed(transform1);
                                        //if (curve.Length < app.ShortCurveTolerance)
                                        //{
                                        //    continue;
                                        //}
                                        curveArray.Append(curve);
                                        cLoop.Append(curve);
                                    }
                                    if (cLoop.IsOpen())
                                    {
                                        try
                                        {
                                            cLoop.Append(Line.CreateBound(cLoop.Last().GetEndPoint(1), cLoop.First().GetEndPoint(0)));
                                        }
                                        catch (Exception)
                                        {
                                            Line correctionLine = Line.CreateBound(cLoop.Last().GetEndPoint(0), cLoop.First().GetEndPoint(0));
                                            CurveLoop newCLoop = new CurveLoop();
                                            foreach (var item in cLoop.Take(cLoop.Count() - 1))
                                            {
                                                newCLoop.Append(item);
                                            }
                                            newCLoop.Append(correctionLine);
                                            cLoop = newCLoop;
                                        }
                                    }
                                    curveLoops.Add(cLoop);
                                }
                                dividedFloorsCurveArrays.Add(curveArray);
                                dividedFloorsCurveLoops.Add((curveLoops,curveToIntersectSolid));
                            }
                        }
                        //Utils.DeleteElements(doc, new List<Element>() { deletarView });
                        transactionGroup.RollBack();
                        transactionGroup.Start("Dividir Pisos");

                        SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 3;


                        SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 0;
                        SelectAmbienteMVVM.MainView.ProgressBar.Maximum = dividedFloorsCurveLoops.Count;
                        int count = 0;
                        foreach (var (CurveLoops, CurveToIntersect) in dividedFloorsCurveLoops)
                        {
                            SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue += 1;
                            count++;
                            ExternalApplication.LPEApp.SelectAmbienteMVVM.ProgressBar_TextBlock.Text = $"Criando pisos do ambiente \"{ambiente}\" ({count}/{dividedFloorsCurveLoops.Count})";

                            /*
                            try
                            {
                                Solid solidUp = GeometryCreationUtilities.CreateExtrusionGeometry(curveLoops, XYZ.BasisZ, 1000);
                                Solid solidDown = GeometryCreationUtilities.CreateExtrusionGeometry(curveLoops, -XYZ.BasisZ, 1000);

                                //Transaction tx = new Transaction(doc, "directShape");
                                //tx.Start();
                                //DirectShape directShape = DirectShape.CreateElement(doc, Category.GetCategory(doc, BuiltInCategory.OST_Walls).Id);
                                //directShape.AppendShape(new List<GeometryObject> { solidUp, solidDown });
                                //tx.Commit();

                                if (Utils.SolidIntersectSolid(openingsSolid, solidDown))
                                {
                                    continue;
                                }
                                Floor createdFloor = Utils.CreateFloor(doc, curveLoops, floors.First().GetTypeId(), floors.First().LevelId);
                                if (createdFloor == null)
                                {
                                    continue;
                                }
                                //bool matchedExistingFloor = false;
                                foreach (Floor existingFloor in floors)
                                {
                                    if (Utils.ElementIntersectSolid(existingFloor, solidUp) || Utils.ElementIntersectSolid(existingFloor, solidDown))
                                    {
                                        Utils.ChangeTypeId(createdFloor, existingFloor.GetTypeId());
                                        Utils.CopyAllParameters(existingFloor, createdFloor, new List<BuiltInParameter>() { BuiltInParameter.ALL_MODEL_MARK, BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM });
    #if Revit2021 || Revit2022 || Revit2023
                                    SlabShapeEditor slabShapeEditor = existingFloor.SlabShapeEditor;
    #else
                                        SlabShapeEditor slabShapeEditor = existingFloor.GetSlabShapeEditor();
    #endif
                                        if (slabShapeEditor == null || slabShapeEditor.IsEnabled)
                                        {
                                            Utils.SetPointsElevationsOfNewFloor(existingFloor as Floor, createdFloor, 0);
                                        }
                                        //matchedExistingFloor = true;
                                        break;
                                    }
                                }
                                //if (!matchedExistingFloor)
                                //{
                                //    Utils.DeleteElements(doc, new List<Element>() { createdFloor });
                                //    continue;
                                //}
                                Utils.SetParameter(createdFloor, "Piso em placas", 1);
                                foreach (Floor existingFloor in reforcoOrPlacafloors)
                                {
                                    if (Utils.ElementIntersectSolid(existingFloor, solidUp) || Utils.ElementIntersectSolid(existingFloor, solidDown))
                                    {
                                        floors.Add(createdFloor);
                                    }
                                }
                                if (curveLoops.Count() > 1)
                                {
                                    for (int i = 1; i < curveLoops.Count(); i++)
                                    {
                                        CurveArray openingCurveArray = Utils.GetCurveArrayByCurveLoop(curveLoops[i]);

                                        Opening op = Utils.CreateOpening(doc, createdFloor, openingCurveArray, false);
                                    }
                                }
                            }
                            catch (Exception)
                            {
                            }
                            */
                            List<CurveLoop> curveLoops = CurveLoops;
                            List<CurveLoop> orderedCurveLoops = curveLoops.OrderBy(curveLoop => curveLoop.GetExactLength()).ToList();
                            XYZ secondVectorInMultiplication = -XYZ.BasisZ;
                            if (curveLoops.First().IsCounterclockwise(XYZ.BasisZ))
                            {
                                secondVectorInMultiplication = XYZ.BasisZ;
                            }

                            //CurveLoop offsetCurveLoop = CurveLoop.CreateViaOffset(orderedCurveLoops.First(), -0.01, XYZ.BasisZ);
                            //XYZ pointInsideCurveLoop = offsetCurveLoop.First().GetEndPoint(0);
                            Line firstLine = (Line)curveLoops.First().FirstOrDefault(curve => curve is Line);
                            if (firstLine == null)
                            {
                                continue;
                            }
                            XYZ middlePoint = firstLine.Evaluate(0.5, true);
                            XYZ pointInsideCurveLoop = middlePoint + firstLine.Direction.CrossProduct(secondVectorInMultiplication) * 0.01;


                            List<CurveLoop> fixedSmallLinesCurveLoops = Utils.FixCurveLoopsSmallLines(orderedCurveLoops, 0.01);

                            //Curve curveToIntersectSolid = Line.CreateBound(pointInsideCurveLoop - XYZ.BasisZ * 10000, pointInsideCurveLoop + XYZ.BasisZ * 10000);
                            Curve curveToIntersectSolid = CurveToIntersect;
                            if (finalSolid.IntersectWithCurve(curveToIntersectSolid, solidCurveIntersectionOptions).Any())
                            {
                                if (openingsSolid != null && openingsSolid.IntersectWithCurve(curveToIntersectSolid, solidCurveIntersectionOptions).Any())
                                {
                                    continue;
                                }
                                Floor createdFloor = Utils.CreateFloor(doc, fixedSmallLinesCurveLoops, floors.First().GetTypeId(), floors.First().LevelId);
                                if (createdFloor == null)
                                {
                                    continue;
                                }
                                //bool matchedExistingFloor = false;
                                foreach (Floor existingFloor in floors.Cast<Floor>())
                                {
                                    Solid existingFloorSolid = Utils.GetElementSolid(existingFloor);
                                    if (existingFloorSolid.IntersectWithCurve(curveToIntersectSolid, solidCurveIntersectionOptions).Any())
                                    {
                                        Utils.ChangeTypeId(createdFloor, existingFloor.GetTypeId());
                                        Utils.CopyAllParameters(existingFloor, createdFloor, new List<BuiltInParameter>() { BuiltInParameter.ALL_MODEL_MARK });
#if Revit2021 || Revit2022 || Revit2023
                                SlabShapeEditor slabShapeEditor = existingFloor.SlabShapeEditor;
#else
                                        SlabShapeEditor slabShapeEditor = existingFloor.GetSlabShapeEditor();
#endif
                                        if (slabShapeEditor == null || slabShapeEditor.IsEnabled)
                                        {
                                            Utils.SetPointsElevationsOfNewFloor(existingFloor as Floor, createdFloor, 0);
                                        }
                                        //matchedExistingFloor = true;
                                        break;
                                    }
                                }
                                //if (!matchedExistingFloor)
                                //{
                                //    Utils.DeleteElements(doc, new List<Element>() { createdFloor });
                                //    continue;
                                //}
                                Utils.SetParameter(createdFloor, "Piso em placas", 1);
                                //foreach (Floor existingFloor in reforcoOrPlacafloors)
                                //{
                                //    Solid existingFloorSolid = Utils.GetElementSolid(existingFloor);
                                //    if (existingFloorSolid.IntersectWithCurve(curveToIntersectSolid, solidCurveIntersectionOptions).Any())
                                //    {
                                //        floors.Add(createdFloor);
                                //    }
                                //}
                                //if (fixedSmallLinesCurveLoops.Count() > 1)
                                //{
                                //    for (int j = 1; j < fixedSmallLinesCurveLoops.Count(); j++)
                                //    {
                                //        CurveArray openingCurveArray = Utils.GetCurveArrayByCurveLoop(curveLoops[i]);

                                //        Opening op = Utils.CreateOpening(doc, createdFloor, openingCurveArray, false);
                                //    }
                                //}
                                //uidoc.ShowElements(createdFloor);
                            }
                        }

                        Utils.DeleteElements(doc, floors);

                        //watchRooms:;
                        //uidoc.ActiveView = initialView;
                        transactionGroup.Assimilate();

                    }
                }
            }
            transactionGroupMaster.Assimilate();
            SelectAmbienteMVVM.MainView.Dispose();


            //return Result.Succeeded;
        }

        public static void SelectFloors(UIDocument uidoc)
        {

        }
    }
}
