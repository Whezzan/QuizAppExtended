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
        public event EventHandler<bool>? ExitGameRequested;
        public event EventHandler? OpenNewPackDialogRequested;
        public event EventHandler<bool>? ToggleFullScreenRequested;

        private readonly Services.MongoDbService _mongoService;
        private readonly Services.MongoCategoryService _mongoCategoryService;
        private readonly Services.MongoGameSessionService _mongoGameSessionService;

        public MainWindowViewModel()
        {
            CanExit = false;
            DeletePackIsEnable = true;
            IsFullscreen = false;

            FilePath = GetFilePath();
            var connection = Environment.GetEnvironmentVariable("QUIZAPP_MONGO_CONN") ?? "mongodb://localhost:27017";

            _mongoService = new Services.MongoDbService(connection, "QuizAppDb");

            // Separate collection for categories (Categories)
            _mongoCategoryService = new Services.MongoCategoryService(connection, "QuizAppDb");

            // New: sessions
            _mongoGameSessionService = new Services.MongoGameSessionService(connection, "QuizAppDb");

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

            AddCategoryCommand = new DelegateCommand(AddCategory);
            DeleteCategoryCommand = new DelegateCommand(async _ => await DeleteSelectedCategoryAsync(), _ => SelectedCategory != null);
        }

        internal Task SaveGameSessionAsync(GameSession session)
            => _mongoGameSessionService.InsertAsync(session);

        internal Task<List<GameSession>> GetTop5ForPackAsync(string packId)
            => _mongoGameSessionService.GetTop5ByPackAsync(packId);

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
                await _mongoGameSessionService.EnsureCreatedAsync();

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
                    var defaultVm = new QuestionPackViewModel(new QuestionPack("Default Question Pack"));
                    Packs.Add(defaultVm);
                    ActivePack = defaultVm;

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
                var importedPack = new QuestionPackViewModel(new QuestionPack("Imported Trivia Pack"))
                {
                    CategoryId = savedCategory?.Id
                };

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
    }
}