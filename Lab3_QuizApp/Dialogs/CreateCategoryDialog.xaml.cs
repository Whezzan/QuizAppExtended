using System;
using System.Windows;

namespace QuizAppExtended.Dialogs
{
    /// <summary>
    /// Interaction logic for CreateCategoryDialog.xaml
    /// </summary>
    public partial class CreateCategoryDialog : Window
    {
        public string CategoryName { get; private set; } = string.Empty;

        public CreateCategoryDialog()
        {
            InitializeComponent();
            Loaded += (_, _) => NameTextBox.Focus();
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            CategoryName = (NameTextBox.Text ?? string.Empty).Trim();
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
