using QuizAppExtended.Command;
using QuizAppExtended.Models;
using System.Linq;

namespace QuizAppExtended.ViewModels
{
    internal class ConfigurationViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel mainWindowViewModel;
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


        public event EventHandler? EditPackOptionsRequested;

        public DelegateCommand AddQuestionCommand { get; }
        public DelegateCommand DeleteQuestionCommand { get; }
        public DelegateCommand EditPackOptionsCommand { get; }
        public DelegateCommand SwitchToConfigurationModeCommand { get; }


        public ConfigurationViewModel(MainWindowViewModel mainWindowViewModel)
        {
            this.mainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));

            DeleteQuestionIsEnable = false;
            IsConfigurationModeVisible = true;

            AddQuestionCommand = new DelegateCommand(AddQuestion, IsAddQuestionEnable);
            DeleteQuestionCommand = new DelegateCommand(DeleteQuestion, IsDeleteQuestionEnable);
            EditPackOptionsCommand = new DelegateCommand(EditPackOptions, IsEditPackOptionsEnable);
            SwitchToConfigurationModeCommand = new DelegateCommand(StartConfigurationMode, IsStartConfigurationModeEnable);

            SelectedQuestion = ActivePack?.Questions.FirstOrDefault();
            TextVisibility = (ActivePack?.Questions.Count ?? 0) > 0;
        }

        private void AddQuestion(object? obj)
        {
            var pack = ActivePack;
            if (pack == null) return;

            pack.Questions.Add(new Question("New Question", string.Empty, string.Empty, string.Empty, string.Empty));

            SelectedQuestion = pack.Questions.Count > 0
                ? pack.Questions.Last()
                : pack.Questions.FirstOrDefault();

            UpdateCommandStates();
            ChangeTextVisibility();
            _ = mainWindowViewModel.SaveToMongoAsync();
        }

        private bool IsAddQuestionEnable(object? obj) => IsConfigurationModeVisible;

        private void DeleteQuestion(object? obj)
        {
            var pack = ActivePack;
            if (pack == null) return;

            pack.Questions.Remove(SelectedQuestion);
            UpdateCommandStates();
            ChangeTextVisibility();
            _ = mainWindowViewModel.SaveToMongoAsync();
        }

        private bool IsDeleteQuestionEnable(object? obj)
            => IsConfigurationModeVisible && SelectedQuestion != null && (DeleteQuestionIsEnable = (ActivePack != null && (ActivePack.Questions.Count > 0)));

        private void EditPackOptions(object? obj)
        {
            EditPackOptionsRequested?.Invoke(this, EventArgs.Empty);
            _ = mainWindowViewModel.SaveToMongoAsync();
        }

        private bool IsEditPackOptionsEnable(object? obj) => IsConfigurationModeVisible;

        private void ChangeTextVisibility()
            => TextVisibility = (ActivePack?.Questions.Count ?? 0) > 0 && SelectedQuestion != null;

        private void StartConfigurationMode(object? obj)
        {
            mainWindowViewModel.PlayerViewModel._timer?.Stop();

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





















