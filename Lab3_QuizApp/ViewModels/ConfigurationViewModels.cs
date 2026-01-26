using QuizAppExtended.Command;
using QuizAppExtended.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using MongoDB.Driver;

namespace QuizAppExtended.ViewModels
{
    internal class ConfigurationViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel mainWindowViewModel;
        public QuestionPackViewModel? ActivePack { get => mainWindowViewModel.ActivePack; }
        private string FilePath { get => mainWindowViewModel.FilePath; }

        public ObservableCollection<TriviaCategory> Categories => mainWindowViewModel.Categories;

        private readonly DispatcherTimer _autoSaveTimer;

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
                SaveQuestionToBankCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged();
                ChangeTextVisibility();
                RaisePropertyChanged(nameof(HasIncompleteQuestions));
                mainWindowViewModel.PlayerViewModel?.SwitchToPlayModeCommand.RaiseCanExecuteChanged();
            }
        }

        public bool HasIncompleteQuestions
            => ActivePack?.Questions.Any(q => !IsQuestionComplete(q)) ?? true;

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
        public DelegateCommand SaveQuestionToBankCommand { get; }

        public ConfigurationViewModel(MainWindowViewModel mainWindowViewModel)
        {
            this.mainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));

            _autoSaveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(400)
            };
            _autoSaveTimer.Tick += async (_, _) =>
            {
                _autoSaveTimer.Stop();
                await mainWindowViewModel.SaveToMongoAsync();
                RaisePropertyChanged(nameof(HasIncompleteQuestions));
                mainWindowViewModel.PlayerViewModel.SwitchToPlayModeCommand.RaiseCanExecuteChanged();
            };

            DeleteQuestionIsEnable = false;
            IsConfigurationModeVisible = true;

            AddQuestionCommand = new DelegateCommand(AddQuestion, IsAddQuestionEnable);
            DeleteQuestionCommand = new DelegateCommand(DeleteQuestion, IsDeleteQuestionEnable);
            EditPackOptionsCommand = new DelegateCommand(EditPackOptions, IsEditPackOptionsEnable);
            SwitchToConfigurationModeCommand = new DelegateCommand(StartConfigurationMode, IsStartConfigurationModeEnable);

            SaveQuestionToBankCommand = new DelegateCommand(async _ => await SaveSelectedQuestionToBankAsync(), IsSaveQuestionToBankEnable);

            SelectedQuestion = ActivePack?.Questions.FirstOrDefault();
            TextVisibility = (ActivePack?.Questions.Count ?? 0) > 0;
        }

        public void ScheduleAutoSave()
        {
            if (!IsConfigurationModeVisible)
            {
                return;
            }

            _autoSaveTimer.Stop();
            _autoSaveTimer.Start();
        }

        private static bool IsQuestionComplete(Question? q)
        {
            if (q == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(q.Query) || string.IsNullOrWhiteSpace(q.CorrectAnswer))
            {
                return false;
            }

            if (q.IncorrectAnswers == null || q.IncorrectAnswers.Length != 3)
            {
                return false;
            }

            return !q.IncorrectAnswers.Any(a => string.IsNullOrWhiteSpace(a));
        }

        private void AddQuestion(object? obj)
        {
            var pack = ActivePack;
            if (pack == null) return;

            var newQuestion = new Question("New Question", string.Empty, string.Empty, string.Empty, string.Empty)
            {
                CategoryId = pack.CategoryId
            };

            pack.Questions.Add(newQuestion);

            SelectedQuestion = pack.Questions.Count > 0
                ? pack.Questions.Last()
                : pack.Questions.FirstOrDefault();

            UpdateCommandStates();
            ChangeTextVisibility();

            ScheduleAutoSave();
        }

        private bool IsAddQuestionEnable(object? obj) => IsConfigurationModeVisible;

        private void DeleteQuestion(object? obj)
        {
            var pack = ActivePack;
            if (pack == null)
            {
                return;
            }

            var selected = SelectedQuestion;
            if (selected == null)
            {
                return;
            }

            pack.Questions.Remove(selected);
            SelectedQuestion = pack.Questions.FirstOrDefault();

            UpdateCommandStates();
            ChangeTextVisibility();

            ScheduleAutoSave();
        }

        private bool IsDeleteQuestionEnable(object? obj)
            => IsConfigurationModeVisible && SelectedQuestion != null && (DeleteQuestionIsEnable = (ActivePack != null && (ActivePack.Questions.Count > 0)));

        private void EditPackOptions(object? obj)
        {
            EditPackOptionsRequested?.Invoke(this, EventArgs.Empty);
            ScheduleAutoSave();
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

        private bool IsSaveQuestionToBankEnable(object? obj)
            => IsConfigurationModeVisible && SelectedQuestion != null;

        private async Task SaveSelectedQuestionToBankAsync()
        {
            if (SelectedQuestion == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedQuestion.CategoryId))
            {
                MessageBox.Show("Choose a category before saving to Question Bank.", "Save to Question Bank",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var copy = new Question(SelectedQuestion.Query, SelectedQuestion.CorrectAnswer, SelectedQuestion.IncorrectAnswers.ToArray())
                {
                    CategoryId = SelectedQuestion.CategoryId
                };

                await mainWindowViewModel.SaveQuestionToBankAsync(copy);
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                MessageBox.Show(
                    "The question already exists in Question Bank",
                    "Duplicate",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save question to question bank: {ex.Message}", "DB Save Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateCommandStates()
        {
            AddQuestionCommand.RaiseCanExecuteChanged();
            DeleteQuestionCommand.RaiseCanExecuteChanged();
            EditPackOptionsCommand.RaiseCanExecuteChanged();
            SaveQuestionToBankCommand.RaiseCanExecuteChanged();
            mainWindowViewModel.PlayerViewModel.SwitchToPlayModeCommand.RaiseCanExecuteChanged();
        }

        public async Task FlushAutoSaveAsync()
        {
            _autoSaveTimer.Stop();

            await mainWindowViewModel.SaveToMongoAsync();

            RaisePropertyChanged(nameof(HasIncompleteQuestions));
            mainWindowViewModel.PlayerViewModel?.SwitchToPlayModeCommand.RaiseCanExecuteChanged();
        }
    }
}