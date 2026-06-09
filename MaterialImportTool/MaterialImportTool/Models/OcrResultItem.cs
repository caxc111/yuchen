using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace MaterialImportTool.Models
{
    public class OcrResultItem : ObservableObject
    {
        private string _fieldName = string.Empty;
        public string FieldName
        {
            get => _fieldName;
            set => SetProperty(ref _fieldName, value);
        }

        private string _value = string.Empty;
        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }
    }
}
