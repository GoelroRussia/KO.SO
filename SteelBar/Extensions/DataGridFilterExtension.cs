using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace SteelBar.Extensions
{
    public static class DataGridFilterExtension
    {
        public static void ApplyExcelCheckboxFilter<T>(this ICollectionView view, Dictionary<string, HashSet<string>> activeFilters)
        {
            if (view == null) return;

            // Nếu không có filter nào hoặc tất cả đều được tick full -> Bỏ lọc
            if (activeFilters == null || activeFilters.Count == 0)
            {
                view.Filter = null;
                return;
            }

            view.Filter = item =>
            {
                if (item == null) return false;
                Type type = typeof(T);

                foreach (var filter in activeFilters)
                {
                    PropertyInfo? propertyInfo = type.GetProperty(filter.Key);
                    if (propertyInfo == null) continue;

                    var cellValue = propertyInfo.GetValue(item)?.ToString() ?? "(Trống)";

                    // Nếu giá trị của ô KHÔNG NẰM TRONG danh sách các CheckBox được tick -> Ẩn dòng đó
                    if (!filter.Value.Contains(cellValue))
                    {
                        return false;
                    }
                }
                return true;
            };

            view.Refresh();
        }
    }
    public partial class ColumnFilterItem : ObservableObject
    {
        [ObservableProperty] private string _value = string.Empty;
        [ObservableProperty] private bool _isSelected = true;
        public string ColumnName { get; set; } = string.Empty;
    }
}