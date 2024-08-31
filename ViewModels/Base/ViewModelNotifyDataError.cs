using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CladeTyping.ViewModels.Base
{
    public abstract class ViewModelNotifyDataError : DialogViewModel, INotifyDataErrorInfo
    {

        protected void FloatValidateProperty(string propertyName, object value)
        {
            try
            {
                float dummy;
                if (string.IsNullOrWhiteSpace(value.ToString()))
                    AddError(propertyName, "no input");
                else if (!float.TryParse(value.ToString(), out dummy))
                    AddError(propertyName, "not numeric");
                else
                    RemoveError(propertyName);
            }
            catch
            {
                AddError(propertyName, "not numeric");
            }
        }

        protected void NumericValidateProperty(string propertyName, object value)
        {
            var strvalue = string.Empty;
            try
            {
                strvalue = value.ToString();
                int dummy;
                if (string.IsNullOrWhiteSpace(strvalue))
                    AddError(propertyName, "no input");
                else if (!int.TryParse(strvalue, out dummy))
                    AddError(propertyName, "not numeric");
                else
                    RemoveError(propertyName);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                AddError(propertyName, "not numeric");
            }
        }

        protected void StringValidateProperty(string propertyName, object value)
        {
            var instring = string.Empty;
            if (value != null) instring = value.ToString();

            if (string.IsNullOrWhiteSpace(instring))
                AddError(propertyName, "no input");
            else
                RemoveError(propertyName);
        }

        #region 発生中のエラーを保持する処理を実装
        readonly Dictionary<string, List<string>> _currentErrors = new Dictionary<string, List<string>>();

        protected void AddError(string propertyName, string error)
        {
            if (!_currentErrors.ContainsKey(propertyName))
                _currentErrors[propertyName] = new List<string>();

            if (!_currentErrors[propertyName].Contains(error))
            {
                _currentErrors[propertyName].Add(error);
                OnErrorsChanged(propertyName);
            }
        }

        protected void RemoveError(string propertyName)
        {
            if (_currentErrors.ContainsKey(propertyName))
                _currentErrors.Remove(propertyName);

            OnErrorsChanged(propertyName);
        }
        #endregion

        private void OnErrorsChanged(string propertyName)
        {
            var h = this.ErrorsChanged;
            if (h != null)
            {
                h(this, new DataErrorsChangedEventArgs(propertyName));
            }
        }

        #region INotifyDataErrorInfoの実装
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public System.Collections.IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) ||
                !_currentErrors.ContainsKey(propertyName))
                return null;

            return _currentErrors[propertyName];
        }

        public bool HasErrors
        {
            get { return _currentErrors.Count > 0; }
        }
        #endregion

    }
    public class IntAttribute : ValidationAttribute
    {
        public IntAttribute(string errorMessage) : base(errorMessage)
        {
        }

        public override bool IsValid(object value)
        {
            int temp;
            return int.TryParse(value?.ToString(), out temp);
        }
    }

}
