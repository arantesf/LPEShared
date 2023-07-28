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
using System.IO;
using System.Reflection;

#endregion

namespace Revit.Common
{
    [Transaction(TransactionMode.Manual)]
    public class SplitJoints : IExternalCommand
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
            View initialView = uidoc.ActiveView;

            //try
            //{
            bool OK = true;
            string errors = "";
            if (!Utils.VerifyIfProjectParameterExists(doc, "Ambiente"))
            {
                errors += "\n- Ambiente";
                OK = false;
            }

            if (!OK)
            {
                TaskDialog.Show("ATENÇÃO!", $"Não foi possível executar o comando por não existir no modelo os seguintes parâmetros:\n {errors}");
                return Result.Cancelled;
            }

            List<string> dividedAmbienteJoints = new FilteredElementCollector(doc, uidoc.ActiveView.Id)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .Where(a => a.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).HasValue && a.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString() != "")
                .Where(a => a.LookupParameter("Ambiente").HasValue)
                .GroupBy(a => a.LookupParameter("Ambiente").AsString())
                .Select(a => a.First().LookupParameter("Ambiente").AsString())
                .ToList();

            List<string> ambientes = new FilteredElementCollector(doc, uidoc.ActiveView.Id)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .Where(a => a.LookupParameter("Ambiente").HasValue)
                .Where(a => !dividedAmbienteJoints.Contains(a.LookupParameter("Ambiente").AsString()))
                .GroupBy(a => a.LookupParameter("Ambiente").AsString())
                .Select(a => a.First().LookupParameter("Ambiente").AsString())
                .ToList();

            //string familyName1 = "ERROR_ANNOTATION";

            //AnnotationSymbolType errorSymbol1 = new FilteredElementCollector(doc)
            //    .WhereElementIsElementType()
            //    .OfCategory(BuiltInCategory.OST_GenericAnnotation)
            //    .Where(a => a.Name == "ERRO DE PONTA")
            //    .Cast<AnnotationSymbolType>()
            //    .FirstOrDefault();

            //AnnotationSymbolType errorSymbol2 = new FilteredElementCollector(doc)
            //    .WhereElementIsElementType()
            //    .OfCategory(BuiltInCategory.OST_GenericAnnotation)
            //    .Where(a => a.Name == "ERRO DE IMPRECISÃO - REFAZER LINHA")
            //    .Cast<AnnotationSymbolType>()
            //    .FirstOrDefault();

            //if (errorSymbol1 == null || errorSymbol2 == null)
            //{
            //    string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //    string path = assemblyDirectory + "\\" + familyName1 + ".rfa";

            //    Transaction txLoad = new Transaction(doc, "Load Family");
            //    txLoad.Start();

            //    if (!doc.LoadFamily(path, new FamilyOption(), out Family family))
            //    {
            //        txLoad.RollBack();
            //        return Result.Cancelled;
            //    }
            //    txLoad.Commit();
            //}

            //string familyName2 = "ERROR_GENERICMODEL";

            //FamilySymbol errorSymbol3 = new FilteredElementCollector(doc)
            //    .WhereElementIsElementType()
            //    .OfCategory(BuiltInCategory.OST_GenericModel)
            //    .Where(a => a.Name == "ERRO DE PONTA")
            //    .Cast<FamilySymbol>()
            //    .FirstOrDefault();

            //FamilySymbol errorSymbol4 = new FilteredElementCollector(doc)
            //    .WhereElementIsElementType()
            //    .OfCategory(BuiltInCategory.OST_GenericModel)
            //    .Where(a => a.Name == "ERRO DE IMPRECISÃO - REFAZER LINHA")
            //    .Cast<FamilySymbol>()
            //    .FirstOrDefault();

            //if (errorSymbol3 == null || errorSymbol4 == null)
            //{
            //    string assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //    string path = assemblyDirectory + "\\" + familyName2 + ".rfa";

            //    Transaction txLoad = new Transaction(doc, "Load Family");
            //    txLoad.Start();

            //    if (!doc.LoadFamily(path, new FamilyOption(), out Family family))
            //    {
            //        txLoad.RollBack();
            //        return Result.Cancelled;
            //    }
            //    txLoad.Commit();
            //}

            //errorSymbol1 = new FilteredElementCollector(doc)
            //   .WhereElementIsElementType()
            //   .OfCategory(BuiltInCategory.OST_GenericAnnotation)
            //   .Where(a => a.Name == "ERRO DE PONTA")
            //   .Cast<AnnotationSymbolType>()
            //   .FirstOrDefault();

            //errorSymbol2 = new FilteredElementCollector(doc)
            //    .WhereElementIsElementType()
            //    .OfCategory(BuiltInCategory.OST_GenericAnnotation)
            //    .Where(a => a.Name == "ERRO DE IMPRECISÃO - REFAZER LINHA")
            //    .Cast<AnnotationSymbolType>()
            //    .FirstOrDefault();

            //errorSymbol3 = new FilteredElementCollector(doc)
            //    .WhereElementIsElementType()
            //    .OfCategory(BuiltInCategory.OST_GenericModel)
            //    .Where(a => a.Name == "ERRO DE PONTA")
            //    .Cast<FamilySymbol>()
            //    .FirstOrDefault();

            //errorSymbol4 = new FilteredElementCollector(doc)
            //    .WhereElementIsElementType()
            //    .OfCategory(BuiltInCategory.OST_GenericModel)
            //    .Where(a => a.Name == "ERRO DE IMPRECISÃO - REFAZER LINHA")
            //    .Cast<FamilySymbol>()
            //    .FirstOrDefault();
            var window = new SelectAmbienteMVVM(uidoc, SelectAmbientMVVMExecuteCommand.SplitJoints);

            //var window = new SelectAmbienteMVVM(ambientes, "DIVIDIR JUNTAS", System.Windows.Visibility.Collapsed);
            window.ShowDialog();
            if (!window.Execute)
            {
                return Result.Cancelled;
            }
            List<string> selectedAmbientes = new List<string>();
            foreach (string item in window.AmbienteViewModels.Where(x => x.IsChecked).Select(x => x.Name))
            {
                selectedAmbientes.Add(item);
            }

            Dictionary<string, DateTime> dateTimes = new Dictionary<string, DateTime>();
            //dateTimes.Add("inicio", DateTime.Now);

            Dictionary<string, List<Element>> groupedJointsByAmbiente = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .Where(a => !a.Name.Contains("JPE") && !a.Name.EndsWith("JE") && selectedAmbientes.Contains(a.LookupParameter("Ambiente").AsString()))
                .GroupBy(a => a.LookupParameter("Ambiente").AsString())
                .ToDictionary(a => a.Key, a => a.ToList());



            Dictionary<string, List<Element>> groupedFloorByAmbiente = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_Floors)
                .Where(a => a.LookupParameter("Reforço de Tela").AsInteger() == 0 && selectedAmbientes.Contains(a.LookupParameter("Ambiente").AsString()))
                .GroupBy(a => a.LookupParameter("Ambiente").AsString())
                .ToDictionary(a => a.Key, a => a.ToList());

            Dictionary<string, List<Element>> dividedGroupedJointsByAmbiente = new Dictionary<string, List<Element>>();

            //dateTimes.Add("grouped", DateTime.Now);

            TransactionGroup tg = new TransactionGroup(doc, "Divide Joints");
            tg.Start();
            using (Transaction tx = new Transaction(doc, "Divide Joints"))
            {

                foreach (var ambienteJoints in groupedJointsByAmbiente)
                {
                    string ambiente = ambienteJoints.Key;
                    List<Element> joints = ambienteJoints.Value;

                    //List<Element> verifiedJoints = joints
                    //    .Where(a => a.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString() == "Junta Verificada")
                    //    .ToList();

                    //if (joints.Count == verifiedJoints.Count)
                    //{
                    //    TaskDialog.Show("ATENÇÃO", $"Verifique as juntas do ambiente {ambiente} antes de executar o comando");
                    //    continue;
                    //}

                    List<Curve> jointsCurves = joints.Select(a => Utils.GetCurveProjection((a.Location as LocationCurve).Curve)).ToList();
                    List<Curve> allCurves = new List<Curve>();
                    allCurves.AddRange(jointsCurves);
                    List<Element> ambienteFloors = new List<Element>();
                    try
                    {
                        ambienteFloors = groupedFloorByAmbiente[ambiente];

                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    dividedGroupedJointsByAmbiente.Add(ambienteJoints.Key, new List<Element>());
                    List<Curve> floorCurves = new List<Curve>();
                    List<ElementId> elementIdsToIsolate = new List<ElementId>();
                    List<ElementId> elementsToFocus = new List<ElementId>();
                    Transaction tx1 = new Transaction(doc);
                    //List<Curve> originalFloorCurves = new List<Curve>();
                    List<CurveLoop> insideOpeningCurveLoops = new List<CurveLoop>();
                    //foreach (var floor in ambienteFloors)
                    //{
                    //    floorCount++;
                    //    floorCurves.AddRange(Utils.GetFloorLinesWithoutSideOpeningsForJoint(doc, floor as Floor, out insideOpeningCurveLoops));
                    //    //originalFloorCurves.AddRange(Utils.GetFloorLinesWithoutSideOpenings(doc, floor as Floor, out insideOpeningCurveLoops));
                    //}
                    List<Element> correctJoints = new List<Element>();
                    correctJoints.AddRange(joints);
                    //Tuple<List<XYZ>, List<XYZ>> pointsWithError = Utils.VerifyJoints3(ambiente, floorCurves, insideOpeningCurveLoops, joints, out correctJoints);



                    ////goto End;

                    //if (pointsWithError.Item1.Any() || pointsWithError.Item2.Any())
                    //{
                    //    try
                    //    {
                    //        checkView = new FilteredElementCollector(doc)
                    //            .WhereElementIsNotElementType()
                    //            .OfCategory(BuiltInCategory.OST_Views)
                    //            .Where(a => a.Name == "CONFERÊNCIA - " + ambiente)
                    //            .Cast<View>()
                    //            .First();
                    //    }
                    //    catch (Exception)
                    //    {
                    //        tx1.Start("Criar Vista e Isolar Elementos");
                    //        checkView = doc.GetElement(initialView.Duplicate(ViewDuplicateOption.Duplicate)) as View;
                    //        checkView.Name = "CONFERÊNCIA - " + ambiente;
                    //        tx1.Commit();
                    //    }

                    //    tx1.Start("Criar Erros");
                    //    if (initialView is View3D)
                    //    {
                    //        foreach (var point in pointsWithError.Item1)
                    //        {
                    //            FamilyInstance error = doc.Create.NewFamilyInstance(point, errorSymbol3, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                    //            elementIdsToIsolate.Add(error.Id);
                    //        }
                    //        foreach (var point in pointsWithError.Item2)
                    //        {
                    //            FamilyInstance error = doc.Create.NewFamilyInstance(point, errorSymbol4, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                    //            elementIdsToIsolate.Add(error.Id);
                    //        }
                    //    }
                    //    else
                    //    {
                    //        foreach (var point in pointsWithError.Item1)
                    //        {
                    //            FamilyInstance error = doc.Create.NewFamilyInstance(point, errorSymbol1, checkView);
                    //            elementIdsToIsolate.Add(error.Id);
                    //        }
                    //        foreach (var point in pointsWithError.Item2)
                    //        {
                    //            FamilyInstance error = doc.Create.NewFamilyInstance(point, errorSymbol2, checkView);
                    //            elementIdsToIsolate.Add(error.Id);
                    //        }
                    //    }


                    //    errorCount += pointsWithError.Item1.Count + pointsWithError.Item2.Count;
                    //    elementIdsToIsolate.AddRange(joints.Select(a => a.Id));
                    //    elementIdsToIsolate.AddRange(ambienteFloors.Select(a => a.Id));
                    //    //checkViews.Add(checkView, elementIdsToIsolate);

                    //    tx1.Commit();
                    //    uidoc.ActiveView = checkView;
                    //    elementsToFocus.AddRange(joints.Select(a => a.Id));
                    //    elementsToFocus.AddRange(ambienteFloors.Select(a => a.Id));


                    //}
                    //else
                    //{
                    //    tx1.Start("Criar Vista e Isolar Elementos");
                    //    try
                    //    {
                    //        doc.Delete(new FilteredElementCollector(doc).WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Views).Where(a => a.Name == "CONFERÊNCIA - " + ambiente).Select(a => a.Id).ToList());
                    //    }
                    //    catch (Exception) { }
                    //    tx1.Commit();
                    //}


                    //if (elementIdsToIsolate.Any())
                    //{
                    //    tx1.Start("Isolar Elementos");
                    //    checkView.IsolateElementsTemporary(elementIdsToIsolate);
                    //    checkView.ConvertTemporaryHideIsolateToPermanent();
                    //    tx1.Commit();
                    //    uidoc.ShowElements(elementsToFocus);

                    //    var checkWindow = new Form2(ambiente, errorCount, "Dividir Juntas");
                    //    checkWindow.ShowDialog();
                    //    if (checkWindow.cancelled)
                    //    {
                    //        tg.Assimilate();
                    //        return Result.Succeeded;
                    //    }
                    //    if (!checkWindow.force)
                    //    {
                    //        continue;
                    //    }
                    //}

                    ////dateTimes.Add("verify", DateTime.Now);
                    FailureHandlingOptions failOpt = tx.GetFailureHandlingOptions();
                    failOpt.SetFailuresPreprocessor(new Utils.BeamWarningsSwallower());
                    tx.SetFailureHandlingOptions(failOpt);
                    //allCurves.AddRange(floorCurves);
                    tx.Start();

                    Dictionary<int, List<Element>> dividedJointsAndOriginal = new Dictionary<int, List<Element>>();

                    foreach (var joint in joints)
                    {
                        if (joint.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).HasValue && joint.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).AsString() != "")
                        {
                            continue;
                        }
                        Curve initialCurve = (joint.Location as LocationCurve).Curve;
                        Curve initialCurveProjection = Utils.GetCurveProjection(initialCurve);
                        List<Curve> splitCurves = Utils.SplitCurveByOtherCurves(initialCurveProjection, allCurves);
                        string guid = Guid.NewGuid().ToString();
#if Revit2021 || Revit2022 || Revit2023
                            int jointId = joint.Id.IntegerValue;
#else
                        int jointId = (int)joint.Id.Value;
#endif
                        dividedJointsAndOriginal.Add(jointId, new List<Element>());

                        if (splitCurves.Count > 1)
                        {
                            //tx.Start();
                            joint.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set(guid);
                            //tx.Commit();
                            dividedGroupedJointsByAmbiente[ambiente].Add(joint);
                            foreach (var splitCurve in splitCurves)
                            {
                                Level level = doc.GetElement((joint as FamilyInstance).get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM).AsElementId()) as Level;
                                FamilyInstance newJoint = doc.Create.NewFamilyInstance(splitCurve, (joint as FamilyInstance).Symbol, level, Autodesk.Revit.DB.Structure.StructuralType.Beam);
                                if (!newJoint.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM).IsReadOnly)
                                {
                                    newJoint.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM).Set(joint.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM).AsElementId());
                                }
                                //tx.Start();
                                Utils.CopyAllParametersWithoutTransaction(joint, newJoint, new List<BuiltInParameter>() { BuiltInParameter.ALL_MODEL_MARK });
                                //tx.Commit();

                                dividedJointsAndOriginal[jointId].Add(newJoint);
                                if (Math.Abs((initialCurve.GetEndPoint(1) - initialCurve.GetEndPoint(0)).AngleTo(XYZ.BasisZ) - (Math.PI / 2)) > 0.01)
                                {
                                    Curve curveEp0 = Line.CreateBound(splitCurve.GetEndPoint(0) - XYZ.BasisZ * 5000, splitCurve.GetEndPoint(0) + XYZ.BasisZ * 10000);
                                    Curve curveEp1 = Line.CreateBound(splitCurve.GetEndPoint(1) - XYZ.BasisZ * 5000, splitCurve.GetEndPoint(1) + XYZ.BasisZ * 10000);
                                    curveEp0.Intersect(initialCurve, out IntersectionResultArray resultArray0);
                                    curveEp1.Intersect(initialCurve, out IntersectionResultArray resultArray1);
                                    double ep0Offset = resultArray0.get_Item(0).XYZPoint.Z - splitCurve.GetEndPoint(0).Z;
                                    double ep1Offset = resultArray1.get_Item(0).XYZPoint.Z - splitCurve.GetEndPoint(1).Z;
                                    newJoint.get_Parameter(BuiltInParameter.STRUCTURAL_BEAM_END0_ELEVATION).Set(ep0Offset);
                                    newJoint.get_Parameter(BuiltInParameter.STRUCTURAL_BEAM_END1_ELEVATION).Set(ep1Offset);
                                }
                            }
                            doc.Delete(joint.Id);
                        }

                    }
                    tx.Commit();
                    //dateTimes.Add("Divide", DateTime.Now);
                    //tx.Start();

                    //foreach (var kvp in dividedJointsAndOriginal)
                    //{
                    //    Element e = doc.GetElement(new ElementId(kvp.Key));
                    //    foreach (var split in kvp.Value)
                    //    {
                    //        Utils.CopyAllParameters(e, split, new List<BuiltInParameter>() { BuiltInParameter.ALL_MODEL_MARK, BuiltInParameter.STRUCTURAL_BEAM_END0_ELEVATION, BuiltInParameter.STRUCTURAL_BEAM_END1_ELEVATION });
                    //        split.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM).Set(e.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM).AsElementId());
                    //    }
                    //}
                    //tx.Commit();
                    //dateTimes.Add("Copy", DateTime.Now);

                }



                //tx.Start();
                //foreach (var ambienteJoints in dividedGroupedJointsByAmbiente)
                //{
                //    List<Element> joints = ambienteJoints.Value;

                //        doc.Delete(joints.Select(a => a.Id).ToList());
                //}
                //tx.Commit();

                //dateTimes.Add("Delete", DateTime.Now);


            }
            //End:;
            tg.Assimilate();

            ////dateTimes.Add("fim", DateTime.Now);
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

