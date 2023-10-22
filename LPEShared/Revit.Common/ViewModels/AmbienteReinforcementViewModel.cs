using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Revit.Common
{
    public class AmbienteAndReinforcementViewModel : ViewModelBase
    {
        private string name;

        public string Name
        {
            get { return name; }
            set 
            { 
                name = value; 
                OnPropertyChanged();
            }
        }

        private ObservableCollection<KeyScheduleElementViewModel> reinforcementsViewModels = new ObservableCollection<KeyScheduleElementViewModel>();
        public ObservableCollection<KeyScheduleElementViewModel> ReinforcementsViewModels
        {
            get { return reinforcementsViewModels; }
            set
            {
                reinforcementsViewModels = value;
                OnPropertyChanged();
            }
        }

        private KeyScheduleElementViewModel selectedReinforcement = null;
        public KeyScheduleElementViewModel SelectedReinforcement
        {
            get { return selectedReinforcement; }
            set
            {
                selectedReinforcement = value;
                OnPropertyChanged();
            }
        }

        public AmbienteAndReinforcementViewModel(string name, List<Element> reinforcements)
        {
            Name = name;
            List<KeyScheduleElementViewModel> keyScheduleElementViewModelsList = new List<KeyScheduleElementViewModel>();
            foreach (var reinforcement in reinforcements)
            {
                keyScheduleElementViewModelsList.Add(new KeyScheduleElementViewModel(reinforcement));
            }
            keyScheduleElementViewModelsList.Add(new KeyScheduleElementViewModel(null));
            keyScheduleElementViewModelsList = keyScheduleElementViewModelsList.OrderBy(x => x.Name).ToList();
            foreach (var viewModel in keyScheduleElementViewModelsList)
            {
                ReinforcementsViewModels.Add(viewModel);
            }
            SelectedReinforcement = reinforcementsViewModels.First();
        }
    }
}
