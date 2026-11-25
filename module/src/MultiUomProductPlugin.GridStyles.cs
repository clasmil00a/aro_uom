using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Aronium.Pos.MultiUom
{
    public sealed partial class MultiUomProductPlugin
    {
        private static DataGridTemplateColumn CheckTemplateColumn(string header, string path, double width)
        {
            var template = new DataTemplate(typeof(CheckBox));
            var box = new FrameworkElementFactory(typeof(CheckBox));
            box.SetBinding(CheckBox.IsCheckedProperty, new Binding(path)
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            box.SetValue(Control.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            box.SetValue(Control.VerticalAlignmentProperty, VerticalAlignment.Center);
            box.SetValue(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Center);
            box.SetValue(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center);
            box.SetValue(FrameworkElement.MarginProperty, new Thickness(0));
            box.SetValue(FrameworkElement.WidthProperty, 28d);
            box.SetValue(FrameworkElement.HeightProperty, 28d);
            box.SetValue(Control.TemplateProperty, CenteredCheckTemplate());
            template.VisualTree = box;
            return new DataGridTemplateColumn { Header = header, Width = width, CellTemplate = template };
        }

        private static ControlTemplate CenteredCheckTemplate()
        {
            var template = new ControlTemplate(typeof(CheckBox));
            var root = new FrameworkElementFactory(typeof(Grid));
            root.SetValue(FrameworkElement.WidthProperty, 20d);
            root.SetValue(FrameworkElement.HeightProperty, 20d);
            root.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            root.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

            var border = new FrameworkElementFactory(typeof(Border));
            border.Name = "CheckBoxBorder";
            border.SetValue(FrameworkElement.WidthProperty, 18d);
            border.SetValue(FrameworkElement.HeightProperty, 18d);
            border.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            border.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1.5));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(3));
            border.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(175, 190, 197)));
            border.SetValue(Border.BackgroundProperty, Brushes.Transparent);

            var check = new FrameworkElementFactory(typeof(Path));
            check.Name = "CheckMark";
            check.SetValue(Path.DataProperty, Geometry.Parse("M 0,4 L 3.5,7.5 L 9,0"));
            check.SetValue(FrameworkElement.WidthProperty, 12d);
            check.SetValue(FrameworkElement.HeightProperty, 12d);
            check.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            check.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            check.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
            check.SetValue(Shape.StretchProperty, Stretch.Uniform);
            check.SetValue(Shape.StrokeProperty, Brushes.White);
            check.SetValue(Shape.StrokeThicknessProperty, 2d);

            root.AppendChild(border);
            root.AppendChild(check);
            template.VisualTree = root;

            var checkedTrigger = new Trigger { Property = CheckBox.IsCheckedProperty, Value = true };
            var visibleCheck = new Setter(UIElement.VisibilityProperty, Visibility.Visible) { TargetName = "CheckMark" };
            var checkedBackground = new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(64, 132, 180))) { TargetName = "CheckBoxBorder" };
            checkedTrigger.Setters.Add(visibleCheck);
            checkedTrigger.Setters.Add(checkedBackground);
            template.Triggers.Add(checkedTrigger);
            return template;
        }

        private static Brush GetDefaultForeground(DataGrid grid)
        {
            Style style = grid.TryFindResource(typeof(DataGridCell)) as Style;
            while (style != null)
            {
                foreach (Setter setter in style.Setters)
                    if (setter.Property == Control.ForegroundProperty && setter.Value is Brush)
                        return (Brush)setter.Value;
                style = style.BasedOn;
            }
            return Brushes.White;
        }

        private void ApplyGridStyle(DataGrid grid)
        {
            var rowStyle = new Style(typeof(DataGridRow));
            rowStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            rowStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
            rowStyle.Setters.Add(new Setter(Control.FocusVisualStyleProperty, null));
            var selectedTrigger = new DataTrigger
            {
                Binding = new Binding("IsSelected") { RelativeSource = RelativeSource.Self },
                Value = true
            };
            selectedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, SelectedRowBrush));
            rowStyle.Triggers.Add(selectedTrigger);
            grid.RowStyle = rowStyle;

            var cellStyle = new Style(typeof(DataGridCell));
            cellStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            cellStyle.Setters.Add(new Setter(Control.ForegroundProperty, GetDefaultForeground(grid)));
            cellStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
            cellStyle.Setters.Add(new Setter(Control.FocusVisualStyleProperty, null));
            cellStyle.Setters.Add(new Setter(Control.VerticalAlignmentProperty, VerticalAlignment.Center));
            cellStyle.Setters.Add(new Setter(Control.HorizontalAlignmentProperty, HorizontalAlignment.Stretch));
            grid.CellStyle = cellStyle;

            var textStyle = new Style(typeof(TextBox));
            textStyle.Setters.Add(new Setter(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center));
            grid.Resources[typeof(TextBox)] = textStyle;

            var checkStyle = new Style(typeof(CheckBox));
            checkStyle.Setters.Add(new Setter(Control.MinWidthProperty, 28d));
            checkStyle.Setters.Add(new Setter(Control.MinHeightProperty, 28d));
            checkStyle.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0)));
            checkStyle.Setters.Add(new Setter(Control.HorizontalAlignmentProperty, HorizontalAlignment.Center));
            checkStyle.Setters.Add(new Setter(Control.VerticalAlignmentProperty, VerticalAlignment.Center));
            grid.Resources[typeof(CheckBox)] = checkStyle;
            grid.SelectionChanged += OnGridSelectionChanged;
            grid.AddHandler(FrameworkElement.LoadedEvent, new RoutedEventHandler(OnGridElementLoaded));
            grid.PreviewMouseLeftButtonDown += OnGridPreviewMouseLeftButtonDown;
        }

        private static readonly Brush SelectedRowBrush = new SolidColorBrush(Color.FromRgb(64, 132, 180));

        private void OnGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var grid = sender as DataGrid;
            if (grid == null) return;
            foreach (object item in e.AddedItems)
            {
                var row = grid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                if (row != null) row.Background = SelectedRowBrush;
            }
            foreach (object item in e.RemovedItems)
            {
                var row = grid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                if (row != null) row.Background = Brushes.Transparent;
            }
        }

        private void OnGridElementLoaded(object sender, RoutedEventArgs e)
        {
            var row = e.OriginalSource as DataGridRow;
            if (row != null)
            {
                row.Background = row.IsSelected ? SelectedRowBrush : Brushes.Transparent;
                row.BorderThickness = new Thickness(0);
            }
            var cell = e.OriginalSource as DataGridCell;
            if (cell != null)
            {
                cell.BorderThickness = new Thickness(0);
                cell.FocusVisualStyle = null;
                cell.VerticalAlignment = VerticalAlignment.Center;
            }
        }

        private void OnGridPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as DataGrid;
            var source = e.OriginalSource as DependencyObject;
            if (grid == null || source == null) return;
            if (source is DataGridColumnHeader || FindVisualParent<DataGridColumnHeader>(source) != null) return;
            if (FindVisualParent<ScrollBar>(source) != null) return;
            if (FindVisualParent<DataGridRow>(source) != null) return;
            grid.SelectedItem = null;
            grid.CurrentItem = null;
        }

        private void OnGridPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (pageScroller == null) return;
            var grid = sender as DataGrid;
            ScrollViewer inner = grid == null ? null : FindVisualChild<ScrollViewer>(grid);
            bool atTop = inner == null || inner.VerticalOffset <= 0;
            bool atBottom = inner == null || inner.VerticalOffset >= inner.ScrollableHeight - 0.5;
            bool canForward = e.Delta < 0 ? atTop : atBottom;
            if (!canForward || pageScroller.ScrollableHeight <= 0) return;
            e.Handled = true;
            double offset = pageScroller.VerticalOffset - Math.Sign(e.Delta) * 48d;
            pageScroller.ScrollToVerticalOffset(Math.Max(0, offset));
        }

        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject current = child;
            while (current != null)
            {
                var typed = current as T;
                if (typed != null) return typed;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                var typed = child as T;
                if (typed != null) return typed;
                T nested = FindVisualChild<T>(child);
                if (nested != null) return nested;
            }
            return null;
        }
    }
}
