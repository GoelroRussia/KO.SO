using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteelBar.Models.CopyState;
using SteelBar.Utils.CopyState;

namespace SteelBar.ViewModels.CopyState
{
    public partial class PasteStateViewModel : ObservableObject
    {
        private readonly Document _doc;
        private readonly Action _closeAction;
        private readonly ViewStateService _service;

        // Danh sách gốc chứa toàn bộ View
        public ObservableCollection<ViewItem> Views { get; } = new();

        // Danh sách dùng để hiển thị lên UI (Hỗ trợ Search Filter)
        public ICollectionView FilteredViews { get; }

        // Biến Search gắn với Textbox trên XAML
        [ObservableProperty]
        private string _searchText = string.Empty;

        public PasteStateViewModel(Document doc, Action closeAction, ICollection<ElementId> preSelectedIds)
        {
            _doc = doc;
            _closeAction = closeAction;
            _service = new ViewStateService();

            LoadViews(preSelectedIds);

            // Khởi tạo CollectionView và gắn hàm lọc dữ liệu
            FilteredViews = CollectionViewSource.GetDefaultView(Views);
            FilteredViews.Filter = FilterViewItem;
        }

        // Hàm này tự động chạy mỗi khi người dùng gõ chữ vào thanh Search
        partial void OnSearchTextChanged(string value)
        {
            FilteredViews.Refresh();
        }

        // Logic tìm kiếm (Không phân biệt hoa thường)
        private bool FilterViewItem(object obj)
        {
            if (obj is not ViewItem item) return false;
            if (string.IsNullOrWhiteSpace(SearchText)) return true;

            return item.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                   item.ViewType.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        }

        private void LoadViews(ICollection<ElementId> preSelectedIds)
        {
            var allViews = new FilteredElementCollector(_doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => !v.IsTemplate && v.CanBePrinted)
                .OrderBy(v => v.ViewType.ToString())
                .ThenBy(v => v.Name);

            foreach (var view in allViews)
            {
                bool isSelected = preSelectedIds.Contains(view.Id) || view.Id == _doc.ActiveView?.Id;
                Views.Add(new ViewItem(view, isSelected));
            }
        }

        [RelayCommand]
        private void Paste()
        {
            // Lấy ra các View được chọn (từ danh sách gốc)
            var selectedViews = Views.Where(x => x.IsSelected).Select(x => x.RevitView).ToList();

            if (!selectedViews.Any())
            {
                TaskDialog.Show("Warning", "Please select at least one view to paste.");
                return;
            }

            using var t = new Transaction(_doc, $"Paste State to {selectedViews.Count} views");
            t.Start();
            try
            {
                foreach (var view in selectedViews)
                {
                    try
                    {
                        _service.ApplyState(view, StateClipboard.CopiedState);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Lỗi dán view {view.Name}: {ex.Message}");
                    }
                }
                t.Commit();
                _closeAction();
            }
            catch (Exception ex)
            {
                t.RollBack();
                TaskDialog.Show("Error", "Error pasting: " + ex.Message);
            }
        }

        [RelayCommand]
        private void CheckAll()
        {
            // Chỉ Tick chọn những View ĐANG ĐƯỢC HIỂN THỊ sau khi lọc Search
            foreach (var item in FilteredViews)
            {
                if (item is ViewItem viewItem) viewItem.IsSelected = true;
            }
        }

        [RelayCommand]
        private void UncheckAll()
        {
            // Chỉ Bỏ chọn những View ĐANG ĐƯỢC HIỂN THỊ sau khi lọc Search
            foreach (var item in FilteredViews)
            {
                if (item is ViewItem viewItem) viewItem.IsSelected = false;
            }
        }

        [RelayCommand]
        private void Close() => _closeAction();
    }
}