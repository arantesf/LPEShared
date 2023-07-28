using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;

namespace Revit.Common
{
    public class ProgressBarViewModel : INotifyPropertyChanged
    {
        private bool blueProgressBar = true;

        public bool BlueProgressBar
        {
            get { return blueProgressBar; }
            set { blueProgressBar = value; }
        }

        private int progressBarMaxValue = 2;

        public int ProgressBarMaxValue
        {
            get { return progressBarMaxValue; }
            set
            {
                progressBarMaxValue = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged();
            }
        }
        private int progressBarValue;

        public int ProgressBarValue
        {
            get { return progressBarValue; }
            set
            {
                progressBarValue = value;
                // Call OnPropertyChanged whenever the property is updated
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

            System.Windows.Forms.Application.DoEvents();
            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
        }
    }
}
