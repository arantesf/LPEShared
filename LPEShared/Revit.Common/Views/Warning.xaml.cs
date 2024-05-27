using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Revit.Common
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class WarningMVVM : Window
    {
        public WarningViewModel WarningViewModel { get; set; } = new WarningViewModel();

        public WarningMVVM(string text)
        {
            InitializeComponent();
            this.DataContext = this;
            WarningViewModel.Message = text;
        }
        private void Close_Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
    public class WarningViewModel : INotifyPropertyChanged
    {
        private string _Message;
        public string Message
        {
            get { return _Message; }
            set
            {
                _Message = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged();
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
