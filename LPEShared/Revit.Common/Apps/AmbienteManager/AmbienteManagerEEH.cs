using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Revit.Common
{
    public class AddAmbienteEEH : IExternalEventHandler
    {
        public void Execute(UIApplication uiapp)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            //string selectedAmbienteName = "TESTE";
            //AmbienteManagerUtils.DuplicateViewSchedules(doc, selectedAmbienteName);
        }

        public string GetName()
        {
            return this.GetType().Name;
        }
    }

    public class RemoveAmbienteEEH : IExternalEventHandler
    {
        public void Execute(UIApplication uiapp)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            //string selectedAmbienteName = "TESTE";
            //AmbienteManagerUtils.DeleteViewSchedules(doc, selectedAmbienteName);

        }

        public string GetName()
        {
            return this.GetType().Name;
        }
    }

    public class EditAmbienteEEH : IExternalEventHandler
    {
        public void Execute(UIApplication uiapp)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

        }

        public string GetName()
        {
            return this.GetType().Name;
        }
    }

    public class DuplicateAmbienteEEH : IExternalEventHandler
    {
        public void Execute(UIApplication uiapp)
        {
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            string selectedAmbienteName = "TESTE";
            AmbienteManagerUtils.DuplicateViewSchedules(doc, selectedAmbienteName);

        }

        public string GetName()
        {
            return this.GetType().Name;
        }
    }

    public class ApplyAmbientesEEH : IExternalEventHandler
    {
        public void Execute(UIApplication uiapp)
        {
            try
            {
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;
                TransactionGroup tg = new TransactionGroup(doc, "Gerenciador de Ambientes");
                Transaction tx = new Transaction(doc, "tx");
                tg.Start();
                foreach (var ambienteViewModel in AmbienteManagerMVVM.MainView.AmbienteViewModelsToDelete)
                {
                    tx.Start();
                    var floorType = new FilteredElementCollector(doc)
                        .OfClass(typeof(FloorType))
                        .Cast<FloorType>()
                        .FirstOrDefault(a => a.Name == ambienteViewModel.FloorMatriz?.FloorName);
                    if (floorType != null)
                    {
                        doc.Delete(floorType.Id);
                    }
                    doc.Delete(ambienteViewModel.Id);
                    tx.Commit();
                }
                foreach (var ambienteViewModel in AmbienteManagerMVVM.MainView.AmbienteViewModels)
                {
                    ViewSchedule keyScheduleTipoDePiso = new FilteredElementCollector(doc)
                        .WhereElementIsNotElementType()
                        .OfCategory(BuiltInCategory.OST_Schedules)
                        .Cast<ViewSchedule>()
                        .Where(view => view.Name == "LPE_TIPO DE PISO")
                        .FirstOrDefault();

                    ViewSchedule keyScheduleTipoDeJunta = new FilteredElementCollector(doc)
                        .WhereElementIsNotElementType()
                        .OfCategory(BuiltInCategory.OST_Schedules)
                        .Cast<ViewSchedule>()
                        .Where(view => view.Name == "LPE_TIPO DE JUNTA")
                        .FirstOrDefault();

                    ViewSchedule keyScheduleItensDeDetalhe = new FilteredElementCollector(doc)
                        .WhereElementIsNotElementType()
                        .OfCategory(BuiltInCategory.OST_Schedules)
                        .Cast<ViewSchedule>()
                        .Where(view => view.Name == "LPE_ITENS DE DETALHE SEÇÕES")
                        .FirstOrDefault();
                    
                    Element tipoDePisoComMesmoId = new FilteredElementCollector(doc, keyScheduleTipoDePiso.Id).Where(a => a.Id == ambienteViewModel.Id).FirstOrDefault();
                    Element itemDeDetalheComMesmoAmbiente = new FilteredElementCollector(doc, keyScheduleItensDeDetalhe.Id).Where(a => a.get_Parameter(BuiltInParameter.REF_TABLE_ELEM_NAME).AsString() == ambienteViewModel.TipoDePiso).FirstOrDefault();
                    Element tipoDeJuntaComMesmoAmbiente = new FilteredElementCollector(doc, keyScheduleTipoDeJunta.Id).Where(a => a.get_Parameter(BuiltInParameter.REF_TABLE_ELEM_NAME).AsString() == ambienteViewModel.TipoDePiso).FirstOrDefault();
                    switch (ambienteViewModel.Action)
                    {
                        case Action.Continue:
                            if (itemDeDetalheComMesmoAmbiente == null)
                            {
                                if (ambienteViewModel.Ambiente != null || ambienteViewModel.Ambiente != "")
                                {
                                    List<ElementId> itensDeDetalheExistentes = new FilteredElementCollector(doc, keyScheduleItensDeDetalhe.Id).ToElementIds().ToList();
                                    tx.Start();
                                    TableData tableDataItensDeDetalhe = keyScheduleItensDeDetalhe.GetTableData();
                                    TableSectionData tsdItensDeDetalhe = tableDataItensDeDetalhe.GetSectionData(SectionType.Body);
                                    tsdItensDeDetalhe.InsertRow(1);
                                    tx.Commit();
                                    itemDeDetalheComMesmoAmbiente = new FilteredElementCollector(doc, keyScheduleItensDeDetalhe.Id).Where(a => !itensDeDetalheExistentes.Contains(a.Id)).FirstOrDefault();
                                }
                            }
                            if (tipoDeJuntaComMesmoAmbiente == null)
                            {
                                if (ambienteViewModel.Ambiente != null || ambienteViewModel.Ambiente != "")
                                {
                                    List<ElementId> tipoDeJuntaExistentes = new FilteredElementCollector(doc, keyScheduleTipoDeJunta.Id).ToElementIds().ToList();
                                    tx.Start();
                                    TableData tableDataTipoDeJunta = keyScheduleTipoDeJunta.GetTableData();
                                    TableSectionData tsdTipoDeJunta = tableDataTipoDeJunta.GetSectionData(SectionType.Body);
                                    tsdTipoDeJunta.InsertRow(1);
                                    tx.Commit();
                                    tipoDeJuntaComMesmoAmbiente = new FilteredElementCollector(doc, keyScheduleTipoDeJunta.Id).Where(a => !tipoDeJuntaExistentes.Contains(a.Id)).FirstOrDefault();
                                }
                            }

                            tx.Start();
                            AmbienteManagerUtils.SetAmbienteLPETipoDePiso(tipoDePisoComMesmoId, ambienteViewModel);
                            AmbienteManagerUtils.SetAmbienteLPEItensDeDetalhe(itemDeDetalheComMesmoAmbiente, ambienteViewModel);
                            AmbienteManagerUtils.SetAmbienteLPETipoDeJunta(tipoDeJuntaComMesmoAmbiente, ambienteViewModel);
                            if (ambienteViewModel.SelectedfloorMatriz?.FloorName != null || ambienteViewModel.FloorMatriz?.FloorName != null)
                            {
                                AmbienteManagerUtils.SetFloorType(doc, ambienteViewModel, false);
                            }
                            tx.Commit();
                            break;
                        case Action.Delete:
                            break;
                        case Action.Modify:
                            if (itemDeDetalheComMesmoAmbiente == null)
                            {
                                if (ambienteViewModel.Ambiente != null || ambienteViewModel.Ambiente != "")
                                {
                                    List<ElementId> itensDeDetalheExistentes = new FilteredElementCollector(doc, keyScheduleItensDeDetalhe.Id).ToElementIds().ToList();
                                    tx.Start();
                                    TableData tableDataItensDeDetalhe = keyScheduleItensDeDetalhe.GetTableData();
                                    TableSectionData tsdItensDeDetalhe = tableDataItensDeDetalhe.GetSectionData(SectionType.Body);
                                    tsdItensDeDetalhe.InsertRow(1);
                                    tx.Commit();
                                    itemDeDetalheComMesmoAmbiente = new FilteredElementCollector(doc, keyScheduleItensDeDetalhe.Id).Where(a => !itensDeDetalheExistentes.Contains(a.Id)).FirstOrDefault();
                                }
                            }
                            if (tipoDeJuntaComMesmoAmbiente == null)
                            {
                                if (ambienteViewModel.Ambiente != null || ambienteViewModel.Ambiente != "")
                                {
                                    List<ElementId> tipoDeJuntaExistentes = new FilteredElementCollector(doc, keyScheduleTipoDeJunta.Id).ToElementIds().ToList();
                                    tx.Start();
                                    TableData tableDataTipoDeJunta = keyScheduleTipoDeJunta.GetTableData();
                                    TableSectionData tsdTipoDeJunta = tableDataTipoDeJunta.GetSectionData(SectionType.Body);
                                    tsdTipoDeJunta.InsertRow(1);
                                    tx.Commit();
                                    tipoDeJuntaComMesmoAmbiente = new FilteredElementCollector(doc, keyScheduleTipoDeJunta.Id).Where(a => !tipoDeJuntaExistentes.Contains(a.Id)).FirstOrDefault();
                                }
                            }
                            tx.Start();
                            AmbienteManagerUtils.SetAmbienteLPETipoDePiso(tipoDePisoComMesmoId, ambienteViewModel);
                            AmbienteManagerUtils.SetAmbienteLPEItensDeDetalhe(itemDeDetalheComMesmoAmbiente, ambienteViewModel);
                            AmbienteManagerUtils.SetAmbienteLPETipoDeJunta(tipoDeJuntaComMesmoAmbiente, ambienteViewModel);
                            if (ambienteViewModel.SelectedfloorMatriz?.FloorName != null || ambienteViewModel.FloorMatriz?.FloorName != null)
                            {
                                AmbienteManagerUtils.SetFloorType(doc, ambienteViewModel, false);
                            }
                            tx.Commit();
                            break;
                        case Action.Create:
                            tx.Start();
                            TableData tableData = keyScheduleTipoDePiso.GetTableData();
                            TableSectionData tsd = tableData.GetSectionData(SectionType.Body);
                            tsd.InsertRow(1);
                            tx.Commit();

                            
                            Element novoTipoDePiso = new FilteredElementCollector(doc, keyScheduleTipoDePiso.Id).Where(a => !a.LookupParameter("Ambiente").HasValue).FirstOrDefault();

                            if (itemDeDetalheComMesmoAmbiente == null)
                            {
                                if (ambienteViewModel.Ambiente != null || ambienteViewModel.Ambiente != "")
                                {
                                    List<ElementId> itensDeDetalheExistentes = new FilteredElementCollector(doc, keyScheduleItensDeDetalhe.Id).ToElementIds().ToList();
                                    tx.Start();
                                    TableData tableDataItensDeDetalhe = keyScheduleItensDeDetalhe.GetTableData();
                                    TableSectionData tsdItensDeDetalhe = tableDataItensDeDetalhe.GetSectionData(SectionType.Body);
                                    tsdItensDeDetalhe.InsertRow(1);
                                    tx.Commit();
                                    itemDeDetalheComMesmoAmbiente = new FilteredElementCollector(doc, keyScheduleItensDeDetalhe.Id).Where(a => !itensDeDetalheExistentes.Contains(a.Id)).FirstOrDefault();
                                }
                            }
                            if (tipoDeJuntaComMesmoAmbiente == null)
                            {
                                if (ambienteViewModel.Ambiente != null || ambienteViewModel.Ambiente != "")
                                {
                                    List<ElementId> tipoDeJuntaExistentes = new FilteredElementCollector(doc, keyScheduleTipoDeJunta.Id).ToElementIds().ToList();
                                    tx.Start();
                                    TableData tableDataTipoDeJunta = keyScheduleTipoDeJunta.GetTableData();
                                    TableSectionData tsdTipoDeJunta = tableDataTipoDeJunta.GetSectionData(SectionType.Body);
                                    tsdTipoDeJunta.InsertRow(1);
                                    tx.Commit();
                                    tipoDeJuntaComMesmoAmbiente = new FilteredElementCollector(doc, keyScheduleTipoDeJunta.Id).Where(a => !tipoDeJuntaExistentes.Contains(a.Id)).FirstOrDefault();
                                }
                            }
                            tx.Start();
                            AmbienteManagerUtils.SetAmbienteLPETipoDePiso(novoTipoDePiso, ambienteViewModel);
                            AmbienteManagerUtils.SetAmbienteLPEItensDeDetalhe(itemDeDetalheComMesmoAmbiente, ambienteViewModel);
                            AmbienteManagerUtils.SetAmbienteLPETipoDeJunta(tipoDeJuntaComMesmoAmbiente, ambienteViewModel);
                            if (ambienteViewModel.SelectedfloorMatriz?.FloorName != null || ambienteViewModel.FloorMatriz?.FloorName != null)
                            {
                                AmbienteManagerUtils.SetFloorType(doc, ambienteViewModel, true);
                            }
                            tx.Commit();
                            break;
                        default:
                            break;
                    }
                }

                tg.Assimilate();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("ATENÇÃO!", $"Erro, contate os desenvolvedores.\n{ex.Message} - {ex.StackTrace}");
            }
            TaskDialog.Show("Sucesso!", $"Ambientes configurados com sucesso!");
        }

        public string GetName()
        {
            return this.GetType().Name;
        }
    }

}
