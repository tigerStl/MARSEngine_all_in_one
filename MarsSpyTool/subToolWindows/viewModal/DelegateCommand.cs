namespace Mars.message.ViewModel
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Input;

    public class DelegateCommand : ICommand
    {
        private readonly Action executeMethod;

        private readonly Func<bool> canExecuteMethod;

        //public DelegateCommand(Action<object> execute, Func<object, bool> canExecute)
        //{
        //    if (execute == null)
        //        throw new ArgumentNullException("execute", "execute cannot be null");
        //    if (canExecute == null)
        //        throw new ArgumentNullException("canExecute", "canExecute cannot be null");

        //    this.executeMethod = executeMethod;
        //    this.canExecuteMethod = canExecuteMethod;
        //}        


        public DelegateCommand(Action executeMethod)
            : this(executeMethod, null)
        {
        }

        public DelegateCommand(Action executeMethod, Func<bool> canExecuteMethod)
        {
            this.executeMethod = executeMethod;
            this.canExecuteMethod = canExecuteMethod;
        }

        public bool CanExecute(object parameter)
        {
            return this.canExecuteMethod != null ? this.canExecuteMethod() : true;
        }

        public void Execute(object parameter)
        {
            if (this.executeMethod != null)
            {
                this.executeMethod();
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }

            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }
    }


    public class DelegateCommandWithParam : ICommand
    {
        private Action<object> execute;
        private Func<object, bool> canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public DelegateCommandWithParam(Action<object> execute, Func<object, bool> canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return this.canExecute == null || this.canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            this.execute(parameter);
        }
    }
}
