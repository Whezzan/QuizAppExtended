using Lab3_QuizApp.Command;
using Lab3_QuizApp.Models;

namespace Lab3_QuizApp.ViewModels
{
    internal class ConfigurationViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel? mainWindowViewModel;
        public QuestionPackViewModel? ActivePack { get => mainWindowViewModel.ActivePack; }
        private string FilePath { get => mainWindowViewModel.FilePath; }


        private bool _deleteQuestionIsEnable;
        public bool DeleteQuestionIsEnable
        {
            get => _deleteQuestionIsEnable;
            set
            {
                _deleteQuestionIsEnable = value;
                RaisePropertyChanged();
            }
        }

        private bool _isConfigurationModeVisible;
        public bool IsConfigurationModeVisible
        {
            get => _isConfigurationModeVisible;
            set
            {
                _isConfigurationModeVisible = value;
                RaisePropertyChanged();
            }
        }


        private Question? _selectedQuestion;
        public Question? SelectedQuestion
        {
            get => _selectedQuestion;
            set
            {
                _selectedQuestion = value;
                DeleteQuestionCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged();
                ChangeTextVisibility();
            }
        }

        private bool _textVisibility;
        public bool TextVisibility
        {
            get => _textVisibility;
            set
            {
                _textVisibility = value;
                RaisePropertyChanged();
            }
        }


        public event EventHandler EditPackOptionsRequested;

        public DelegateCommand AddQuestionCommand { get; }
        public DelegateCommand DeleteQuestionCommand { get; }
        public DelegateCommand EditPackOptionsCommand { get; }
        public DelegateCommand SwitchToConfigurationModeCommand { get; }


        public ConfigurationViewModel(MainWindowViewModel? mainWindowViewModel)
        {
            this.mainWindowViewModel = mainWindowViewModel;

            DeleteQuestionIsEnable = false;
            IsConfigurationModeVisible = true;

            AddQuestionCommand = new DelegateCommand(AddQuestion, IsAddQuestionEnable);
            DeleteQuestionCommand = new DelegateCommand(DeleteQuestion, IsDeleteQuestionEnable);
            EditPackOptionsCommand = new DelegateCommand(EditPackOptions, IsEditPackOptionsEnable);
            SwitchToConfigurationModeCommand = new DelegateCommand(StartConfigurationMode, IsStartConfigurationModeEnable);

            SelectedQuestion = ActivePack?.Questions.FirstOrDefault();
            TextVisibility = ActivePack?.Questions.Count > 0;
        }

        private void AddQuestion(object? obj)
        {
            ActivePack?.Questions.Add(new Question("New Question", string.Empty, string.Empty, string.Empty, string.Empty));

            SelectedQuestion = (ActivePack?.Questions.Count > 0)
                ? ActivePack?.Questions.Last()
                : ActivePack?.Questions.FirstOrDefault();

            UpdateCommandStates();
            ChangeTextVisibility();
            mainWindowViewModel.SaveToJsonAsync();
        }

        private bool IsAddQuestionEnable(object? obj) => IsConfigurationModeVisible;

        private void DeleteQuestion(object? obj)
        {
            ActivePack?.Questions.Remove(SelectedQuestion);
            UpdateCommandStates();
            ChangeTextVisibility();
            mainWindowViewModel.SaveToJsonAsync();
        }

        private bool IsDeleteQuestionEnable(object? obj)
            => IsConfigurationModeVisible && SelectedQuestion != null && (DeleteQuestionIsEnable = (ActivePack != null && ActivePack?.Questions.Count > 0));

        private void EditPackOptions(object? obj)
        {
            EditPackOptionsRequested.Invoke(this, EventArgs.Empty);
            mainWindowViewModel.SaveToJsonAsync();
        }

        private bool IsEditPackOptionsEnable(object? obj) => IsConfigurationModeVisible;

        private void ChangeTextVisibility()
            => TextVisibility = ActivePack?.Questions.Count > 0 && SelectedQuestion != null;

        private void StartConfigurationMode(object? obj)
        {
            mainWindowViewModel.PlayerViewModel._timer.Stop();

            IsConfigurationModeVisible = true;
            mainWindowViewModel.PlayerViewModel.IsPlayerModeVisible = false;
            mainWindowViewModel.PlayerViewModel.IsResultModeVisible = false;
            UpdateCommandStates();
        }

        private bool IsStartConfigurationModeEnable(object? obj) => IsConfigurationModeVisible ? false : true;

        private void UpdateCommandStates()
        {
            AddQuestionCommand.RaiseCanExecuteChanged();
            DeleteQuestionCommand.RaiseCanExecuteChanged();
            EditPackOptionsCommand.RaiseCanExecuteChanged();
            mainWindowViewModel.PlayerViewModel.SwitchToPlayModeCommand.RaiseCanExecuteChanged();
        }

    }
}



