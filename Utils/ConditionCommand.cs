using System;
using System.Windows.Input;

namespace MathTex.Utils {

    public class ConditionCommand : ICommand {

        #region Constructor
        protected ConditionCommand() {
        }

        public ConditionCommand(Action execute) {
            this.action = execute;
        }

        public ConditionCommand(Action execute, Func<bool> canExecute) : this(execute) {
            this.canExecute = canExecute;
        }
        #endregion

        #region PublicAPI
        public bool CanExecute(object parameter) {
            return this.canExecute == null || this.canExecute();
        }

        public void Execute(object parameter) {
            this.Executing?.Invoke(this, null);
            this.action();
            this.Executed?.Invoke(this, null);
        }
        #endregion

        #region EventHandler
        public event EventHandler Executed;
        public event EventHandler Executing;
        public event EventHandler CanExecuteChanged {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        #endregion

        private readonly Action action;
        private readonly Func<bool> canExecute = null;
    }

}
