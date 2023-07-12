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

namespace Revit.Common
{
    [Transaction(TransactionMode.Manual)]
    public class DividirPisosByRoom : IExternalCommand
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

            bool OK = true;
            string errors = "";
            if (!Utils.VerifyIfProjectParameterExists(doc, "Ambiente"))
            {
                errors += "\n- Ambiente";
                OK = false;
            }
            if (!Utils.VerifyIfProjectParameterExists(doc, "Piso em placas"))
            {
                errors += "\n- Piso em placas";
                OK = false;
            }
            if (!Utils.VerifyIfProjectParameterExists(doc, "Reforço de Tela"))
            {
                errors += "\n- Reforço de Tela";
                OK = false;
            }
            if (!OK)
            {
                TaskDialog.Show("ATENÇÃO!", $"Não foi possível executar o comando por não existir no modelo os seguintes parâmetros:\n {errors}");
                return Result.Cancelled;
            }

            List<string> ambientes = new FilteredElementCollector(doc, uidoc.ActiveView.Id)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_Floors)
                .Where(a => a.LookupParameter("Ambiente").HasValue && a.LookupParameter("Piso em placas").AsInteger() == 0 && a.LookupParameter("Reforço de Tela").AsInteger() == 0)
                .GroupBy(a => a.LookupParameter("Ambiente").AsString())
                .Select(a => a.First().LookupParameter("Ambiente").AsString())
                .ToList();

            // SELECT AMBIENTES TO DIVIDE

            List<string> checkedAmbientes = new List<string>();
            var window = new SelectAmbienteMVVM(ambientes, "Dividir Pisos", System.Windows.Visibility.Visible);
            List<ElementId> selectedFloors = new List<ElementId>();
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
                        selectedFloors.Add(doc.GetElement(reference).Id);
                        checkedAmbientes.Add(doc.GetElement(reference).LookupParameter("Ambiente").AsString());
                    }
                    checkedAmbientes = checkedAmbientes.Distinct().ToList();
                }
                else
                {
                    foreach (string floor in window.SelectedAmbientes)
                    {
                        checkedAmbientes.Add(floor);
                    }
                }
            }

            foreach (var ambiente in checkedAmbientes)
            {
                List<Element> floors = new FilteredElementCollector(doc, uidoc.ActiveView.Id)
                    .WhereElementIsNotElementType()
                    .OfCategory(BuiltInCategory.OST_Floors)
                    .Where(floor => floor.LookupParameter("Ambiente").HasValue && floor.LookupParameter("Piso em placas").AsInteger() == 0 && floor.LookupParameter("Reforço de Tela").AsInteger() == 0 && floor.LookupParameter("Ambiente").AsString() == ambiente)
                    .ToList();

                if (selectedFloors.Any())
                {
                    floors = floors.Where(floor => selectedFloors.Contains(floor.Id)).ToList();
                }

                List<Element> reforcoOrPlacafloors = new FilteredElementCollector(doc, uidoc.ActiveView.Id)
                    .WhereElementIsNotElementType()
                    .OfCategory(BuiltInCategory.OST_Floors)
                    .Where(floor => floor.LookupParameter("Ambiente").HasValue && floor.LookupParameter("Ambiente").AsString() == ambiente && (floor.LookupParameter("Reforço de Tela").AsInteger() == 1 || floor.LookupParameter("Piso em placas").AsInteger() == 1))
                    .ToList();

                List<Element> openings = new FilteredElementCollector(doc, uidoc.ActiveView.Id)
                   .WhereElementIsNotElementType()
                   .OfCategory(BuiltInCategory.OST_FloorOpening)
                   .Where(opening => (opening as Opening).Host.LookupParameter("Ambiente").HasValue && (opening as Opening).Host.LookupParameter("Piso em placas").AsInteger() == 0 && (opening as Opening).Host.LookupParameter("Ambiente").AsString() == ambiente)
                   .ToList();

                Solid openingsSolid = null;
                if (openings.Any())
                {
                    foreach (Opening opening in openings)
                    {
                        List<CurveLoop> openingCurveLoops = Utils.GetCurveLoopsByCurveArray(opening.BoundaryCurves);
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

                List<Element> joints = new FilteredElementCollector(doc, uidoc.ActiveView.Id)
                    .WhereElementIsNotElementType()
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .Where(a => a.LookupParameter("Ambiente").HasValue && a.LookupParameter("Ambiente").AsString() == ambiente)
                    .ToList();

                if (floors.Any() && joints.Any())
                {
                    CurveArray curveArrayToCreateRoomBoundaries = new CurveArray();
                    TransactionGroup transactionGroup = new TransactionGroup(doc);
                    transactionGroup.Start("transactionToRollBack");

                    //Delete All Rooms And RoomBoundaries
                    List<Element> roomsToDelete = new FilteredElementCollector(doc, uidoc.ActiveView.Id)
                        .WhereElementIsNotElementType()
                        .WherePasses(new ElementMulticategoryFilter(new List<BuiltInCategory>() { BuiltInCategory.OST_Rooms, BuiltInCategory.OST_RoomSeparationLines }))
                        .ToList();

                    Utils.DeleteElements(doc, roomsToDelete);

                    Utils.AddFloorTopFacesCurvesToCurveArray(doc, curveArrayToCreateRoomBoundaries, floors);
                    Utils.AddJointCurvesToCurveArray(doc, curveArrayToCreateRoomBoundaries, joints);

                    Level level = doc.GetElement(floors[0].LevelId) as Level;
                    View initialView = new FilteredElementCollector(doc)
                        .WhereElementIsNotElementType()
                        .OfClass(typeof(ViewPlan))
                        .Where(view => (view as View).GenLevel != null && (view as View).GenLevel.Id == level.Id)
                        .Cast<View>()
                        .FirstOrDefault();
                    if (initialView == null)
                    {
                        initialView = Utils.CreateTemporaryViewInLevel(doc, level);
                    }

                    ModelCurveArray modelCurveArray = Utils.CreateRoomBoundaryLines(doc, level.Id, curveArrayToCreateRoomBoundaries, initialView);
                    List<Room> createdRooms = Utils.CreateRooms(doc, level);

                    //goto watchRooms;

                    List<CurveArray> dividedFloorsCurveArrays = new List<CurveArray>();
                    foreach (Room createdRoom in createdRooms)
                    {
                        CurveArray curveArray = new CurveArray();
                        SpatialElementBoundaryOptions opt = new SpatialElementBoundaryOptions();
                        opt.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish;
                        opt.StoreFreeBoundaryFaces = true;
                        IList<IList<BoundarySegment>> boundarySegmentArray = createdRoom.GetBoundarySegments(opt);
                        foreach (IList<BoundarySegment> list in boundarySegmentArray)
                        {
                            foreach (BoundarySegment boundarySegment in list)
                            {
                                curveArray.Append(boundarySegment.GetCurve());
                            }
                        }
                        dividedFloorsCurveArrays.Add(curveArray);
                    }
                    transactionGroup.Assimilate();
                    transactionGroup.Start("Dividir Pisos");

                    foreach (var curveArray in dividedFloorsCurveArrays)
                    {
                        if (curveArray.IsEmpty)
                        {
                            continue;
                        }
                        IList<CurveLoop> curveLoops = Utils.GetCurveLoopsByCurveArray(curveArray);
                        CurveArray curveArrayToUse = curveArray;
                        if (curveLoops.Count() > 1)
                        {
                            curveArrayToUse = Utils.GetCurveArrayByCurveLoop(curveLoops.First());
                        }
                        Solid solidUp = GeometryCreationUtilities.CreateExtrusionGeometry(curveLoops, XYZ.BasisZ, 1000);
                        Solid solidDown = GeometryCreationUtilities.CreateExtrusionGeometry(curveLoops, -XYZ.BasisZ, 1000);

                        Transaction tx = new Transaction(doc, "directShape");
                        tx.Start();
                        DirectShape directShape = DirectShape.CreateElement(doc, Category.GetCategory(doc, BuiltInCategory.OST_Walls).Id);
                        directShape.AppendShape(new List<GeometryObject> { solidUp, solidDown });
                        tx.Commit();

                        if (Utils.SolidIntersectSolid(openingsSolid, solidDown))
                        {
                            continue;
                        }
                        Floor createdFloor = Utils.CreateFloor(doc, curveArrayToUse, false);
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
                                Utils.CopyAllParameters(existingFloor, createdFloor, new List<BuiltInParameter>() { BuiltInParameter.ALL_MODEL_MARK/*, BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM*/ });
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

                    Utils.DeleteElements(doc, floors);

                //watchRooms:;
                    //uidoc.ActiveView = initialView;
                    transactionGroup.Assimilate();

                }
            }


            return Result.Succeeded;
        }
    }
}
