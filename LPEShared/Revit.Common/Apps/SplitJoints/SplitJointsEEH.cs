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
using static UIFramework.Widget.CustomControls.NativeMethods;

namespace Revit.Common
{
    [Transaction(TransactionMode.Manual)]
    public class SplitJointsEEH : IExternalEventHandler
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
                View initialView = uidoc.ActiveView;

                List<string> selectedAmbientes = SelectAmbienteMVVM.MainView.AmbienteViewModels.Where(x => x.IsChecked).Select(x => x.Name).ToList();

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
                        List<CurveLoop> insideOpeningCurveLoops = new List<CurveLoop>();
                        List<Element> correctJoints = new List<Element>();
                        correctJoints.AddRange(joints);
                        FailureHandlingOptions failOpt = tx.GetFailureHandlingOptions();
                        failOpt.SetFailuresPreprocessor(new Utils.BeamWarningsSwallower());
                        tx.SetFailureHandlingOptions(failOpt);
                        tx.Start();

                        Dictionary<int, List<Element>> dividedJointsAndOriginal = new Dictionary<int, List<Element>>();
                        SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 0;
                        SelectAmbienteMVVM.MainView.ProgressBar.Maximum = joints.Count;
                        int count = 0;

                        foreach (var joint in joints)
                        {
                            ExternalApplication.LPEApp.SelectAmbienteMVVM.ProgressBar_TextBlock.Text = $"Dividindo juntas do ambiente \"{ambiente}\" ({count}/{joints.Count})";
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
                            SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue += 1;
                            count++;
                        }
                        ExternalApplication.LPEApp.SelectAmbienteMVVM.ProgressBar_TextBlock.Text = $"Finalizando transação...";
                        SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 0;
                        SelectAmbienteMVVM.MainView.ProgressBar.Maximum = 2;
                        SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue += 1;

                        tx.Commit();
                        SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue += 1;

                    }
                }
                //End:;
                tg.Assimilate();
                SelectAmbienteMVVM.MainView.Dispose();
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
