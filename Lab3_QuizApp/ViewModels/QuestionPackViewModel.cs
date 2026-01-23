using QuizAppExtended.Models;
using System.Collections.ObjectModel;


namespace QuizAppExtended.ViewModels
{
    internal class QuestionPackViewModel : ViewModelBase
    {
        private readonly QuestionPack model;

        public QuestionPack Model => model; // <-- added accessor

        public string Name
        {
            get => model.Name;
            set
            {
                model.Name = value;
                RaisePropertyChanged();
            }
        }

        public Difficulty Difficulty
        {
            get => model.Difficulty;
            set
            {
                model.Difficulty = value;
                RaisePropertyChanged();
            }
        }

        public int TimeLimitInSeconds
        {
            get => model.TimeLimitInSeconds;
            set
            {
                model.TimeLimitInSeconds = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<Question> Questions { get; }

        public QuestionPackViewModel(QuestionPack model)
        {
            this.model = model;
            // defensive: model.Questions can be null when data in DB lacks the property
            this.Questions = new ObservableCollection<Question>(model.Questions ?? new System.Collections.Generic.List<Question>());
        }
    }
}
