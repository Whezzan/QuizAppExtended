using QuizAppExtended.Dialogs;
using QuizAppExtended.ViewModels;
using System.Windows;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace QuizAppExtended
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel? mainWindowViewModel;
        private Window? _currentDialog;

        public MainWindow()

        {
            InitializeComponent();
            mainWindowViewModel = new MainWindowViewModel();
            DataContext = mainWindowViewModel;

            mainWindowViewModel.CloseDialogRequested += OnCloseDialogRequested;
            mainWindowViewModel.ConfigurationViewModel.EditPackOptionsRequested += OnShowOptionsDialogRequested;
            mainWindowViewModel.ExitGameRequested += OnExitRequested;
            mainWindowViewModel.OpenNewPackDialogRequested += OnShowPackDialogRequested;
            mainWindowViewModel.ToggleFullScreenRequested += OnToggleFullScreenRequested;

            // New: QuestionBank dialog hook
            mainWindowViewModel.OpenQuestionBankDialogRequested += OnShowQuestionBankDialogRequested;
        }

        public void OnExitRequested(object? obj, bool canExit)
        {
            if (canExit)
            {
                var result = MessageBox.Show("Are you sure you want to exit the game?", "Exit Game?",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    Application.Current.MainWindow.Close();
                }
            }
        }

        public void OnCloseDialogRequested(object? sender, EventArgs args)
        {
            if (_currentDialog != null)
            {
                _currentDialog.Close();
                _currentDialog = null;
            }
        }

        public void OnShowOptionsDialogRequested(object? sender, EventArgs args)
        {
            var dialog = new PackOptionsDialog();
            ShowDialog(dialog);
        }

        public void OnShowPackDialogRequested(object? sender, EventArgs args)
        {
            var dialog = new CreateNewPackDialog();
            ShowDialog(dialog);
        }

        public void OnToggleFullScreenRequested(object? sender, bool isFullscreen)
        {
            if (isFullscreen)
            {
                this.WindowState = WindowState.Maximized;
                this.WindowStyle = WindowStyle.None;
            }
            else
            {
                this.WindowState = WindowState.Normal;
                this.WindowStyle = WindowStyle.SingleBorderWindow;
            }
        }

        public void OnShowQuestionBankDialogRequested(object? sender, EventArgs args)
        {
            var dialog = new QuestionBankDialog();
            ShowDialog(dialog);
        }

        public void ShowDialog(Window dialog)
        {
            try
            {
                dialog.DataContext = mainWindowViewModel;
                dialog.Owner = Application.Current.MainWindow;
                _currentDialog = dialog;
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while opening the dialog box: {ex.Message}");
            }
        }
        private static void CommitFocusedTextBox()
        {
            if (Keyboard.FocusedElement is not DependencyObject depObj)
            {
                return;
            }

            var expr = BindingOperations.GetBindingExpression(depObj, TextBox.TextProperty);
            expr?.UpdateSource();
        }
    }
}