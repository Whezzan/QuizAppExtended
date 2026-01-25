using QuizAppExtended.Command;
using QuizAppExtended.Models;
using System.Windows.Threading;
using System.Linq;
using QuizAppExtended.Dialogs;
using System.Windows;

namespace QuizAppExtended.ViewModels
{
    internal class PlayerViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel mainWindowViewModel;
        public QuestionPackViewModel? ActivePack { get => mainWindowViewModel.ActivePack; }

        public DispatcherTimer? _timer;
        private int _timeLimit;
        public int TimeLimit
        {
            get => _timeLimit;
            private set
            {
                _timeLimit = value;
                RaisePropertyChanged();
            }
        }

        private int correctAnswerIndex;
        private int currentQuestionIndex;
        private int playerAnswerIndex;
        private int amountcorrectAnswers;
        private readonly Random rnd = new Random();

        private DateTime _gameStartedAtUtc;
        private DateTime _questionStartedAtUtc;
        private string _playerName = string.Empty;
        private readonly List<GameSessionAnswer> _answers = new List<GameSessionAnswer>();

        private string _correctAnswer = string.Empty;
        public string CorrectAnswer
        {
            get => _correctAnswer;
            set
            {
                _correctAnswer = value;
                RaisePropertyChanged();
            }
        }

        private string _currentQuestion = string.Empty;
        public string CurrentQuestion
        {
            get => _currentQuestion;
            set
            {
                _currentQuestion = value;
                RaisePropertyChanged();
            }
        }

        private string _questionStatus = string.Empty;
        public string QuestionStatus
        {
            get => _questionStatus;
            set
            {
                _questionStatus = value;
                RaisePropertyChanged();
            }
        }

        private string _results = string.Empty;
        public string Results
        {
            get => _results;
            set
            {
                _results = value;
                RaisePropertyChanged();
            }
        }

        private bool _isAnswerButtonActive;

        private List<string> _shuffledAnswers = new List<string>();
        public List<string> ShuffledAnswers
        {
            get => _shuffledAnswers;
            set
            {
                _shuffledAnswers = value;
                RaisePropertyChanged();
            }
        }

        private List<Question> _questions = new List<Question>();
        public List<Question> Questions
        {
            get => _questions;
            set
            {
                _questions = value;
                RaisePropertyChanged();
            }
        }

        public List<Question> ShuffledQuestions { get; set; } = new List<Question>();

        private bool _isPlayerModeVisible;
        public bool IsPlayerModeVisible
        {
            get => _isPlayerModeVisible;
            set
            {
                _isPlayerModeVisible = value;
                RaisePropertyChanged();
            }
        }

        private bool _isResultModeVisible;
        public bool IsResultModeVisible
        {
            get => _isResultModeVisible;
            set
            {
                _isResultModeVisible = value;
                RaisePropertyChanged();
            }
        }

        private bool[] _checkmarkVisibilities = new bool[4] { false, false, false, false };
        public bool[] CheckmarkVisibilities
        {
            get => _checkmarkVisibilities;
            set
            {
                _checkmarkVisibilities = value;
                RaisePropertyChanged();
            }
        }

        private bool[] _crossVisibilities = new bool[4] { false, false, false, false };
        public bool[] CrossVisibilities
        {
            get => _crossVisibilities;
            set
            {
                _crossVisibilities = value;
                RaisePropertyChanged();
            }
        }

        private List<GameSession> _top5Sessions = new List<GameSession>();
        public List<GameSession> Top5Sessions
        {
            get => _top5Sessions;
            private set
            {
                _top5Sessions = value;
                RaisePropertyChanged();
            }
        }

        private int[] _answerPercentages = new int[4];
        public int[] AnswerPercentages
        {
            get => _answerPercentages;
            private set
            {
                _answerPercentages = value;
                RaisePropertyChanged();
            }
        }

        private bool _showAnswerPercentages;
        public bool ShowAnswerPercentages
        {
            get => _showAnswerPercentages;
            private set
            {
                _showAnswerPercentages = value;
                RaisePropertyChanged();
            }
        }

        public DelegateCommand SwitchToPlayModeCommand { get; }
        public DelegateCommand CheckPlayerAnswerCommand { get; }

        public PlayerViewModel(MainWindowViewModel mainWindowViewModel)
        {
            this.mainWindowViewModel = mainWindowViewModel ?? throw new ArgumentNullException(nameof(mainWindowViewModel));
            _isAnswerButtonActive = true;

            SwitchToPlayModeCommand = new DelegateCommand(StartPlayMode, IsPlayModeEnable);
            CheckPlayerAnswerCommand = new DelegateCommand(OnSelectedAnswerAsync, IsAnswerButtonActive);

            CheckmarkVisibilities = new bool[4] { false, false, false, false };
            CrossVisibilities = new bool[4] { false, false, false, false };
        }

        private void StartPlayMode(object? obj)
        {
            var packs = mainWindowViewModel.Packs;
            if (packs is null || packs.Count == 0)
            {
                MessageBox.Show("No question packs available.", "Play", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new EnterNameDialog(packs, mainWindowViewModel.ActivePack)
            {
                Owner = Application.Current.MainWindow
            };

            var ok = dialog.ShowDialog();
            if (ok != true)
            {
                return;
            }

            _playerName = dialog.PlayerName;

            if (dialog.SelectedPack is not null)
            {
                mainWindowViewModel.ActivePack = dialog.SelectedPack;
            }

            if (ActivePack is null || ActivePack.Questions.Count == 0)
            {
                MessageBox.Show("Selected pack has no questions.", "Play", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _answers.Clear();
            _gameStartedAtUtc = DateTime.UtcNow;

            IsPlayerModeVisible = true;
            IsResultModeVisible = false;
            mainWindowViewModel.ConfigurationViewModel.IsConfigurationModeVisible = false;

            if (_timer == null)
            {
                _timer = new DispatcherTimer();
                _timer.Interval = TimeSpan.FromSeconds(1);
                _timer.Tick += OnTimerTick;
            }

            Questions = ActivePack.Questions.ToList();
            ShuffledQuestions = ActivePack.Questions.OrderBy(a => rnd.Next()).ToList();
            currentQuestionIndex = 0;
            amountcorrectAnswers = 0;

            LoadNextQuestion();
        }

        private bool IsPlayModeEnable(object? obj)
        {
            return !IsPlayerModeVisible && mainWindowViewModel.Packs.Count > 0;
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (TimeLimit > 0)
            {
                TimeLimit--;
            }
            else
            {
                _timer?.Stop();
                AwaitDisplayCorrectAnswerAsync();
            }
        }

        private void LoadNextQuestion()
        {
            TimeLimit = ActivePack!.TimeLimitInSeconds;
            _questionStartedAtUtc = DateTime.UtcNow;
            _timer?.Start();

            ResetChecksAndCrossVisibility();

            if (currentQuestionIndex < Questions.Count)
            {
                QuestionStatus = $"Question {currentQuestionIndex + 1} of {Questions.Count}";
                GetNextQuestion();
            }
            else
            {
                _ = SaveSessionAsync();
                SwitchToResultView();
            }
        }

        private void GetNextQuestion()
        {
            ShowAnswerPercentages = false;

            CurrentQuestion = ShuffledQuestions[currentQuestionIndex].Query;
            CorrectAnswer = ShuffledQuestions[currentQuestionIndex].CorrectAnswer;

            List<string> Answers = new List<string>
            {
                ShuffledQuestions[currentQuestionIndex].CorrectAnswer,
                ShuffledQuestions[currentQuestionIndex].IncorrectAnswers[0],
                ShuffledQuestions[currentQuestionIndex].IncorrectAnswers[1],
                ShuffledQuestions[currentQuestionIndex].IncorrectAnswers[2]
            };

            ShuffledAnswers = Answers.OrderBy(a => rnd.Next()).ToList();
            correctAnswerIndex = ShuffledAnswers.IndexOf(CorrectAnswer);
            playerAnswerIndex = -1;
            currentQuestionIndex++;

            _ = LoadAnswerStatsAsync();
        }

        private async void OnSelectedAnswerAsync(object? obj)
        {
            if (obj is not string s)
            {
                await DisplayCorrectAnswerAsync();
                return;
            }

            if (!int.TryParse(s, out playerAnswerIndex))
            {
                await DisplayCorrectAnswerAsync();
                return;
            }

            await DisplayCorrectAnswerAsync();
        }

        private bool IsAnswerButtonActive(object? obj) => _isAnswerButtonActive;

        private async Task DisplayCorrectAnswerAsync()
        {
            _timer?.Stop();

            ShowAnswerPercentages = true;

            _isAnswerButtonActive = false;
            CheckPlayerAnswerCommand.RaiseCanExecuteChanged();

            var questionIndex = currentQuestionIndex - 1;
            var selectedAnswer = (playerAnswerIndex >= 0 && playerAnswerIndex < ShuffledAnswers.Count)
                ? ShuffledAnswers[playerAnswerIndex]
                : string.Empty;

            var timeSpentSeconds = (int)Math.Max(0, (DateTime.UtcNow - _questionStartedAtUtc).TotalSeconds);
            var isCorrect = playerAnswerIndex != -1 && playerAnswerIndex == correctAnswerIndex;

            if (isCorrect)
            {
                amountcorrectAnswers++;
            }

            _answers.Add(new GameSessionAnswer
            {
                QuestionIndex = questionIndex,
                QuestionText = CurrentQuestion,
                SelectedAnswer = selectedAnswer,
                CorrectAnswer = CorrectAnswer,
                IsCorrect = isCorrect,
                TimeSpentSeconds = timeSpentSeconds
            });

            if (playerAnswerIndex != -1)
            {
                if (playerAnswerIndex == correctAnswerIndex)
                {
                    CheckmarkVisibilities[playerAnswerIndex] = true;
                }
                else
                {
                    CrossVisibilities[playerAnswerIndex] = true;
                }
            }
            CheckmarkVisibilities[correctAnswerIndex] = true;

            UpdateCommandStates();
            await Task.Delay(2000);

            _isAnswerButtonActive = true;
            CheckPlayerAnswerCommand.RaiseCanExecuteChanged();

            LoadNextQuestion();
        }

        private async void AwaitDisplayCorrectAnswerAsync() => await DisplayCorrectAnswerAsync();

        private void ResetChecksAndCrossVisibility()
        {
            for (int i = 0; i < CheckmarkVisibilities.Length; i++)
            {
                CheckmarkVisibilities[i] = false;
                CrossVisibilities[i] = false;
            }

            UpdateCommandStates();
        }

        private void SwitchToResultView()
        {
            _timer?.Stop();

            IsResultModeVisible = true;
            IsPlayerModeVisible = false;

            Results = $"{_playerName}: {amountcorrectAnswers} / {Questions.Count} correct";

            UpdateCommandStates();
        }

        private async Task SaveSessionAsync()
        {
            try
            {
                var endedAt = DateTime.UtcNow;

                var session = new GameSession
                {
                    PlayerName = _playerName,
                    PackId = ActivePack?.Model?.Id,
                    PackName = ActivePack?.Name,
                    StartedAtUtc = _gameStartedAtUtc,
                    EndedAtUtc = endedAt,
                    TotalTimeSeconds = (int)Math.Max(0, (endedAt - _gameStartedAtUtc).TotalSeconds),
                    CorrectCount = amountcorrectAnswers,
                    QuestionCount = Questions.Count,
                    Answers = _answers.ToList()
                };

                await mainWindowViewModel.SaveGameSessionAsync(session);

                // New: refresh Top 5 for this pack (correct desc, time asc)
                if (!string.IsNullOrWhiteSpace(session.PackId))
                {
                    Top5Sessions = await mainWindowViewModel.GetTop5ForPackAsync(session.PackId);
                }
                else
                {
                    Top5Sessions = new List<GameSession>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save game session: {ex.Message}", "DB Save Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateCommandStates()
        {
            RaisePropertyChanged("CheckmarkVisibilities");
            RaisePropertyChanged("CrossVisibilities");

            SwitchToPlayModeCommand.RaiseCanExecuteChanged();
            mainWindowViewModel.ConfigurationViewModel.AddQuestionCommand.RaiseCanExecuteChanged();
            mainWindowViewModel.ConfigurationViewModel.EditPackOptionsCommand.RaiseCanExecuteChanged();
            mainWindowViewModel.ConfigurationViewModel.DeleteQuestionCommand.RaiseCanExecuteChanged();
            mainWindowViewModel.ConfigurationViewModel.SwitchToConfigurationModeCommand.RaiseCanExecuteChanged();
        }

        private async Task LoadAnswerStatsAsync()
        {
            try
            {
                var packId = ActivePack?.Model?.Id;
                if (string.IsNullOrWhiteSpace(packId))
                {
                    AnswerPercentages = new int[4];
                    return;
                }

                var stats = await mainWindowViewModel.GetAnswerStatsAsync(packId, CurrentQuestion);
                if (stats.TotalAnswers <= 0)
                {
                    AnswerPercentages = new int[4];
                    return;
                }

                var perc = new int[4];
                for (int i = 0; i < 4 && i < ShuffledAnswers.Count; i++)
                {
                    var answer = ShuffledAnswers[i];
                    stats.CountsByAnswer.TryGetValue(answer, out var count);
                    perc[i] = (int)Math.Round((count * 100.0) / stats.TotalAnswers);
                }

                AnswerPercentages = perc;
            }
            catch
            {
                // Fail silent; stats are non-critical
                AnswerPercentages = new int[4];
            }
        }
    }
}