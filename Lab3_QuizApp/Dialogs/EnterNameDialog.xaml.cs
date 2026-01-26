using QuizAppExtended.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace QuizAppExtended.Dialogs
{
    public partial class EnterNameDialog : Window
    {
        public string PlayerName { get; private set; } = string.Empty;

        public QuestionPackViewModel? SelectedPack { get; private set; }

        public EnterNameDialog(IEnumerable<QuestionPackViewModel> packs, QuestionPackViewModel? preselectedPack = null)
        {
            InitializeComponent();

            PackComboBox.ItemsSource = packs?.ToList() ?? new List<QuestionPackViewModel>();
            PackComboBox.SelectedItem = preselectedPack ?? PackComboBox.Items.OfType<QuestionPackViewModel>().FirstOrDefault();

            Loaded += (_, _) => NameTextBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            PlayerName = (NameTextBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(PlayerName))
            {
                MessageBox.Show("Name cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedPack = PackComboBox.SelectedItem as QuestionPackViewModel;
            if (SelectedPack is null)
            {
                MessageBox.Show("Please select a question pack.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
