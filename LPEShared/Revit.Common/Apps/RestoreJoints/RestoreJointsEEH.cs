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

namespace Revit.Common
{
    [Transaction(TransactionMode.Manual)]
    public class RestoreJointsEEH : IExternalEventHandler
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

                using (TransactionGroup tg = new TransactionGroup(doc, "Restaurar Juntas"))
                {
                    tg.Start();

                    foreach (var stringAmbiente in checkedAmbientesNames)
                    {
                        Transaction tx = new Transaction(doc, "Rest");

                        SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 0;
                        SelectAmbienteMVVM.MainView.ProgressBar.Maximum = 2;
                        ExternalApplication.LPEApp.SelectAmbienteMVVM.ProgressBar_TextBlock.Text = $"Restaurando juntas do ambiente \"{stringAmbiente}\"...";

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
                        SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue += 1;

                        tx.Start();
                        foreach (string guid in GUIDGroupedJoints.Keys)
                        {
                            Curve curve = Utils.JoinCurves(GUIDGroupedJoints[guid].Select(a => (a.Location as LocationCurve).Curve).ToList());
                            try
                            {
                                FamilyInstance newJoint = doc.Create.NewFamilyInstance(curve, (GUIDGroupedJoints[guid][0] as FamilyInstance).Symbol, doc.GetElement((GUIDGroupedJoints[guid][0] as FamilyInstance).LevelId) as Level, Autodesk.Revit.DB.Structure.StructuralType.Beam);
                                Utils.CopyAllParametersWithoutTransaction(GUIDGroupedJoints[guid].First(), newJoint, new List<BuiltInParameter>() { BuiltInParameter.ALL_MODEL_MARK, BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM });
                            }
                            catch (Exception)
                            {

                            }
                            doc.Delete(GUIDGroupedJoints[guid].Select(a => a.Id).ToList());
                        }
                        tx.Commit();

                        SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue += 1;

                    }

                    tg.Assimilate();
                    SelectAmbienteMVVM.MainView.Dispose();
                }
            }
            catch (Exception ex)
            {
                SelectAmbienteMVVM.MainView.Dispose();
                Autodesk.Revit.UI.TaskDialog.Show("ATENÇÃO!", "Erro não mapeado, contate os desenvolvedores.\n\n" + ex.StackTrace);
                throw;
            }
        }

        public string GetName()
        {
            return this.GetType().Name;
        }
    }


}
