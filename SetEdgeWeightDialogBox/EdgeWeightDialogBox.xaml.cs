using System;
using System.Windows;
using System.Windows.Input;


namespace SetEdgeWeightDialogBox
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class EdgeWeightDialogBox : Window
    {
        public int weight;
        public bool result;

        public EdgeWeightDialogBox()
        {
            InitializeComponent();
            edgeWeightTextBox.Focus();
            edgeWeightTextBox.Text = "1";
            edgeWeightTextBox.CaretIndex = edgeWeightTextBox.Text.Length;
        }

        /// <summary>
        /// Checks if user has entered a correct edge weight.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSetWeight_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(edgeWeightTextBox.Text, out weight) || weight <= 0 || weight > 100)
                MessageBox.Show("incorrect weight", "error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            else
            {
                result = true;
                this.Close();
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                buttonSetWeight_Click(this, new RoutedEventArgs());
        }
    }
}
