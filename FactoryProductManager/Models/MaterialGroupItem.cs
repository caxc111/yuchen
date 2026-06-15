using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FactoryProductManager.Models
{
    /// <summary>
    /// 物料组合的子项模板（如"台面"、"门板"）。
    /// MaterialType 决定该子项可选的物料范围（按 FactoryProducts.category / texture 匹配）。
    /// </summary>
    public class MaterialGroupItem : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public int GroupId { get; set; }

        private string _itemName = string.Empty;
        public string ItemName
        {
            get => _itemName;
            set { _itemName = value; OnPropertyChanged(); }
        }

        public int ItemOrder { get; set; }

        /// <summary>
        /// 该子项可选的物料类型（多个用逗号分隔，如"石英石,大理石,岩板"）。
        /// MaterialSelectorDialog 会按此过滤 FactoryProducts。
        /// </summary>
        public string MaterialType { get; set; } = string.Empty;

        /// <summary>
        /// 单选/多选规则：Single / SingleOrNone / Multiple
        /// </summary>
        public string SelectionRule { get; set; } = SelectionRuleType.Single;

        public bool IsRequired { get; set; } = true;

        public string Prompt { get; set; } = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public static class SelectionRuleType
    {
        /// <summary>必选 1 个</summary>
        public const string Single = "Single";
        /// <summary>可选，选 0 或 1 个</summary>
        public const string SingleOrNone = "SingleOrNone";
        /// <summary>可选，选 0..N 个</summary>
        public const string Multiple = "Multiple";
    }
}
