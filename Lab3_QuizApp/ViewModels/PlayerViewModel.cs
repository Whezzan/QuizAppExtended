using Lab3_QuizApp.Command;
using Lab3_QuizApp.Models;
using System.Windows.Threading;

namespace Lab3_QuizApp.ViewModels
{
    internal class PlayerViewModel : ViewModelBase
    {
        private readonly MainWindowViewModel? mainWindowViewModel;
        public QuestionPackViewModel? ActivePack { get => mainWindowViewModel.ActivePack; }

        public DispatcherTimer _timer;
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
        private Random rnd = new Random();


        private string _correctAnswer;
        public string CorrectAnswer
        {
            get => _correctAnswer;
            set
            {
                _correctAnswer = value;
                RaisePropertyChanged();
            }
        }

        private string _currentQuestion;
        public string CurrentQuestion
        {
            get => _currentQuestion;
            set
            {
                _currentQuestion = value;
                RaisePropertyChanged();
            }
        }

        private string _questionStatus;
        public string QuestionStatus
        {
            get => _questionStatus;
            set
            {
                _questionStatus = value;
                RaisePropertyChanged();
            }
        }

        private string _results;
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

        private List<string> _shuffledAnswers;
        public List<string> ShuffledAnswers
        {
            get => _shuffledAnswers;
            set
            {
                _shuffledAnswers = value;
                RaisePropertyChanged();
            }
        }

        private List<Question> _questions;
        public List<Question> Questions
        {
            get => _questions;
            set
            {
                _questions = value;
                RaisePropertyChanged();
            }
        }
        public List<Question> ShuffledQuestions { get; set; }


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

        private bool _playButtonIsEnable;
        public bool PlayButtonIsEnable
        {
            get => _playButtonIsEnable;
            set
            {
                _playButtonIsEnable = value;
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


        private bool[] _checkmarkVisibilities;
        public bool[] CheckmarkVisibilities
        {
            get => _checkmarkVisibilities;
            set
            {
                _checkmarkVisibilities = value;
                RaisePropertyChanged();
            }
        }

        private bool[] _crossVisibilities;
        public bool[] CrossVisibilities
        {
            get => _crossVisibilities;
            set
            {
                _crossVisibilities = value;
                RaisePropertyChanged();
            }
        }


        public DelegateCommand SwitchToPlayModeCommand { get; }
        public DelegateCommand CheckPlayerAnswerCommand { get; }


        public PlayerViewModel(MainWindowViewModel? mainWindowViewModel)
        {
            this.mainWindowViewModel = mainWindowViewModel;
            _isAnswerButtonActive = true;

            SwitchToPlayModeCommand = new DelegateCommand(StartPlayMode, IsPlayModeEnable);
            CheckPlayerAnswerCommand = new DelegateCommand(OnSelectedAnswerAsync, IsAnswerButtonActive);

            CheckmarkVisibilities = new bool[4] { false, false, false, false };
            CrossVisibilities = new bool[4] { false, false, false, false };
        }

        private void StartPlayMode(object? obj)
        {
            PlayButtonIsEnable = false;
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
            if (ActivePack is null)
            {
                return false;
            }
            return (PlayButtonIsEnable = !IsPlayerModeVisible && ActivePack.Questions.Count > 0);
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (TimeLimit > 0)
            {
                TimeLimit--;
            }
            else
            {
                _timer.Stop();
                AwaitDisplayCorrectAnswerAsync();
            }
        }

        private void LoadNextQuestion()
        {
            TimeLimit = ActivePack.TimeLimitInSeconds;
            _timer.Start();

            ResetChecksAndCrossVisibility();

            if (currentQuestionIndex < Questions.Count)
            {
                QuestionStatus = $"Question {currentQuestionIndex + 1} of {Questions.Count}";
                GetNextQuestion();
            }
            else
            {
                SwitchToResultView();
            }
        }

        private void GetNextQuestion()
        {
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
        }

        private async void OnSelectedAnswerAsync(object? obj)
        {
            playerAnswerIndex = int.Parse(obj as string);

            if (obj == null)
            {
                await DisplayCorrectAnswerAsync();
                return;
            }

            await DisplayCorrectAnswerAsync();
        }

        private bool IsAnswerButtonActive(object? obj) => _isAnswerButtonActive;

        private async Task DisplayCorrectAnswerAsync()
        {
            _timer.Stop();
            _isAnswerButtonActive = false;
            CheckPlayerAnswerCommand.RaiseCanExecuteChanged();

            if (playerAnswerIndex != -1)
            {
                if (playerAnswerIndex == correctAnswerIndex)
                {
                    amountcorrectAnswers++;
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
            _timer.Stop();

            IsResultModeVisible = true;
            IsPlayerModeVisible = false;

            Results = $"You got {amountcorrectAnswers} out of {Questions.Count} question(s) correct";

            UpdateCommandStates();
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

    }
}