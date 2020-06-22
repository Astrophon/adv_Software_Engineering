using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace NICE_P16F8x
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SourceFile SourceFile;
        private readonly MainWindowViewModel View;
        private readonly Timer StepTimer;
        private DateTime LastRegUpdate;
        private bool OutOfBoundsMessageShown = false;

        public MainWindow()
        {
            //Set DataContext for UI
            View = new MainWindowViewModel();
            DataContext = View;

            StepTimer = new Timer(View.SimSpeed);  //Time between steps in running mode
            StepTimer.AutoReset = true;
            StepTimer.Elapsed += new ElapsedEventHandler(OnRunTimerEvent);
            View.PropertyChanged += UpdateTimerInterval;

            //Init UI
            InitializeComponent();

            //Initial data reset / refresh
            Reset();
        }

        #region User interaction logic functions

        private void Start()
        {
            if (Data.IsProgramInitialized())
            {
                FileRegister.IsReadOnly = true;
                StepTimer.Start();
            }
        }
        private void Stop()
        {
            StepTimer.Stop();
            Dispatcher.Invoke(() =>
            {
                FileRegister.IsReadOnly = false;
            });
        }
        private void StopAndUpdateUI()
        {
            Stop();
            Dispatcher.Invoke(() =>
            {
                UpdateUI();
            });
        }
        private void Reset()
        {
            Stop();
            Data.Reset();
            UpdateUI();
        }

        /// <summary>
        /// Handles user hex edit in fileregister view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileRegister_CellEditEnding(object sender, System.Windows.Controls.DataGridCellEditEndingEventArgs e)
        {
            var editingTextBox = e.EditingElement as TextBox;
            string newValue = editingTextBox.Text;

            if (e.EditAction == DataGridEditAction.Commit)
            {
                if (newValue.Length <= 2)
                {
                    try
                    {
                        byte b = (byte)int.Parse(newValue, System.Globalization.NumberStyles.HexNumber);
                        editingTextBox.Text = b.ToString("X2");
                        int row = e.Row.GetIndex();
                        int column = e.Column.DisplayIndex;
                        Data.SetRegister((byte)(row * 8 + column), b);
                        UpdateUI();
                        if (row > 0)
                        {
                            FileRegister.CurrentCell = new DataGridCellInfo(FileRegister.Items[row - 1], FileRegister.Columns[column]);
                        }
                    }
                    catch
                    {
                        e.Cancel = true;
                        (sender as DataGrid).CancelEdit(DataGridEditingUnit.Cell);
                        MessageBox.Show("Invalid hexadecimal value", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    e.Cancel = true;
                    (sender as DataGrid).CancelEdit(DataGridEditingUnit.Cell);
                    MessageBox.Show("Only one hexadecimal byte allowed", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        #endregion

        #region Menu Items
        /// <summary>
        /// Logic for File -> Open
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog()
            {
                Title = "Open Source File",
                Filter = "LST files (*.LST)|*.LST",
                RestoreDirectory = true,
            };

            if (dialog.ShowDialog() == true)
            {
                Reset();
                SourceFile = new SourceFile(dialog.FileName);
                SourceDataGrid.ItemsSource = SourceFile.GetSourceLines(); //Set new data to LST View
                SourceDataGrid.Columns[4].Width = 0; //Reset comment column width
                SourceDataGrid.UpdateLayout();
                SourceDataGrid.Columns[4].Width = DataGridLength.Auto; //Set new column width automatically according to content
                UpdateUI();
            }
        }
        /// <summary>
        /// Handles user click on Menu -> Help
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuHelp_Click(object sender, RoutedEventArgs e)
        {

        }
        /// <summary>
        /// Debug action for development testing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuDebugAction_Click(object sender, RoutedEventArgs e) // TEST METHOD
        {

        }
        #endregion

        #region Button / Textbox Functions
        private void Button_Step_Click(object sender, RoutedEventArgs e)
        {
            if (Data.IsProgramInitialized())
            {
                ProgramStep();
                UpdateUI();
                CheckOutOfProgramRange();
            }
        }
        private void Button_Reset_Click(object sender, RoutedEventArgs e)
        {
            Reset();
            UpdateUI();
        }
        private void Button_StartStop_Click(object sender, RoutedEventArgs e)
        {
            if (StepTimer.Enabled)
            {
                Stop();
                UpdateUI();
            }
            else
            {
                Start();
            }
        }
        private void TextBox_UpdateSource(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TextBox tBox = (TextBox)sender;
                DependencyProperty prop = TextBox.TextProperty;

                BindingExpression binding = BindingOperations.GetBindingExpression(tBox, prop);
                if (binding != null) { binding.UpdateSource(); }
            }
        }
        private void TextBlock_StatusBitChange(object sender, MouseButtonEventArgs e)
        {
            TextBlock tBlock = (TextBlock)sender;
            int bit = (int)typeof(Data.Flags.Status).GetField(tBlock.Name).GetValue(this);
            Data.ToggleRegisterBit(Data.Registers.STATUS, bit);
            UpdateUI();
        }
        private void TextBlock_OptionBitChange(object sender, MouseButtonEventArgs e)
        {
            TextBlock tBlock = (TextBlock)sender;
            int bit = (int)typeof(Data.Flags.Option).GetField(tBlock.Name).GetValue(this);
            Data.ToggleRegisterBit(Data.Registers.OPTION, bit);
            UpdateUI();
        }
        private void TextBlock_IntconBitChange(object sender, MouseButtonEventArgs e)
        {
            TextBlock tBlock = (TextBlock)sender;
            int bit = (int)typeof(Data.Flags.Intcon).GetField(tBlock.Name).GetValue(this);
            Data.ToggleRegisterBit(Data.Registers.INTCON, bit);
            UpdateUI();

        }
        #endregion

        #region UI Helper Functions
        /// <summary>
        /// Highlights and scrolls to the given pcl
        /// </summary>
        /// <param name="pcl"></param>
        private void HighlightSourceLine(int pcl)
        {
            try
            {
                SourceFile.HighlightLine(pcl);
                SourceDataGrid.ScrollIntoView(SourceDataGrid.Items[6]);
                SourceDataGrid.ScrollIntoView(SourceDataGrid.Items[SourceFile.GetSourceLineIndexFromPC(pcl)]);
            }
            catch (Exception)
            {
            }
        }
        private void OnRunTimerEvent(object source, ElapsedEventArgs e)
        {
            ProgramStep();
            Dispatcher.Invoke(() =>
            {
                if (DateTime.Now.Subtract(LastRegUpdate).TotalSeconds > 1)
                {
                    UpdateUI();
                    LastRegUpdate = DateTime.Now;
                }
                else
                {
                    UpdateUIWithoutFileRegister();
                }
                CheckOutOfProgramRange();
            });
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Stop();
        }
        /// <summary>
        /// Executes one program step. Stops running timer if 1. out of program bounds AND warning message has not yet been shown 2. a breakpoint is hit
        /// </summary>
        private void ProgramStep()
        {
            try
            {
                InstructionProcessor.PCStep();
            }
            catch (IndexOutOfRangeException)
            {
                Stop();
                UpdateUI();
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Tried to get/set an out of range register!\nCheck your code!", "Index out of range");
                });
                return;
            }
            if (IsPCOutOfRange())
            {
                if (!OutOfBoundsMessageShown) StopAndUpdateUI();
            }
            else if (IsBreakpointHit())
            {
                StopAndUpdateUI();
            }
        }
        /// <summary>
        /// Shows a message if the current PC is out of bounds
        /// </summary>
        private void CheckOutOfProgramRange()
        {
            if (IsPCOutOfRange())
            {
                if (!OutOfBoundsMessageShown)
                {
                    OutOfBoundsMessageShown = true;
                    MessageBox.Show("PC has left program area!\nPlease avoid this behavior by ending the code in an infinite loop.", "Out of program bounds", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                }
            }
            else
            {
                OutOfBoundsMessageShown = false;
            }
        }
        private bool IsPCOutOfRange()
        {
            return (Data.GetPC() >= Data.GetProgram().Count);
        }
        public bool IsBreakpointHit()
        {
            return SourceFile.LineHasBreakpoint(Data.GetPC());
        }
        #endregion

        #region UI Update Functions
        /// <summary>
        /// Refreshes data for all UI elements that need converted info from Data class
        /// </summary>
        public void UpdateUI()
        {
            UpdateFileRegisterUI();
            UpdateUIWithoutFileRegister();
        }
        /// <summary>
        /// Refreshes data for all UI elements BUT the file register view
        /// </summary>
        public void UpdateUIWithoutFileRegister()
        {
            View.TrisA = new ObservableCollection<bool>(Data.ByteToBoolArray(Data.GetRegister(Data.Registers.TRISA)));
            View.TrisB = new ObservableCollection<bool>(Data.ByteToBoolArray(Data.GetRegister(Data.Registers.TRISB)));
            View.PortA = new ObservableCollection<bool>(Data.ByteToBoolArray(Data.GetRegister(Data.Registers.PORTA)));
            View.PortB = new ObservableCollection<bool>(Data.ByteToBoolArray(Data.GetRegister(Data.Registers.PORTB)));
            View.TrisA.CollectionChanged += new NotifyCollectionChangedEventHandler(TrisAChanged);
            View.TrisB.CollectionChanged += new NotifyCollectionChangedEventHandler(TrisBChanged);
            View.PortA.CollectionChanged += new NotifyCollectionChangedEventHandler(PortAChanged);
            View.PortB.CollectionChanged += new NotifyCollectionChangedEventHandler(PortBChanged);
            View.Status = new ObservableCollection<string>(Data.ByteToStringArray(Data.GetRegister(Data.Registers.STATUS)));
            View.Option = new ObservableCollection<string>(Data.ByteToStringArray(Data.GetRegister(Data.Registers.OPTION)));
            View.Intcon = new ObservableCollection<string>(Data.ByteToStringArray(Data.GetRegister(Data.Registers.INTCON)));
            View.StackDisplay = new ObservableCollection<string>(Data.GetStack().Select(x => x.ToString("D4")).ToArray());
            View.SFRValues[0] = Data.GetRegisterW().ToString("X2");
            View.SFRValues[1] = Data.GetRegister(Data.Registers.PCL).ToString("X2");
            View.SFRValues[2] = Data.GetRegister(Data.Registers.PCLATH).ToString("X2");
            View.SFRValues[3] = Data.GetPC().ToString("D2");
            View.SFRValues[4] = Data.GetRegister(Data.Registers.STATUS).ToString("X2");
            View.SFRValues[5] = Data.GetRegister(Data.Registers.FSR).ToString("X2");
            View.SFRValues[6] = Data.GetRegister(Data.Registers.OPTION).ToString("X2");
            View.SFRValues[7] = Data.GetRegister(Data.Registers.TMR0).ToString("X2");
            View.SFRValues[8] = "1:" + Data.GetPrePostscalerRatio();

            if (Data.GetRegisterBit(Data.Registers.OPTION, Data.Flags.Option.PSA)) View.PrePostScalerText = "Postscaler"; //Postscaler assigned to WDT
            else View.PrePostScalerText = "Prescaler"; //Prescaler assigned to TMR0

            if (StepTimer.Enabled) View.StartStopButtonText = "Stop";
            else View.StartStopButtonText = "Start";


            if (Data.GetPC() < Data.GetProgram().Count)
            {
                View.SFRValues[9] = Data.InstructionLookup(Data.GetProgram()[Data.GetPC()]).ToString();
            }
            else
            {
                View.SFRValues[9] = "N/A";
            }

            View.Runtime = Data.GetRuntime();
            View.Watchdog = Data.GetWatchdog();
            if (SourceFile != null)
            {
                HighlightSourceLine(Data.GetPC());
            }
        }
        private void UpdateFileRegisterUI()
        {
            string[,] data = new string[32, 8];

            int index = 0;
            for (int i = 0; i < 32; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    data[i, j] = Data.GetAllRegisters()[index++].ToString("X2");
                }
            }
            View.FileRegisterData = data;
        }

        private void UpdateTimerInterval(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SimSpeed")
            {
                StepTimer.Interval = View.SimSpeed;
            }
        }
        #endregion

        #region Checkbox ChangedEventHandlers
        private void TrisAChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Data.SetRegister(Data.Registers.TRISA, Data.BoolArrayToByte(View.TrisA.ToArray<bool>()));
            UpdateUI();
        }
        private void TrisBChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Data.SetRegister(Data.Registers.TRISB, Data.BoolArrayToByte(View.TrisB.ToArray<bool>()));
            UpdateUI();
        }
        private void PortAChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Data.SetRegister(Data.Registers.PORTA, Data.BoolArrayToByte(View.PortA.ToArray<bool>()));
            UpdateUI();
        }
        private void PortBChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Data.SetRegister(Data.Registers.PORTB, Data.BoolArrayToByte(View.PortB.ToArray<bool>()));
            UpdateUI();
        }
        #endregion
    }
}
