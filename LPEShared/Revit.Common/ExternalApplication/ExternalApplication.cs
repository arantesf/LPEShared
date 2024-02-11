using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.IO;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit.Resources;
using System.Collections.Generic;

namespace Revit.Common
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ExternalApplication : IExternalApplication
    {
        internal UIApplication uiApp = null;
        internal static ExternalApplication LPEApp = new ExternalApplication();

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            AddRibbonButtons(application);

            return Result.Succeeded;
        }

        internal ExternalEvent ExternalEvent = null;
        internal SelectAmbienteMVVM SelectAmbienteMVVM = null;
        internal SelectAmbienteReinforcementMVVM SelectAmbienteReinforcementMVVM = null;
        internal AmbienteManagerMVVM AmbienteManagerMVVM = null;

        internal void ShowAmbienteManagerUI(
          Dictionary<FloorMatrizClass, List<FloorMatriz>> floorMatrizes,
          List<FullAmbienteViewModel> fullAmbienteViewModels,
          List<string> allMaterialNames,
          Dictionary<MaterialClass, List<string>> materialsByClass)
        {
            if (this.AmbienteManagerMVVM != null && this.AmbienteManagerMVVM.IsLoaded)
                return;
            UIDocument activeUiDocument = this.uiApp.ActiveUIDocument;
            this.AmbienteManagerMVVM = new AmbienteManagerMVVM(floorMatrizes, fullAmbienteViewModels, allMaterialNames, materialsByClass);
            this.AmbienteManagerMVVM.Topmost = true;
            if (!this.AmbienteManagerMVVM.IsInitialized)
                return;
            this.AmbienteManagerMVVM.Show();
        }

        internal void ShowMeshReinforcementUI()
        {
            if (SelectAmbienteReinforcementMVVM == null || SelectAmbienteReinforcementMVVM.IsLoaded == false)
            {
                UIDocument uidoc = uiApp.ActiveUIDocument;
                //INICIALIZANDO A JANELA E PASSANDO O EXTERNAL EVENT
                SelectAmbienteReinforcementMVVM = new SelectAmbienteReinforcementMVVM(uidoc);
                SelectAmbienteReinforcementMVVM.Topmost = true;
                if (SelectAmbienteReinforcementMVVM.IsInitialized)
                {
                    SelectAmbienteReinforcementMVVM.Show();
                }
            }
        }

        internal void ShowSplitFloorsUI()
        {
            if (SelectAmbienteMVVM == null || SelectAmbienteMVVM.IsLoaded == false)
            {
                UIDocument uidoc = uiApp.ActiveUIDocument;

                //INICIALIZANDO A JANELA E PASSANDO O EXTERNAL EVENT
                SelectAmbienteMVVM = new SelectAmbienteMVVM(uidoc, SelectAmbientMVVMExecuteCommand.SplitFloors);
                SelectAmbienteMVVM.Topmost = true;
                if (SelectAmbienteMVVM.IsInitialized)
                {
                    SelectAmbienteMVVM.Show();
                }
            }
        }

        internal void ShowRestoreFloorsUI()
        {
            if (SelectAmbienteMVVM == null || SelectAmbienteMVVM.IsLoaded == false)
            {
                UIDocument uidoc = uiApp.ActiveUIDocument;

                //INICIALIZANDO A JANELA E PASSANDO O EXTERNAL EVENT
                SelectAmbienteMVVM = new SelectAmbienteMVVM(uidoc, SelectAmbientMVVMExecuteCommand.RestoreFloors);
                SelectAmbienteMVVM.Topmost = true;
                if (SelectAmbienteMVVM.IsInitialized)
                {
                    SelectAmbienteMVVM.Show();
                }
            }
        }

        internal void ShowDimensionFloorsUI()
        {
            if (SelectAmbienteMVVM == null || SelectAmbienteMVVM.IsLoaded == false)
            {
                UIDocument uidoc = uiApp.ActiveUIDocument;

                //INICIALIZANDO A JANELA E PASSANDO O EXTERNAL EVENT
                SelectAmbienteMVVM = new SelectAmbienteMVVM(uidoc, SelectAmbientMVVMExecuteCommand.DimensionFloors);
                SelectAmbienteMVVM.Topmost = true;
                if (SelectAmbienteMVVM.IsInitialized)
                {
                    SelectAmbienteMVVM.Show();
                }
            }
        }

        internal void ShowTagJointsUI()
        {
            if (SelectAmbienteMVVM == null || SelectAmbienteMVVM.IsLoaded == false)
            {
                UIDocument uidoc = uiApp.ActiveUIDocument;

                //INICIALIZANDO A JANELA E PASSANDO O EXTERNAL EVENT
                SelectAmbienteMVVM = new SelectAmbienteMVVM(uidoc, SelectAmbientMVVMExecuteCommand.TagJoints);
                SelectAmbienteMVVM.Topmost = true;
                if (SelectAmbienteMVVM.IsInitialized)
                {
                    SelectAmbienteMVVM.Show();
                }
            }
        }

        internal void ShowSplitJointsUI()
        {
            if (SelectAmbienteMVVM == null || SelectAmbienteMVVM.IsLoaded == false)
            {
                UIDocument uidoc = uiApp.ActiveUIDocument;

                //INICIALIZANDO A JANELA E PASSANDO O EXTERNAL EVENT
                SelectAmbienteMVVM = new SelectAmbienteMVVM(uidoc, SelectAmbientMVVMExecuteCommand.SplitJoints);
                SelectAmbienteMVVM.Topmost = true;
                if (SelectAmbienteMVVM.IsInitialized)
                {
                    SelectAmbienteMVVM.Show();
                }
            }
        }
        private void AddRibbonButtons(UIControlledApplication application)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string executingAssemblyPath = assembly.Location;
            Debug.Print(executingAssemblyPath);
            string executingAssemblyName = assembly.GetName().Name;
            Console.WriteLine(executingAssemblyName);
            string tabName = "LPE";

            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                // tab already exists
            }
            RibbonPanel pisosEJuntasPanel = application.CreateRibbonPanel(tabName, "Pisos e Juntas");
            RibbonPanel ambientesPanel = application.CreateRibbonPanel(tabName, "Ambientes");
            RibbonPanel sobrePanel = application.CreateRibbonPanel(tabName, "Sobre");

            //////////// SPLIT FLOORS ////////////

            pisosEJuntasPanel.AddItem(new PushButtonData("SplitFloors", "Dividir Pisos", executingAssemblyPath, typeof(SplitFloorsEC).FullName)
            {
                ToolTip = "Divide o piso de um ambiente com base nas suas juntas.",
                LongDescription = "O parâmetro \"Ambiente\" deve ser preenchido tanto nos elementos de pisos quanto nos respectivos elementos de juntas.",
                LargeImage = ResourceImage.GetIcon("DividirPisosLPE.png")
            });

            //////////// SPLIT JOINTS ////////////

            pisosEJuntasPanel.AddItem(new PushButtonData("SplitJoints", "Dividir Juntas", executingAssemblyPath, typeof(SplitJointsEC).FullName)
            {
                ToolTip = "Divide as juntas do modelo pelos encontros com outras juntas ou outros pisos do mesmo ambiente.",
                LongDescription = "O parâmetro \"Ambiente\" deve ser preenchido tanto nos elementos de pisos quanto nos respectivos elementos de juntas.",
                LargeImage = ResourceImage.GetIcon("DividirJuntasLPE.png")
            });

            //////////// DIMENSION FLOORS ////////////

            pisosEJuntasPanel.AddItem(new PushButtonData("DimensionFloors", "Cotar Pisos", executingAssemblyPath, typeof(DimensionFloorsEC).FullName)
            {
                ToolTip = "Cota os pisos divididos em suas duas direções principais.",
                LongDescription = "O comando utilizará o comando Aligned Dimension para criar 2 cotas em cada piso do modelo.",
                LargeImage = ResourceImage.GetIcon("CotarPisosLPE.png")
            });

            ////////////// MESH REINFORCEMENT ////////////

            pisosEJuntasPanel.AddItem(new PushButtonData("MeshReinforcement", "Reforçar com Tela", executingAssemblyPath, typeof(MeshReinforcementEC).FullName)
            {
                ToolTip = "Caracteriza os pisos de fibra (Parâmetro ''(s/n) Fibra'') que possuem dimensões com porporção maiores que 1,5:1 com uma hachura e o preenchimento dos parâmetros de espaçadores.",
                LongDescription = "Escolha, na janela que se abre, a tela que deseja inserir como reforço e o cobrimento a ser aplicado.",
                LargeImage = ResourceImage.GetIcon("ReforçarPisosLPE.png")
            });

            //////////// TAG JOINTS ////////////

            pisosEJuntasPanel.AddItem(new PushButtonData("TagJoints", "Tagear Juntas", executingAssemblyPath, typeof(TagJointsEC).FullName)
            {
                ToolTip = "Etiqueta as juntas dos ambientes escolhidos na janela.",
                LongDescription = "As juntas serão etiquetadas alternadamente.",
                LargeImage = ResourceImage.GetIcon("TagearJuntasLPE.png")
            });

            //////////// RESTORE FLOORS ////////////

            pisosEJuntasPanel.AddItem(new PushButtonData("RestoreFloors", "Restaurar Pisos", executingAssemblyPath, typeof(RestoreFloorsEC).FullName)
            {
                ToolTip = "Restaura os pisos divididos pelo comando \"Dividir Pisos\" ao seu formato original.",
                LongDescription = "O comando utilizará o valor prenchido no parâmetro \"Ambiente\" para identificar quais pisos se unirão.",
                LargeImage = ResourceImage.GetIcon("RestaurarPisosLPE.png")
            });

            //////////// AMBIENTE MANAGER ////////////

            ambientesPanel.AddItem(new PushButtonData("AmbienteManager", "Gerenciador\nde Ambientes", executingAssemblyPath, typeof(AmbienteManagerEC).FullName)
            {
                ToolTip = "O gerenciador de ambientes facilita a gestão dos keyschedules, centralizando as informações.\nEle permite criar, editar, duplicar, importar e deletar ambientes.",
                LongDescription = "",
                LargeImage = ResourceImage.GetIcon("AmbienteManagerLPE.png")
            });

            //////////// ABOUT ////////////

            sobrePanel.AddItem(new PushButtonData("About", "Sobre", executingAssemblyPath, typeof(About).FullName)
            {
                ToolTip = "",
                LongDescription = "",
                LargeImage = ResourceImage.GetIcon("About.png")
            });

        }
    }
}