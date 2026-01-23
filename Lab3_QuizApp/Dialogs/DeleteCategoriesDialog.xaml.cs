using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using QuizAppExtended.Models;

namespace QuizAppExtended.Dialogs
{
    /// <summary>
    /// Interaction logic for DeleteCategoriesDialog.xaml
    /// </summary>
    public partial class DeleteCategoriesDialog : Window
    {
        public List<TriviaCategory> SelectedCategories { get; private set; } = new List<TriviaCategory>();

        public DeleteCategoriesDialog(IEnumerable<TriviaCategory> categories)
        {
            InitializeComponent();
            CategoriesListBox.ItemsSource = categories.OrderBy(c => c.Name).ToList();
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            SelectedCategories = CategoriesListBox.SelectedItems.Cast<TriviaCategory>().ToList();
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
