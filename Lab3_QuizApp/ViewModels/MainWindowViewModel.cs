using QuizAppExtended.Command;
using QuizAppExtended.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Net;
using Path = System.IO.Path;
using System.Net.Http;
using System.Windows;
using System.Linq;

namespace QuizAppExtended.ViewModels
{
    internal class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<QuestionPackViewModel> Packs { get; set; } = new ObservableCollection<QuestionPackViewModel>();
        public ConfigurationViewModel ConfigurationViewModel { get; }
        public PlayerViewModel PlayerViewModel { get; }
        public string FilePath { get; set; }


        private bool _canExit;
        public bool CanExit
        {
            get => _canExit;
            set
            {
                _canExit = value;
                RaisePropertyChanged();

            }
        }

        private bool _deletePackIsEnable;
        public bool DeletePackIsEnable
        {
            get => _deletePackIsEnable;
            set
            {
                _deletePackIsEnable = value;
                RaisePropertyChanged();
            }
        }

        private bool _isFullscreen;
        public bool IsFullscreen
        {
            get => _isFullscreen;
            set
            {
                _isFullscreen = value;
                RaisePropertyChanged();
            }
        }


        private QuestionPackViewModel? _activePack;
        public QuestionPackViewModel? ActivePack
        {
            get => _activePack;
            set
            {
                _activePack = value;
                RaisePropertyChanged();
                ConfigurationViewModel?.RaisePropertyChanged();
            }
        }

        private QuestionPackViewModel? _newPack;
        public QuestionPackViewModel? NewPack
        {
            get => _newPack;
            set
            {
                _newPack = value;
                RaisePropertyChanged();
            }
        }

        private QuestionPackViewModel? _selectedPack;
        public QuestionPackViewModel? SelectedPack
        {
            get => _selectedPack;
            set
            {
                _selectedPack = value;
                RaisePropertyChanged();
            }
        }


        public event EventHandler? CloseDialogRequested;
        public event EventHandler? DeletePackRequested;
        public event EventHandler<bool>? ExitGameRequested;
        public event EventHandler? OpenNewPackDialogRequested;
        public event EventHandler<bool>? ToggleFullScreenRequested;

        public DelegateCommand CloseDialogCommand { get; }
        public DelegateCommand CreateNewPackCommand { get; }
        public DelegateCommand DeletePackCommand { get; }
        public DelegateCommand ExitGameCommand { get; }
        public DelegateCommand OpenDialogCommand { get; }
        public DelegateCommand SaveOnShortcutCommand { get; }
        public DelegateCommand SelectActivePackCommand { get; }
        public DelegateCommand ToggleWindowFullScreenCommand { get; }
        public DelegateCommand OpenImportQuestionsCommand { get; }


        private readonly Services.MongoDbService _mongoService;

        public MainWindowViewModel()
        {
            CanExit = false;
            DeletePackIsEnable = true;
            IsFullscreen = false;

            FilePath = GetFilePath();
            var connection = Environment.GetEnvironmentVariable("QUIZAPP_MONGO_CONN") ?? "mongodb://localhost:27017";
            _mongoService = new Services.MongoDbService(connection, "QuizAppDb");

            _ = InitializeDataAsync(); // fire-and-forget existing pattern

            ConfigurationViewModel = new ConfigurationViewModel(this);
            PlayerViewModel = new PlayerViewModel(this);

            CloseDialogCommand = new DelegateCommand(ClosePackDialog);
            OpenDialogCommand = new DelegateCommand(OpenPackDialog);

            CreateNewPackCommand = new DelegateCommand(CreateNewPack);
            DeletePackCommand = new DelegateCommand(RequestDeletePack, IsDeletePackEnable);

            SaveOnShortcutCommand = new DelegateCommand(SaveOnShortcut);
            SelectActivePackCommand = new DelegateCommand(SelectActivePack);
            ToggleWindowFullScreenCommand = new DelegateCommand(ToggleWindowFullScreen);
            ExitGameCommand = new DelegateCommand(ExitGame);
            OpenImportQuestionsCommand = new DelegateCommand(async _ => await ImportQuestionsAsync());

        }

        private void OpenPackDialog(object? obj)
        {
            NewPack = new QuestionPackViewModel(new QuestionPack());
            OpenNewPackDialogRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ClosePackDialog(object? obj) => CloseDialogRequested?.Invoke(this, EventArgs.Empty);

        private void CreateNewPack(object? obj)
        {
            if (NewPack != null)
            {
                Packs.Add(NewPack!);
                ActivePack = NewPack;

                ConfigurationViewModel.DeleteQuestionCommand.RaiseCanExecuteChanged();
                DeletePackCommand.RaiseCanExecuteChanged();
                _ = SaveToJsonAsync(); // Fire-and-forget, explicitly discard the returned Task
            }

            CloseDialogRequested?.Invoke(this, EventArgs.Empty);
        }

        private void RequestDeletePack(object? obj) => DeletePackRequested?.Invoke(this, EventArgs.Empty);

        public void DeletePack()
        {
            if (ActivePack != null)
            {
                Packs.Remove(ActivePack!);
                DeletePackCommand.RaiseCanExecuteChanged();

                if (Packs.Count > 0)
                {
                    ActivePack = Packs.FirstOrDefault();
                }
                _ = SaveToJsonAsync(); // Fire-and-forget, explicitly discard the returned Task
            }
        }

        private bool IsDeletePackEnable(object? obj) => Packs != null && Packs.Count > 0;

        private void SelectActivePack(object? obj)
        {
            if (obj is QuestionPackViewModel selectedPack)
            {
                SelectedPack = selectedPack;
                ActivePack = SelectedPack;
                _ = SaveToJsonAsync(); // Fire-and-forget, explicitly discard the returned Task
            }
        }

        private void ToggleWindowFullScreen(object? obj)
        {
            IsFullscreen = !IsFullscreen;
            ToggleFullScreenRequested?.Invoke(this, _isFullscreen);
        }

        private async void ExitGame(object? obj)
        {
            await SaveToJsonAsync();

            CanExit = true;
            ExitGameRequested?.Invoke(this, CanExit);
        }

        private string GetFilePath()
        {
            string appDataFilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string directoryFilePath = Path.Combine(appDataFilePath, "Laboration_3");

            if (!Directory.Exists(directoryFilePath))
            {
                Directory.CreateDirectory(directoryFilePath);
            }

            string filePath = Path.Combine(directoryFilePath, "Laboration_3.json");
            return filePath;
        }

        private async Task InitializeDataAsync()
        {
            // keep the same Packs instance so bindings remain valid
            Packs.Clear();

            try
            {
                // Ensure DB and collection exist before trying to read
                await _mongoService.EnsureDatabaseCreatedAsync();

                var packsFromDb = await _mongoService.GetAllPacksAsync();
                if (packsFromDb != null && packsFromDb.Count > 0)
                {
                    foreach (var p in packsFromDb)
                    {
                        // QuestionPackViewModel now handles null Questions safely
                        Packs.Add(new QuestionPackViewModel(p));
                    }
                    ActivePack = Packs.FirstOrDefault();
                }
                else
                {
                    var defaultVm = new QuestionPackViewModel(new QuestionPack("Default Question Pack"));
                    Packs.Add(defaultVm);
                    ActivePack = defaultVm;

                    try
                    {
                        await SaveToMongoAsync();
                    }
                    catch
                    {
                        // don't break startup for DB write failures; consider logging
                    }
                }
            }
            catch (System.Exception ex)
            {
                // Surface startup/load errors so you can see why DB data didn't appear
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show($"Failed to load packs from database: {ex.Message}", "DB Load Error",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                });
            }
            finally
            {
                // safe null-check: DeletePackCommand might not have been created yet
                DeletePackCommand?.RaiseCanExecuteChanged();
            }
        }

        public async Task SaveToMongoAsync()
        {
            foreach (var vm in Packs)
            {
                // ensure vm.Model.Questions is in sync with vm.Questions collection
                vm.Model.Questions = vm.Questions.ToList();
                await _mongoService.UpsertPackAsync(vm.Model);
            }
        }

        private async Task ImportQuestionsAsync()
        {
            try
            {
                // Visa dialog
                var dialog = new Views.ImportDialog();
                bool? result = dialog.ShowDialog();
                if (result != true) return; // avbröt användaren

                int amount = dialog.Amount;
                string category = dialog.Category;
                string difficulty = dialog.Difficulty;

                // API-anrop
                var service = new OpenTriviaService();
                var questions = await service.GetQuestionsAsync(amount, category, difficulty);

                if (!questions.Any())
                {
                    MessageBox.Show("Inga frågor hittades.", "Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Skapa nytt pack
                var importedPack = new QuestionPackViewModel(new QuestionPack("Imported Trivia Pack"));

                foreach (var q in questions)
                {
                    importedPack.Questions.Add(new Question(
                        System.Net.WebUtility.HtmlDecode(q.question),
                        System.Net.WebUtility.HtmlDecode(q.correct_answer),
                        (q.incorrect_answers ?? new List<string>())
                            .Select(a => System.Net.WebUtility.HtmlDecode(a))
                            .ToArray()
                    ));
                }

                Packs.Add(importedPack);
                ActivePack = importedPack;

                // Persist locally and to MongoDB
                await SaveToJsonAsync();

                try
                {
                    await SaveToMongoAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save imported pack to database: {ex.Message}", "DB Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                MessageBox.Show($"✅ {questions.Count} frågor importerade!", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Fel vid API-anrop: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ett oväntat fel inträffade: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveOnShortcut(object? obj) => await SaveToJsonAsync();

        private async Task SaveToJsonAsync()
        {
            try
            {
                if (Packs == null || string.IsNullOrWhiteSpace(FilePath))
                    return;

                var packsToSave = new List<QuestionPack>();
                foreach (var vm in Packs)
                {
                    // keep model in sync with viewmodel
                    vm.Model.Questions = new List<Question>(vm.Questions);
                    packsToSave.Add(vm.Model);
                }

                var json = JsonSerializer.Serialize(packsToSave, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(FilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save packs: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task DeletePackAsync()
        {
            if (ActivePack != null)
            {
                // store id before removing VM
                var packId = ActivePack.Model.Id;

                // remove from UI collection
                Packs.Remove(ActivePack);
                DeletePackCommand.RaiseCanExecuteChanged();

                // select a new active pack if any remain
                if (Packs.Count > 0)
                {
                    ActivePack = Packs.FirstOrDefault();
                }
                else
                {
                    ActivePack = null;
                }

                // delete from MongoDB
                try
                {
                    if (!string.IsNullOrWhiteSpace(packId))
                    {
                        await _mongoService.DeletePackAsync(packId);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete pack from database: {ex.Message}", "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}