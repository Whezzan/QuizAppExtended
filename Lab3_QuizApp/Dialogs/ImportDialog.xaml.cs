using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QuizAppExtended.Models;
using QuizAppExtended.ViewModels;

namespace QuizAppExtended.Views
{
    public partial class ImportDialog : Window
    {
        public int Amount { get; private set; } = 10;
        public string Category { get; private set; } = "";
        public string Difficulty { get; private set; } = "";
        public string? CategoryId { get; private set; }
        public string? CategoryName { get; private set; }
        public string PackName { get; private set; } = "Imported Trivia Pack";

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

            var packName = (PackNameTextBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(packName))
            {
                MessageBox.Show("Ange ett giltigt namn för question pack.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PackName = packName;
            Amount = amount;

            if (DataContext is MainWindowViewModel && CategoryComboBox.SelectedItem is TriviaCategory selected)
            {
                CategoryId = selected.Id;
                Category = selected.OpenTdbId ?? "";
                CategoryName = selected.Name;
            }
            else
            {
                if (CategoryComboBox.SelectedItem is ComboBoxItem item)
                {
                    Category = item.Tag?.ToString() ?? "";
                    CategoryName = item.Content?.ToString();
                }
                else
                {
                    Category = "";
                    CategoryName = null;
                }
            }

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