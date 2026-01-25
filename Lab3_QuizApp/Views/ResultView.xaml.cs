using QuizAppExtended.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace QuizAppExtended.Views
{
    /// <summary>
    /// Interaction logic for ResultView.xaml
    /// </summary>
    public partial class ResultView : UserControl
    {
        public ResultView()
        {
            InitializeComponent();
        }

        private async void OnPlayClick(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow?.DataContext is not MainWindowViewModel mainVm)
            {
                return;
            }

            CommitAllTextBoxes(Application.Current.MainWindow);

            await mainVm.ConfigurationViewModel.FlushAutoSaveAsync();

            if (mainVm.PlayerViewModel.SwitchToPlayModeCommand.CanExecute(null))
            {
                mainVm.PlayerViewModel.SwitchToPlayModeCommand.Execute(null);
            }

            e.Handled = true;
        }

        private static void CommitAllTextBoxes(DependencyObject root)
        {
            if (root == null)
            {
                return;
            }

            var count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);

                if (child is TextBox tb)
                {
                    var expr = tb.GetBindingExpression(TextBox.TextProperty);
                    expr?.UpdateSource();
                }

                CommitAllTextBoxes(child);
            }
        }
    }
}
