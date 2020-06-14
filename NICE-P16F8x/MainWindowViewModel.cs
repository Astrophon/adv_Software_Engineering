using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;

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
        public KeyValuePair<int, string>[] ClockSpeedPairs { get; }
        #endregion

        #region Fields with automatic notification to UI
        private string[,] fileRegisterData;
        public string[,] FileRegisterData
        {
            get { return fileRegisterData; }
            set { SetAndNotify(ref fileRegisterData, value, () => FileRegisterData); }
        }
        private ObservableCollection<bool> trisA;
        public ObservableCollection<bool> TrisA
        {
            get { return trisA; }
            set { SetAndNotify(ref trisA, value, () => TrisA); }
        }
        private ObservableCollection<bool> trisB;
        public ObservableCollection<bool> TrisB
        {
            get { return trisB; }
            set { SetAndNotify(ref trisB, value, () => TrisB); }
        }
        private ObservableCollection<bool> portA;
        public ObservableCollection<bool> PortA
        {
            get { return portA; }
            set { SetAndNotify(ref portA, value, () => PortA); }
        }
        private ObservableCollection<bool> portB;
        public ObservableCollection<bool> PortB
        {
            get { return portB; }
            set { SetAndNotify(ref portB, value, () => PortB); }
        }
        private ObservableCollection<string> status;
        public ObservableCollection<string> Status
        {
            get { return status; }
            set { SetAndNotify(ref status, value, () => Status); }
        }
        private ObservableCollection<string> option;
        public ObservableCollection<string> Option
        {
            get { return option; }
            set { SetAndNotify(ref option, value, () => Option); }
        }
        private ObservableCollection<string> intcon;
        public ObservableCollection<string> Intcon
        {
            get { return intcon; }
            set { SetAndNotify(ref intcon, value, () => Intcon); }
        }
        private ObservableCollection<string> sfrvalues;
        public ObservableCollection<string> SFRValues
        {
            get { return sfrvalues; }
            set { SetAndNotify(ref sfrvalues, value, () => SFRValues); }
        }
        private decimal runtime;
        public decimal Runtime
        {
            get { return runtime; }
            set { SetAndNotify(ref runtime, value, () => Runtime); }
        }
        private decimal watchdog;
        public decimal Watchdog
        {
            get { return watchdog; }
            set { SetAndNotify(ref watchdog, value, () => Watchdog); }
        }
        private int clockSpeed;
        public int ClockSpeed
        {
            get { return clockSpeed; }
            set
            {
                SetAndNotify(ref clockSpeed, value, () => ClockSpeed);
                Data.setClockSpeed(clockSpeed);
            }
        }
        private string startStopButtonText;
        public string StartStopButtonText
        {
            get { return startStopButtonText; }
            set { SetAndNotify(ref startStopButtonText, value, () => StartStopButtonText); }
        }
        private string prePostScalerText;
        public string PrePostScalerText
        {
            get { return prePostScalerText; }
            set { SetAndNotify(ref prePostScalerText, value, () => PrePostScalerText); }
        }
        private ObservableCollection<string> stackDisplay;
        public ObservableCollection<string> StackDisplay
        {
            get { return stackDisplay; }
            set { SetAndNotify(ref stackDisplay, value, () => StackDisplay); }
        }
        private bool watchdogEnabled;
        public bool WatchdogEnabled
        {
            get { return watchdogEnabled; }
            set
            {
                SetAndNotify(ref watchdogEnabled, value, () => WatchdogEnabled);
                Data.setWatchdogEnabled(watchdogEnabled);
            }
        }
        #endregion

        public MainWindowViewModel()
        {
            FileRegisterColumns = new string[] { "+0", "+1", "+2", "+3", "+4", "+5", "+6", "+7" };
            FileRegisterRows = new string[] { "00", "08", "10", "18", "20", "28", "30", "38", "40", "48", "50", "58", "60", "68", "70", "78", "80", "88", "90", "98", "A0", "A8", "B0", "B8", "C0", "C8", "D0", "D8", "E0", "E8", "F0", "F8" };
            FileRegisterData = new string[32, 8];
            ClockSpeedPairs = new KeyValuePair<int, string>[]{
                new KeyValuePair<int, string>(4000000, "4 MHz"),
                new KeyValuePair<int, string>(1000000, "1 MHz"),
                new KeyValuePair<int, string>(50000, "50 KHz"),
                new KeyValuePair<int, string>(10000, "10 KHz"),
            };
            trisA = new ObservableCollection<bool>(new bool[8]);
            trisB = new ObservableCollection<bool>(new bool[8]);
            portA = new ObservableCollection<bool>(new bool[8]);
            portB = new ObservableCollection<bool>(new bool[8]);
            status = new ObservableCollection<string>(Enumerable.Repeat("0", 8).ToList());
            option = new ObservableCollection<string>(Enumerable.Repeat("0", 8).ToList());
            intcon = new ObservableCollection<string>(Enumerable.Repeat("0", 8).ToList());
            stackDisplay = new ObservableCollection<string>(Enumerable.Repeat("0000", 8).ToList());
            sfrvalues = new ObservableCollection<string>(Enumerable.Repeat("00", 10).ToList());
            runtime = 0;
            watchdog = 0; 
            WatchdogEnabled = false;
        }
    }
}
