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
    public class TagJointsEEH : IExternalEventHandler
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

                Element e100 = new FilteredElementCollector(doc)
                    .WhereElementIsElementType()
                    .OfCategory(BuiltInCategory.OST_StructuralFramingTags)
                    .Where(a => a.Name == "100%")
                    .First();

                Element e80 = new FilteredElementCollector(doc)
                    .WhereElementIsElementType()
                    .OfCategory(BuiltInCategory.OST_StructuralFramingTags)
                    .Where(a => a.Name == "80%")
                    .First();

                List<string> selectedAmbientes = new List<string>();
                foreach (string item in SelectAmbienteMVVM.MainView.AmbienteViewModels.Where(x => x.IsChecked).Select(x => x.Name))
                {
                    selectedAmbientes.Add(item);
                }

                XYZ rightDirection = initialView.RightDirection;
                XYZ upDirection = initialView.UpDirection;
                Options options = new Options
                {
                    View = initialView,
                    ComputeReferences = true,
                    IncludeNonVisibleObjects = true
                };
                ElementCategoryFilter linesFilter = new ElementCategoryFilter(BuiltInCategory.OST_SketchLines);

                TransactionGroup tg = new TransactionGroup(doc, "Tagear Juntas");
                tg.Start();

                foreach (var ambiente in selectedAmbientes)
                {
                    List<Element> joints = new FilteredElementCollector(doc)
                        .WhereElementIsNotElementType()
                        .OfCategory(BuiltInCategory.OST_StructuralFraming)
                        .Where(a => a.LookupParameter("Ambiente").AsString() == ambiente && !a.Name.Contains("JEP") && (a.Location as LocationCurve).Curve is Line)
                        .ToList();

                    List<Element> curveJoints = new FilteredElementCollector(doc)
                        .WhereElementIsNotElementType()
                        .OfCategory(BuiltInCategory.OST_StructuralFraming)
                        .Where(a => a.LookupParameter("Ambiente").AsString() == ambiente && !a.Name.Contains("JEP") && (a.Location as LocationCurve).Curve is Arc)
                        .ToList();

                    List<GroupOfJoints> groups = new List<GroupOfJoints>();

                    List<int> usedJoints = new List<int>();
                    groups.Add(new GroupOfJoints(joints[0]));
                    usedJoints.Add(0);
                    int groupContador = 0;
                    while (usedJoints.Count != joints.Count)
                    {
                        for (int j = 1; j < joints.Count; j++)
                        {
                            if (!usedJoints.Contains(j))
                            {
                                if (groups[groupContador].TryInsertNewJoint(joints[j], 3.28084))
                                {
                                    usedJoints.Add(j);
                                    j = 0;
                                }
                            }
                        }
                        for (int k = 1; k < joints.Count; k++)
                        {
                            if (!usedJoints.Contains(k))
                            {
                                groups.Add(new GroupOfJoints(joints[k]));
                                groupContador++;
                                usedJoints.Add(k);
                                break;
                            }
                        }

                    }

                    List<GroupOfJoints> verticalGroups = groups.Where(a => a.Orientation == JointOrientation.Vertical).OrderBy(a => a.DoubleX).ToList();
                    foreach (var group in verticalGroups)
                    {
                        if (group.Ep1.Y > group.Ep0.Y)
                        {
                            group.OrderedJoints.Reverse();
                        }
                    }
                    List<GroupOfJoints> horizontalGroups = groups.Where(a => a.Orientation == JointOrientation.Horizontal).OrderBy(a => a.DoubleY).ToList();
                    foreach (var group in horizontalGroups)
                    {
                        if (group.Ep0.X > group.Ep1.X)
                        {
                            group.OrderedJoints.Reverse();
                        }
                    }


                    SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue = 0;
                    SelectAmbienteMVVM.MainView.ProgressBar.Maximum = verticalGroups.Count + horizontalGroups.Count;
                    int count = 0;
                        

                        using (Transaction tx = new Transaction(doc, "Create Tags"))
                    {
                        tx.Start();
                        int jump = 0;
                        foreach (var group in verticalGroups)
                        {
                            for (int i = jump; i < group.OrderedJoints.Count; i++)
                            {
                                GeometryElement wholeGridGeometry = group.OrderedJoints[i].get_Geometry(options);
                                foreach (GeometryObject geomObj in wholeGridGeometry)
                                {
                                    if (geomObj is Line)
                                    {
                                        Line line = geomObj as Line;
                                        if (line.Length > 2)
                                        {
                                            IndependentTag tag = null;
                                            if (doc.GetElement(group.OrderedJoints[i].GetTypeId()).LookupParameter("Legenda").AsString().Length > 1)
                                            {
                                                tag = IndependentTag.Create(doc, e80.Id, initialView.Id, new Reference(group.OrderedJoints[i]), false, TagOrientation.Vertical, (geomObj as Line).Evaluate(0.5, true));
                                            }
                                            else
                                            {
                                                tag = IndependentTag.Create(doc, e100.Id, initialView.Id, new Reference(group.OrderedJoints[i]), false, TagOrientation.Vertical, (geomObj as Line).Evaluate(0.5, true));
                                            }
                                            if (group.OrderedJoints[index: i].Name.Contains("JE"))
                                            {
                                                tag.TagHeadPosition -= XYZ.BasisX;
                                            }

                                        }
                                        break;
                                    }
                                }
                                i++;
                            }
                            if (jump == 0)
                            {
                                jump = 1;
                            }
                            else
                            {
                                jump = 0;
                            }
                            SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue += 1;
                            count++;
                            ExternalApplication.LPEApp.SelectAmbienteMVVM.ProgressBar_TextBlock.Text = $"Tageando juntas do ambiente \"{ambiente}\" ({count}/{verticalGroups.Count + horizontalGroups.Count})";

                        }
                        jump = 0;
                        foreach (var group in horizontalGroups)
                        {
                            for (int i = jump; i < group.OrderedJoints.Count; i++)
                            {
                                GeometryElement wholeGridGeometry = group.OrderedJoints[i].get_Geometry(options);
                                foreach (GeometryObject geomObj in wholeGridGeometry)
                                {
                                    if (geomObj is Line)
                                    {
                                        Line line = geomObj as Line;
                                        if (line.Length > 2)
                                        {
                                            IndependentTag tag = null;
                                            if (doc.GetElement(group.OrderedJoints[i].GetTypeId()).LookupParameter("Legenda").AsString().Length > 1)
                                            {
                                                tag = IndependentTag.Create(doc, e80.Id, initialView.Id, new Reference(group.OrderedJoints[i]), false, TagOrientation.Vertical, (geomObj as Line).Evaluate(0.5, true));
                                            }
                                            else
                                            {
                                                tag = IndependentTag.Create(doc, e100.Id, initialView.Id, new Reference(group.OrderedJoints[i]), false, TagOrientation.Vertical, (geomObj as Line).Evaluate(0.5, true));
                                            }
                                            if (group.OrderedJoints[i].Name.Contains("JE"))
                                            {
                                                tag.TagHeadPosition += XYZ.BasisY;
                                            }
                                        }
                                    }
                                }
                                i++;
                            }
                            if (jump == 0)
                            {
                                jump = 1;
                            }
                            else
                            {
                                jump = 0;
                            }
                            SelectAmbienteMVVM.ProgressBarViewModel.ProgressBarValue += 1;
                            count++;
                            ExternalApplication.LPEApp.SelectAmbienteMVVM.ProgressBar_TextBlock.Text = $"Tageando juntas do ambiente \"{ambiente}\" ({count}/{verticalGroups.Count + horizontalGroups.Count})";
                        }
                        tx.Commit();
                    }
                }

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
