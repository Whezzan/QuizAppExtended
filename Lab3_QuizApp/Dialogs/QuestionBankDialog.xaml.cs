using System.Windows;
using System.Windows.Controls;
using QuizAppExtended.Models;
using QuizAppExtended.ViewModels;

namespace QuizAppExtended.Dialogs
{
    public partial class QuestionBankDialog : Window
    {
        public QuestionBankDialog()
        {
            InitializeComponent();
        }

        private void OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is not MainWindowViewModel vm)
            {
                return;
            }

            if (e.NewValue is Question q)
            {
                vm.SelectedBankQuestion = q;
            }
            else
            {
                vm.SelectedBankQuestion = null;
            }

            vm.AddQuestionFromBankCommand.RaiseCanExecuteChanged();
        }
    }
}
