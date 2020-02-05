using System;
using System.Windows;
using System.Windows.Xps.Packaging;
using System.IO;

namespace Help
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();

            XpsDocument doc = null;
            try
            {
                doc = new XpsDocument("help.xps", FileAccess.Read);
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("Help file not found", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            docViewer.Document = doc.GetFixedDocumentSequence();
        }
    }
}
