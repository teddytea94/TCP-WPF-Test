using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Serialization;

namespace TCP_WPF_Test
{
    class GenericMVVM
    {
    }
    public class ViewBase : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [XmlIgnore] public readonly Dictionary<string, List<string>> _errorsByPropertyName = new Dictionary<string, List<string>>();

        // Current Error for use when displaying latest error in View Model 
        private string CurrentError_;
        [XmlIgnore]
        public string CurrentError
        {
            get { return CurrentError_; }
            set
            {
                if (value == "" && HasErrors == true)
                {
                    CurrentError_ = _errorsByPropertyName.First().Value.FirstOrDefault();
                }
                else
                {
                    CurrentError_ = value;
                }

                OnPropertyChanged();
            }
        }

        // Current Focused Property for use when displaying latest error in View Model 
        private string CurrentFocusedProperty_;
        [XmlIgnore]
        public string CurrentFocusedProperty
        {
            get { return CurrentFocusedProperty_; }
            set
            {
                CurrentError = _errorsByPropertyName.ContainsKey(value) ?
                _errorsByPropertyName[value].LastOrDefault() : null;
                CurrentFocusedProperty_ = value;
                OnPropertyChanged();
            }
        }

        public bool HasErrors => _errorsByPropertyName.Any();
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        public IEnumerable GetErrors(string propertyName)
        {
            if (propertyName != null)
            {
                return _errorsByPropertyName.ContainsKey(propertyName) ?
                   _errorsByPropertyName[propertyName] : null;
            }
            else
            {
                return null;
            }

        }
        private void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
        public void ClearErrors(string propertyName)
        {
            if (_errorsByPropertyName.ContainsKey(propertyName))
            {
                _errorsByPropertyName.Remove(propertyName);
                OnErrorsChanged(propertyName);
                CurrentError = "";
            }
        }
        public void AddError(string propertyName, string error)
        {
            if (!_errorsByPropertyName.ContainsKey(propertyName))
                _errorsByPropertyName[propertyName] = new List<string>();

            if (!_errorsByPropertyName[propertyName].Contains(error))
            {
                _errorsByPropertyName[propertyName].Add(error);
                OnErrorsChanged(propertyName);
            }
        }

        // Code from https://www.c-sharpcorner.com/UploadFile/tirthacs/inotifydataerrorinfo-in-wpf/
        private object _lock = new object();
        public void Validate()
        {
            lock (_lock)
            {
                var validationContext = new ValidationContext(this, null, null);
                var validationResults = new List<ValidationResult>();
                Validator.TryValidateObject(this, validationContext, validationResults, true);

                //clear all previous _errors  
                var propNames = _errorsByPropertyName.Keys.ToList();
                _errorsByPropertyName.Clear();
                propNames.ForEach(pn => OnErrorsChanged(pn));
                HandleValidationResults(validationResults);
            }
        }
        public void ValidateProperty(object value, [CallerMemberName] string propertyName = null)
        {
            var validationContext = new ValidationContext(this, null, null);
            validationContext.MemberName = propertyName;
            var results = new List<ValidationResult>();
            Validator.TryValidateProperty(value, validationContext, results);
            if (_errorsByPropertyName.ContainsKey(propertyName))
            {
                _errorsByPropertyName.Remove(propertyName);
            }
            OnErrorsChanged(propertyName);
            HandleValidationResults(results);

            //if (results.Any()) { }
            //else
            //{
            //    _errorsByPropertyName.Remove(propertyName);
            //}
            //ErrorsChanged(this, new DataErrorsChangedEventArgs(propertyName));

        }
        private void HandleValidationResults(List<ValidationResult> validationResults)
        {
            //Group validation results by property names  
            var resultsByPropNames = from res in validationResults
                                     from mname in res.MemberNames
                                     group res by mname into g
                                     select g;
            //add _errors to dictionary and inform binding engine about _errors  
            foreach (var prop in resultsByPropNames)
            {
                var messages = prop.Select(r => r.ErrorMessage).ToList();
                _errorsByPropertyName.Add(prop.Key, messages);
                CurrentFocusedProperty = prop.Key;
                OnErrorsChanged(prop.Key);
            }
        }
    }
    public class RelayCommand : ICommand
    {
        private Action<object> execute;
        private Predicate<object> canExecute;
        private event EventHandler CanExecuteChangedInternal;
        public RelayCommand(Action<object> execute)
            : this(execute, DefaultCanExecute)
        {
        }
        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            if (canExecute == null)
            {
                throw new ArgumentNullException("canExecute");
            }

            this.execute = execute;
            this.canExecute = canExecute;
        }
        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
                this.CanExecuteChangedInternal += value;
            }

            remove
            {
                CommandManager.RequerySuggested -= value;
                this.CanExecuteChangedInternal -= value;
            }
        }
        public bool CanExecute(object parameter)
        {
            return this.canExecute != null && this.canExecute(parameter);
        }
        public void Execute(object parameter)
        {
            this.execute(parameter);
        }
        public void OnCanExecuteChanged()
        {
            EventHandler handler = this.CanExecuteChangedInternal;
            if (handler != null)
            {
                //DispatcherHelper.BeginInvokeOnUIThread(() => handler.Invoke(this, EventArgs.Empty));
                handler.Invoke(this, EventArgs.Empty);
            }
        }
        public void Destroy()
        {
            this.canExecute = _ => false;
            this.execute = _ => { return; };
        }
        private static bool DefaultCanExecute(object parameter)
        {
            return true;
        }
    }
}
