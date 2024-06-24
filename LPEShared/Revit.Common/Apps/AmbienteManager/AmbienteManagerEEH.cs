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
                    if (ambienteViewModel.PisoId.IntegerValue != -1)
                        doc.Delete(ambienteViewModel.PisoId);
                    var floorType = new FilteredElementCollector(doc)
                        .OfClass(typeof(FloorType))
                        .Cast<FloorType>()
                        .FirstOrDefault(a => a.Name == ambienteViewModel.FloorMatriz?.FloorName);
                    if (floorType != null && ambienteViewModel.ParentAmbienteViewModelGUID == null)
                        doc.Delete(floorType.Id);
                    if (ambienteViewModel.KSPisoId.IntegerValue != -1)
                        doc.Delete(ambienteViewModel.KSPisoId);
                    if (ambienteViewModel.KSDetalheId.IntegerValue != -1)
                        doc.Delete(ambienteViewModel.KSDetalheId);
                    if (ambienteViewModel.KSJuntaId.IntegerValue != -1)
                        doc.Delete(ambienteViewModel.KSJuntaId);
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

                    Element tipoDePisoComMesmoId = new FilteredElementCollector(doc, keyScheduleTipoDePiso.Id).Where(a => a.Id == ambienteViewModel.KSPisoId).FirstOrDefault();
                    Element itemDeDetalheComMesmoId = new FilteredElementCollector(doc, keyScheduleItensDeDetalhe.Id).Where(a => a.Id == ambienteViewModel.KSDetalheId).FirstOrDefault();
                    Element tipoDeJuntaComMesmoId = new FilteredElementCollector(doc, keyScheduleTipoDeJunta.Id).Where(a => a.Id == ambienteViewModel.KSJuntaId).FirstOrDefault();
                    List<Element> tipoDeJuntaComMesmoId2 = new FilteredElementCollector(doc, keyScheduleTipoDeJunta.Id).ToList();
                    TableData tableDataPiso = keyScheduleTipoDePiso.GetTableData();
                    TableData tableDataDetalhe = keyScheduleItensDeDetalhe.GetTableData();
                    TableData tableDataJunta = keyScheduleTipoDeJunta.GetTableData();
                    TableSectionData tsd1 = tableDataPiso.GetSectionData(SectionType.Body);
                    TableSectionData tsd2 = tableDataDetalhe.GetSectionData(SectionType.Body);
                    TableSectionData tsd3 = tableDataJunta.GetSectionData(SectionType.Body);

                    if (tipoDePisoComMesmoId == null)
                    {
                        tx.Start();
                        tsd1.InsertRow(1);
                        tx.Commit();
                        tipoDePisoComMesmoId = new FilteredElementCollector(doc, keyScheduleTipoDePiso.Id).Where(a => !a.LookupParameter("Ambiente").HasValue).FirstOrDefault();
                    }
                    if (itemDeDetalheComMesmoId == null)
                    {
                        tx.Start();
                        tsd2.InsertRow(1);
                        tx.Commit();
                        itemDeDetalheComMesmoId = new FilteredElementCollector(doc, keyScheduleItensDeDetalhe.Id).Where(a => !a.LookupParameter("Ambiente").HasValue).FirstOrDefault();
                    }
                    if (tipoDeJuntaComMesmoId == null)
                    {
                        tx.Start();
                        tsd3.InsertRow(1);
                        tx.Commit();
                        tipoDeJuntaComMesmoId = new FilteredElementCollector(doc, keyScheduleTipoDeJunta.Id).Where(a => !a.LookupParameter("Ambiente").HasValue).FirstOrDefault();
                    }

                    tx.Start();
                    AmbienteManagerUtils.SetAmbienteLPETipoDePiso(tipoDePisoComMesmoId, ambienteViewModel);
                    AmbienteManagerUtils.SetAmbienteLPEItensDeDetalhe(itemDeDetalheComMesmoId, ambienteViewModel);
                    AmbienteManagerUtils.SetAmbienteLPETipoDeJunta(tipoDeJuntaComMesmoId, ambienteViewModel);
                    if (ambienteViewModel.StoredFloorMatriz?.FloorName != null || ambienteViewModel.FloorMatriz?.FloorName != null)
                    {
                        AmbienteManagerUtils.SetFloorType(doc, ambienteViewModel, false);
                    }
                    tx.Commit();
                }

                tg.Assimilate();
                Autodesk.Revit.UI.TaskDialog.Show("Sucesso!", $"Ambientes configurados com sucesso!");
            }
            catch (Exception ex)
            {
                Autodesk.Revit.UI.TaskDialog.Show("ATENÇÃO!", $"Erro, contate os desenvolvedores.\n{ex.Message} - {ex.StackTrace}");
            }
        }

        public string GetName()
        {
            return this.GetType().Name;
        }
    }

}
