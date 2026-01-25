using QuizAppExtended.ViewModels;
using System.Windows;


namespace QuizAppExtended.Dialogs
{
    public partial class PackOptionsDialog : Window
    {
        public PackOptionsDialog()
        {
            InitializeComponent();

            Closed += async (_, _) =>
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    await vm.SaveToMongoAsync();
                }
            };
        }
    }
}
