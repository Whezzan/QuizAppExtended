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
using QuizAppExtended.Dialogs;
using MongoDB.Driver;
using QuizAppExtended.Services;
using System.Windows.Input;

namespace QuizAppExtended.ViewModels
{
    internal class MainWindowViewModel : ViewModelBase
    {
        public ObservableCollection<QuestionPackViewModel> Packs { get; set; } = new ObservableCollection<QuestionPackViewModel>();

        public ObservableCollection<TriviaCategory> Categories { get; } = new ObservableCollection<TriviaCategory>();

        private TriviaCategory? _selectedCategory;
        public TriviaCategory? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                RaisePropertyChanged();
                DeleteCategoryCommand.RaiseCanExecuteChanged();
            }
        }

        public DelegateCommand CloseDialogCommand { get; }
        public DelegateCommand CreateNewPackCommand { get; }
        public DelegateCommand DeletePackCommand { get; }
        public DelegateCommand ExitGameCommand { get; }
        public DelegateCommand OpenDialogCommand { get; }
        public DelegateCommand SaveOnShortcutCommand { get; }
        public DelegateCommand SelectActivePackCommand { get; }
        public DelegateCommand ToggleWindowFullScreenCommand { get; }
        public DelegateCommand OpenImportQuestionsCommand { get; }
        public DelegateCommand AddCategoryCommand { get; }
        public DelegateCommand DeleteCategoryCommand { get; }

        public DelegateCommand OpenQuestionBankCommand { get; }
        public DelegateCommand AddQuestionFromBankCommand { get; }

        private Question? _selectedBankQuestion;
        public Question? SelectedBankQuestion
        {
            get => _selectedBankQuestion;
            set
            {
                _selectedBankQuestion = value;
                RaisePropertyChanged();
                AddQuestionFromBankCommand.RaiseCanExecuteChanged();
            }
        }

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

                PlayerViewModel?.SwitchToPlayModeCommand.RaiseCanExecuteChanged();
                OpenQuestionBankCommand.RaiseCanExecuteChanged();
                AddQuestionFromBankCommand.RaiseCanExecuteChanged();
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

        private ObservableCollection<Question> _questionBankQuestions = new ObservableCollection<Question>();
        public ObservableCollection<Question> QuestionBankQuestions
        {
            get => _questionBankQuestions;
            private set
            {
                _questionBankQuestions = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(QuestionBankTreeItems));
            }
        }

        // New: expose grouped tree items for QuestionBankDialog
        public IEnumerable<QuestionBankCategoryNode> QuestionBankTreeItems
        {
            get
            {
                var categoriesById = Categories
                    .Where(c => !string.IsNullOrWhiteSpace(c.Id))
                    .ToDictionary(c => c.Id, c => c.Name, StringComparer.Ordinal);

                var grouped = QuestionBankQuestions
                    .GroupBy(q => q.CategoryId ?? string.Empty, StringComparer.Ordinal)
                    .Select(g =>
                    {
                        var categoryId = g.Key;

                        var title = string.IsNullOrWhiteSpace(categoryId)
                            ? "Uncategorized"
                            : (categoriesById.TryGetValue(categoryId, out var name) ? name : "Unknown Category");

                        return new QuestionBankCategoryNode(
                            categoryId: categoryId,
                            title: title,
                            questions: g.OrderBy(q => q.Query).ToList());
                    });

                var activeCategoryId = ActivePack?.CategoryId ?? string.Empty;

                return grouped
                    // Active pack category FIRST
                    .OrderByDescending(n => !string.IsNullOrWhiteSpace(activeCategoryId)
                                            && string.Equals(n.CategoryId, activeCategoryId, StringComparison.Ordinal))
                    .ThenBy(n => n.Title, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
        }

        private void RefreshQuestionBankTree()
            => RaisePropertyChanged(nameof(QuestionBankTreeItems));

        public event EventHandler? CloseDialogRequested;
        public event EventHandler<bool>? ExitGameRequested;
        public event EventHandler? OpenNewPackDialogRequested;
        public event EventHandler<bool>? ToggleFullScreenRequested;
        public event EventHandler? OpenQuestionBankDialogRequested;

        private readonly Services.MongoDbService _mongoService;
        private readonly Services.MongoCategoryService _mongoCategoryService;
        private readonly Services.MongoGameSessionService _mongoGameSessionService;
        private readonly Services.MongoQuestionBankService _mongoQuestionBankService;

        public MainWindowViewModel()
        {
            CanExit = false;
            DeletePackIsEnable = true;
            IsFullscreen = false;

            FilePath = GetFilePath();
            var connection = Environment.GetEnvironmentVariable("QUIZAPP_MONGO_CONN") ?? "mongodb://localhost:27017";

            _mongoService = new Services.MongoDbService(connection, "QuizAppDb");
            _mongoCategoryService = new Services.MongoCategoryService(connection, "QuizAppDb");
            _mongoGameSessionService = new Services.MongoGameSessionService(connection, "QuizAppDb");
            _mongoQuestionBankService = new Services.MongoQuestionBankService(connection, "QuizAppDb");

            _ = InitializeDataAsync();

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

            AddCategoryCommand = new DelegateCommand(AddCategory);
            DeleteCategoryCommand = new DelegateCommand(async _ => await DeleteSelectedCategoryAsync(), _ => SelectedCategory != null);

            OpenQuestionBankCommand = new DelegateCommand(async _ => await OpenQuestionBankAsync(), _ => ActivePack != null);
            AddQuestionFromBankCommand = new DelegateCommand(AddSelectedBankQuestionToActivePack, IsAddSelectedBankQuestionToActivePackEnabled);
        }

        internal Task SaveQuestionToBankAsync(Question question)
            => _mongoQuestionBankService.InsertAsync(question);

        internal Task<List<GameSession>> GetTop5ForPackAsync(string packId)
            => _mongoGameSessionService.GetTop5ByPackAsync(packId);

        internal Task<AnswerStats> GetAnswerStatsAsync(string packId, string questionText)
            => _mongoGameSessionService.GetAnswerStatsAsync(packId, questionText);

        // FIX: PlayerViewModel calls this; it was missing, causing CS1061.
        internal Task SaveGameSessionAsync(GameSession session)
            => _mongoGameSessionService.InsertAsync(session);

        private bool IsOpenQuestionBankEnabled(object? obj)
            => ActivePack != null;

        private async Task OpenQuestionBankAsync()
        {
            try
            {
                var questions = await _mongoQuestionBankService.GetAllAsync();
                QuestionBankQuestions = new ObservableCollection<Question>(questions);

                SelectedBankQuestion = QuestionBankQuestions.FirstOrDefault();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load question bank: {ex.Message}", "DB Load Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            OpenQuestionBankDialogRequested?.Invoke(this, EventArgs.Empty);
        }

        private bool IsAddSelectedBankQuestionToActivePackEnabled(object? obj)
            => ActivePack != null
                && SelectedBankQuestion != null
                && !ContainsFullMatchQuestion(ActivePack, SelectedBankQuestion);

        private void AddSelectedBankQuestionToActivePack(object? obj)
        {
            if (ActivePack == null || SelectedBankQuestion == null)
            {
                return;
            }

            var selected = SelectedBankQuestion;

            // Copy to avoid sharing Mongo Id between Pack and Bank collections.
            var copy = new Question(selected.Query, selected.CorrectAnswer, selected.IncorrectAnswers.ToArray())
            {
                CategoryId = selected.CategoryId
            };

            ActivePack.Questions.Add(copy);

            AddQuestionFromBankCommand.RaiseCanExecuteChanged();

            _ = SaveToMongoAsync();
        }

        private static bool ContainsFullMatchQuestion(QuestionPackViewModel pack, Question candidate)
        {
            var candidateQuery = (candidate.Query ?? string.Empty).Trim();
            var candidateCorrect = (candidate.CorrectAnswer ?? string.Empty).Trim();
            var candidateIncorrect = candidate.IncorrectAnswers ?? Array.Empty<string>();
            var candidateCategoryId = (candidate.CategoryId ?? string.Empty).Trim();

            if (candidateIncorrect.Length != 3)
            {
                return false;
            }

            for (int i = 0; i < candidateIncorrect.Length; i++)
            {
                candidateIncorrect[i] = (candidateIncorrect[i] ?? string.Empty).Trim();
            }

            return pack.Questions.Any(q =>
                string.Equals((q.Query ?? string.Empty).Trim(), candidateQuery, StringComparison.Ordinal)
                && string.Equals((q.CorrectAnswer ?? string.Empty).Trim(), candidateCorrect, StringComparison.Ordinal)
                && string.Equals((q.CategoryId ?? string.Empty).Trim(), candidateCategoryId, StringComparison.Ordinal)
                && q.IncorrectAnswers != null
                && q.IncorrectAnswers.Length == 3
                && string.Equals((q.IncorrectAnswers[0] ?? string.Empty).Trim(), candidateIncorrect[0], StringComparison.Ordinal)
                && string.Equals((q.IncorrectAnswers[1] ?? string.Empty).Trim(), candidateIncorrect[1], StringComparison.Ordinal)
                && string.Equals((q.IncorrectAnswers[2] ?? string.Empty).Trim(), candidateIncorrect[2], StringComparison.Ordinal));
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

        private void RequestDeletePack(object? obj)
        {
            _ = DeleteSelectedPacksAsync();
        }

        private async Task DeleteSelectedPacksAsync()
        {
            // Load from DB to be sure the dialog shows what's actually in Mongo (same pattern as categories)
            List<QuestionPack> allPacks;
            try
            {
                allPacks = await _mongoService.GetAllPacksAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load question packs: {ex.Message}", "DB Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (allPacks.Count == 0)
            {
                MessageBox.Show("No question packs found.", "Delete Question Pack", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Create temporary VMs *only for the dialog* (so it can DisplayMemberPath=Name)
            var allPackVms = allPacks
                .Select(p => new QuestionPackViewModel(p))
                .OrderBy(p => p.Name)
                .ToList();

            var dialog = new DeleteQuestionPacksDialog(allPackVms)
            {
                Owner = Application.Current.MainWindow
            };

            var result = dialog.ShowDialog();
            if (result != true)
            {
                return;
            }

            var toDelete = dialog.SelectedPacks;
            if (toDelete.Count == 0)
            {
                return;
            }

            var confirmation = MessageBox.Show(
                $"Are you sure you want to delete {toDelete.Count} pack(s)?",
                "Delete Question Pack",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                foreach (var packVm in toDelete)
                {
                    var packId = packVm.Model.Id;
                    if (!string.IsNullOrWhiteSpace(packId))
                    {
                        await _mongoService.DeletePackAsync(packId);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete packs: {ex.Message}", "DB Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Update in-memory list used by the UI (remove packs that match deleted ids)
            var deletedIds = toDelete
                .Select(p => p.Model.Id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToHashSet();

            // If active pack is deleted, clear it first to avoid pointing at removed item
            if (ActivePack?.Model?.Id != null && deletedIds.Contains(ActivePack.Model.Id))
            {
                ActivePack = null;
            }

            for (int i = Packs.Count - 1; i >= 0; i--)
            {
                var id = Packs[i].Model.Id;
                if (!string.IsNullOrWhiteSpace(id) && deletedIds.Contains(id))
                {
                    Packs.RemoveAt(i);
                }
            }

            ActivePack = Packs.FirstOrDefault();

            DeletePackCommand.RaiseCanExecuteChanged();
            _ = SaveToJsonAsync();
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
            Packs.Clear();
            Categories.Clear();

            try
            {
                await _mongoService.EnsureDatabaseCreatedAsync();
                await _mongoCategoryService.EnsureCreatedAsync();
                await SeedDefaults();
                await _mongoGameSessionService.EnsureCreatedAsync();
                await _mongoQuestionBankService.EnsureCreatedAsync();

                var categoriesFromDb = await _mongoCategoryService.GetAllAsync();
                foreach (var c in categoriesFromDb.OrderBy(c => c.Name))
                {
                    Categories.Add(c);
                }
                SelectedCategory = Categories.FirstOrDefault();

                var packsFromDb = await _mongoService.GetAllPacksAsync();
                if (packsFromDb != null && packsFromDb.Count > 0)
                {
                    foreach (var p in packsFromDb)
                    {
                        Packs.Add(new QuestionPackViewModel(p));
                    }
                    ActivePack = Packs.FirstOrDefault();
                }
                else
                {
                    var seededPack = CreateSeedPack();
                    var seededVm = new QuestionPackViewModel(seededPack);

                    Packs.Add(seededVm);
                    ActivePack = seededVm;

                    try
                    {
                        await SaveToMongoAsync();
                    }
                    catch
                    {
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show($"Failed to load data from database: {ex.Message}", "DB Load Error",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                });
            }
            finally
            {
                DeletePackCommand?.RaiseCanExecuteChanged();
                DeleteCategoryCommand?.RaiseCanExecuteChanged();
            }
        }

        private async void AddCategory(object? obj)
        {
            var dialog = new CreateCategoryDialog
            {
                Owner = Application.Current.MainWindow
            };

            var result = dialog.ShowDialog();
            if (result != true)
            {
                return;
            }

            var name = dialog.CategoryName;
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Category name cannot be empty.", "Add Category", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Categories.Any(c => string.Equals(c.Name, name, System.StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("A category with that name already exists.", "Add Category", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var category = new TriviaCategory(name, "");

            try
            {
                await _mongoCategoryService.UpsertAsync(category);
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                MessageBox.Show("A category with that name already exists.", "Add Category", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Failed to save category: {ex.Message}", "DB Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Categories.Add(category);
            SelectedCategory = category;
        }

        private async Task DeleteSelectedCategoryAsync()
        {
            // Load from DB to be sure the dialog shows what's actually in Mongo
            List<TriviaCategory> allCategories;
            try
            {
                allCategories = await _mongoCategoryService.GetAllAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load categories: {ex.Message}", "DB Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (allCategories.Count == 0)
            {
                MessageBox.Show("No categories found.", "Remove Category", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new DeleteCategoriesDialog(allCategories)
            {
                Owner = Application.Current.MainWindow
            };

            var result = dialog.ShowDialog();
            if (result != true)
            {
                return;
            }

            var toDelete = dialog.SelectedCategories;
            if (toDelete.Count == 0)
            {
                return;
            }

            var confirmation = MessageBox.Show(
                $"Are you sure you want to delete {toDelete.Count} categor(ies)?",
                "Remove Category",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                foreach (var cat in toDelete)
                {
                    if (!string.IsNullOrWhiteSpace(cat.Id))
                    {
                        await _mongoCategoryService.DeleteAsync(cat.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete categories: {ex.Message}", "DB Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Update in-memory list used by the UI
            var deletedIds = toDelete.Select(c => c.Id).Where(id => !string.IsNullOrWhiteSpace(id)).ToHashSet();
            for (int i = Categories.Count - 1; i >= 0; i--)
            {
                if (Categories[i].Id != null && deletedIds.Contains(Categories[i].Id))
                {
                    Categories.RemoveAt(i);
                }
            }

            SelectedCategory = Categories.FirstOrDefault();
            DeleteCategoryCommand.RaiseCanExecuteChanged();
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
                string category = dialog.Category;   // OpenTDB id
                string difficulty = dialog.Difficulty;

                // Ensure selected import category exists in our Categories collection and get its Id
                TriviaCategory? savedCategory;
                try
                {
                    savedCategory = await EnsureImportCategorySavedAsync(dialog.CategoryId, category, dialog.CategoryName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save/import category: {ex.Message}", "DB Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // API-anrop
                var service = new OpenTriviaService();
                var questions = await service.GetQuestionsAsync(amount, category, difficulty);

                if (!questions.Any())
                {
                    MessageBox.Show("Inga frågor hittades.", "Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Skapa nytt pack
                var importedPack = new QuestionPackViewModel(new QuestionPack(dialog.PackName))
                {
                    CategoryId = savedCategory?.Id
                };

                foreach (var q in questions)
                {
                    importedPack.Questions.Add(new Question(
                        System.Net.WebUtility.HtmlDecode(q.question ?? string.Empty),
                        System.Net.WebUtility.HtmlDecode(q.correct_answer ?? string.Empty),
                        (q.incorrect_answers ?? new List<string>())
                            .Select(a => System.Net.WebUtility.HtmlDecode(a ?? string.Empty))
                            .ToArray()
                    )
                    {
                        CategoryId = savedCategory?.Id
                    });
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

        private async Task<TriviaCategory?> EnsureImportCategorySavedAsync(string? categoryId, string openTdbId, string? categoryName)
        {
            // already mapped to an existing DB category
            if (!string.IsNullOrWhiteSpace(categoryId))
            {
                var existingById = Categories.FirstOrDefault(c => c.Id == categoryId);
                if (existingById != null)
                {
                    return existingById;
                }
            }

            // Any category => no DB entry
            if (string.IsNullOrWhiteSpace(openTdbId))
            {
                return null;
            }

            // Use existing by OpenTdbId
            var existing = Categories.FirstOrDefault(c => c.OpenTdbId == openTdbId);
            if (existing != null)
            {
                return existing;
            }

            // Create with the real display name from the import UI
            var name = string.IsNullOrWhiteSpace(categoryName) ? $"OpenTDB {openTdbId}" : categoryName.Trim();

            // Avoid duplicates by name (client side)
            var existingByName = Categories.FirstOrDefault(c => string.Equals(c.Name, name, System.StringComparison.OrdinalIgnoreCase));
            if (existingByName != null)
            {
                // if it's the same OpenTdbId, reuse; otherwise keep existing name unique and still create by OpenTdbId
                if (string.Equals(existingByName.OpenTdbId, openTdbId, System.StringComparison.OrdinalIgnoreCase))
                {
                    return existingByName;
                }

                name = $"{name} ({openTdbId})";
            }

            var created = new TriviaCategory(name, openTdbId);

            try
            {
                await _mongoCategoryService.UpsertAsync(created);
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                // If unique-name index rejected, fall back to existing category by OpenTdbId if present
                var fromDb = await _mongoCategoryService.FindByOpenTdbIdAsync(openTdbId);
                if (fromDb != null)
                {
                    if (!Categories.Any(c => c.Id == fromDb.Id))
                    {
                        Categories.Add(fromDb);
                    }
                    return fromDb;
                }
                throw;
            }

            Categories.Add(created);
            return created;
        }

        private QuestionPack CreateSeedPack()
        {
            var pack = new QuestionPack("Default Pack");

            pack.Questions.Add(new Question("Vad lär Fredrik Johansson ut på ITHS?", "C# och Databaser", new[] { "Syslöjd", "Historia", "Idrott" }));
            pack.Questions.Add(new Question("What's 2 + 2?", "4", new[] { "3", "22", "Two" }));
            pack.Questions.Add(new Question("What's the chemical symbol for water?", "H2O", new[] { "CO2", "NaCl", "O2" }));
            pack.Questions.Add(new Question("In which year did the Titanic sink?", "1912", new[] { "1905", "1918", "1923" }));
            pack.Questions.Add(new Question("Who painted the Mona Lisa?", "Leonardo da Vinci", new[] { "Vincent van Gogh", "Pablo Picasso", "Claude Monet" }));

            return pack;
        }

        private async Task SeedDefaults()
        {
            await _mongoCategoryService.SeedDefaultsAsync(GetDefaultCategories());
        }

        private IEnumerable<TriviaCategory> GetDefaultCategories()
        {
            yield return new TriviaCategory("General Knowledge", "9");
            yield return new TriviaCategory("Books", "10");
            yield return new TriviaCategory("Movies", "11");
            yield return new TriviaCategory("Music", "12");
            yield return new TriviaCategory("TV", "14");
            yield return new TriviaCategory("Inventions", "15");
            yield return new TriviaCategory("Sport", "21");
            yield return new TriviaCategory("History", "23");
            yield return new TriviaCategory("Geography", "22");
        }
    }

    internal sealed class QuestionBankCategoryNode
    {
        public string CategoryId { get; }
        public string Title { get; }
        public IReadOnlyList<Question> Questions { get; }

        public QuestionBankCategoryNode(string categoryId, string title, IReadOnlyList<Question> questions)
        {
            CategoryId = categoryId;
            Title = title;
            Questions = questions;
        }
    }
}