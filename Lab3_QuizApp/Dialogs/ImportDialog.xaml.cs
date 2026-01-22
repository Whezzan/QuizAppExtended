using System.Windows;
using System.Windows.Controls;

namespace Lab3_QuizApp.Views
{
    public partial class ImportDialog : Window
    {
        public int Amount { get; private set; } = 10;
        public string Category { get; private set; } = "";
        public string Difficulty { get; private set; } = "";

        public ImportDialog()
        {
            InitializeComponent();
            CategoryComboBox.SelectedIndex = 0;
            DifficultyComboBox.SelectedIndex = 0;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(AmountTextBox.Text, out int amount) || amount <= 0)
            {
                MessageBox.Show("Ange ett giltigt antal frågor (större än 0).", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Amount = amount;
            Category = (CategoryComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            Difficulty = (DifficultyComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}