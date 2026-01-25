using QuizAppExtended.ViewModels;
using System.Windows.Controls;


namespace QuizAppExtended.Views
{
    public partial class ConfigurationView : UserControl
    {
        public ConfigurationView()
        {
            InitializeComponent();
        }

        private void OnQuestionFieldChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is ConfigurationViewModel vm)
            {
                vm.ScheduleAutoSave();
            }
        }
    }
}
