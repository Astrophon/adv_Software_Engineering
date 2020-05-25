using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NICE_P16F8x
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SourceFile SourceFile;
        private MainWindowViewModel View;

        public MainWindow()
        {
            //Set DataContext for UI
            View = new MainWindowViewModel();
            DataContext = View;
            //Initial data refresh
            UpdateUI();

            InitializeComponent();
        }

        #region User interaction logic functions
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
                        Data.setRegister((byte)(row * 8 + column), b);
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
                SourceFile = new SourceFile(dialog.FileName);
                SourceDataGrid.ItemsSource = SourceFile.getSourceLines();
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
        private void MenuDebugAction_Click(object sender, RoutedEventArgs e) // TEST METHOD FOR NOW
        {
            Data.Command com = Data.getProgram()[Data.getPC()];
            InstructionProcessor.Execute(Data.InstructionLookup(com), com);
            Data.IncPC();
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
                SourceDataGrid.ScrollIntoView(SourceDataGrid.Items[SourceFile.getSourceLineIndexFromPCL(pcl)]);
            }
            catch (ArgumentOutOfRangeException) { }
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
            if(SourceFile != null)
            {
                HighlightSourceLine(Data.getPC());
            }
        }
        /// <summary>
        /// Refreshes data for all UI elements BUT the file register view
        /// </summary>
        public void UpdateUIWithoutFileRegister()
        {
            View.TrisA = new ObservableCollection<bool>(Data.ByteToBoolArray(Data.getRegister(Data.Registers.TRISA)));
            View.TrisB = new ObservableCollection<bool>(Data.ByteToBoolArray(Data.getRegister(Data.Registers.TRISB)));
            View.PortA = new ObservableCollection<bool>(Data.ByteToBoolArray(Data.getRegister(Data.Registers.PORTA)));
            View.PortB = new ObservableCollection<bool>(Data.ByteToBoolArray(Data.getRegister(Data.Registers.PORTB)));
            View.TrisA.CollectionChanged += new NotifyCollectionChangedEventHandler(TrisAChanged);
            View.TrisB.CollectionChanged += new NotifyCollectionChangedEventHandler(TrisBChanged);
            View.PortA.CollectionChanged += new NotifyCollectionChangedEventHandler(PortAChanged);
            View.PortB.CollectionChanged += new NotifyCollectionChangedEventHandler(PortBChanged);
            View.Status = new ObservableCollection<string>(Data.ByteToStringArray(Data.getRegister(Data.Registers.STATUS)));
            View.Option = new ObservableCollection<string>(Data.ByteToStringArray(Data.getRegister(Data.Registers.OPTION)));
            View.Intcon = new ObservableCollection<string>(Data.ByteToStringArray(Data.getRegister(Data.Registers.INTCON)));
        }
        private void UpdateFileRegisterUI()
        {
            string[,] data = new string[32, 8];

            int index = 0;
            for (int i = 0; i < 32; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    data[i, j] = Data.getAllRegisters()[index++].ToString("X2");
                }
            }
            View.FileRegisterData = data;
        }
        #endregion

        #region Checkbox ChangedEventHandlers
        private void TrisAChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Data.setRegister(Data.Registers.TRISA, Data.BoolArrayToByte(View.TrisA.ToArray<bool>()));
            UpdateUI();
        }
        private void TrisBChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Data.setRegister(Data.Registers.TRISB, Data.BoolArrayToByte(View.TrisB.ToArray<bool>()));
            UpdateUI();
        }
        private void PortAChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Data.setRegister(Data.Registers.PORTA, Data.BoolArrayToByte(View.PortA.ToArray<bool>()));
            UpdateUI();
        }
        private void PortBChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Data.setRegister(Data.Registers.PORTB, Data.BoolArrayToByte(View.PortB.ToArray<bool>()));
            UpdateUI();
        }
        #endregion
    }
}
