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
using QuizAppExtended.ViewModels;

namespace QuizAppExtended.Dialogs
{
    /// <summary>
    /// Interaction logic for DeleteQuestionPacksDialog.xaml
    /// </summary>
    public partial class DeleteQuestionPacksDialog : Window
    {
        public List<QuestionPackViewModel> SelectedPacks { get; private set; } = new List<QuestionPackViewModel>();

        public DeleteQuestionPacksDialog(IEnumerable<QuestionPackViewModel> packs)
        {
            InitializeComponent();
            PacksListBox.ItemsSource = packs.OrderBy(p => p.Name).ToList();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            SelectedPacks = PacksListBox.SelectedItems.Cast<QuestionPackViewModel>().ToList();
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
