using System.Windows.Input;

namespace Lab3_QuizApp.Command
{
    internal class DelegateCommand : ICommand
    {
        private readonly Action<object> execute;
        private readonly Func<object?, bool> canExecute;

        public event EventHandler? CanExecuteChanged;

        public DelegateCommand(Action<object> execute, Func<object?, bool> canExecute = null)
        {
            ArgumentNullException.ThrowIfNull(execute);
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        public bool CanExecute(object? parameter) => canExecute is null ? true : canExecute(parameter);

        public void Execute(object? parameter) => execute(parameter);

    }
}