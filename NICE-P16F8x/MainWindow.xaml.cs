using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NICE_P16F8x
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SourceFile sourceFile;
        public static string[,] UIFileRegisterData;
        private MainWindowViewModel view;

        public MainWindow()
        {
            view = new MainWindowViewModel();
            DataContext = view;
            InitializeComponent();

            UpdateFileRegisterUI();
        }

        private void menuOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog()
            {
                Title = "Open Source File",
                Filter = "LST files (*.LST)|*.LST",
                RestoreDirectory = true,
            };

            if (dialog.ShowDialog() == true)
            {
                sourceFile = new SourceFile(dialog.FileName);
                SourceDataGrid.ItemsSource = sourceFile.getSourceLines();
                SelectSourceLineFromPC(0);
            }
        }

        private void SelectSourceLineFromPC(int pc)
        {
            if (sourceFile.getSourceLineFromPC(pc) >= 0 )
            {
                UIHelper.SelectRowByIndexes(SourceDataGrid, sourceFile.getSourceLineFromPC(pc));
            }
        }

        private void SourceDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void SourceDataGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void UpdateFileRegisterUI()
        {
            view.FileRegisterData = GetFileRegisterDataUI();
            view.OnPropertyChanged(nameof(view.FileRegisterData));
        }

        private string[,] GetFileRegisterDataUI()
        {
            string[,] data = new string[32, 8];

            int index = 0;
            for (int i = 0; i < 32; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    data[i, j] = Data.getAllRegisters()[index++].ToString();                    
                }
            }
            return data;
        }
    }
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public string[] FileRegisterColumns { get; set; }
        public string[] FileRegisterRows { get; }
        public string[,] FileRegisterData { get; set; }

        public MainWindowViewModel()
        {
            this.FileRegisterColumns = new string[] { "+0", "+1", "+2", "+3", "+4", "+5", "+6", "+7" };
            this.FileRegisterRows = new string[] { "00", "08", "10", "18", "20", "28", "30", "38", "40", "48", "50", "58", "60", "68", "70", "78", "80", "88", "90", "98", "A0", "A8", "B0", "B8", "C0", "C8", "D0", "D8", "E0", "E8", "F0", "F8" };
            this.FileRegisterData = new string[32,8];
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }
}
