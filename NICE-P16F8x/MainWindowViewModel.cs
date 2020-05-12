using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace NICE_P16F8x
{
    /// <summary>
    /// Data Model for UI, contains fields for all data shown on the UI
    /// </summary>
    class MainWindowViewModel : ObservableObject
    {
        #region Static non-changing fields
        public string[] FileRegisterColumns { get; }
        public string[] FileRegisterRows { get; }
        #endregion

        #region Fields with automatic notification to UI
        private string[,] fileRegisterData;
        public string[,] FileRegisterData
        {
            get { return this.fileRegisterData; }
            set { this.SetAndNotify(ref this.fileRegisterData, value, () => this.FileRegisterData); }
        }
        private ObservableCollection<bool> trisA;
        public ObservableCollection<bool> TrisA
        {
            get { return this.trisA; }
            set { this.SetAndNotify(ref this.trisA, value, () => this.TrisA); }
        }
        private ObservableCollection<bool> trisB;
        public ObservableCollection<bool> TrisB
        {
            get { return this.trisB; }
            set { this.SetAndNotify(ref this.trisB, value, () => this.TrisB); }
        }
        private ObservableCollection<bool> portA;
        public ObservableCollection<bool> PortA
        {
            get { return this.portA; }
            set { this.SetAndNotify(ref this.portA, value, () => this.PortA); }
        }
        private ObservableCollection<bool> portB;
        public ObservableCollection<bool> PortB
        {
            get { return this.portB; }
            set { this.SetAndNotify(ref this.portB, value, () => this.PortB); }
        }

        #endregion

        public MainWindowViewModel()
        {
            this.FileRegisterColumns = new string[] { "+0", "+1", "+2", "+3", "+4", "+5", "+6", "+7" };
            this.FileRegisterRows = new string[] { "00", "08", "10", "18", "20", "28", "30", "38", "40", "48", "50", "58", "60", "68", "70", "78", "80", "88", "90", "98", "A0", "A8", "B0", "B8", "C0", "C8", "D0", "D8", "E0", "E8", "F0", "F8" };
            this.FileRegisterData = new string[32, 8];
            this.trisA = new ObservableCollection<bool>(new bool[8]);
            this.trisB = new ObservableCollection<bool>(new bool[8]);
            this.portA = new ObservableCollection<bool>(new bool[8]);
            this.portB = new ObservableCollection<bool>(new bool[8]);
        }

    }
}
