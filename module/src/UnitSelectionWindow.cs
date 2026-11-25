using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Aronium.Pos.MultiUom
{
    internal sealed class UnitSelectionWindow : Window
    {
        private static readonly MoneyConverter Money = new MoneyConverter();
        private readonly DataGrid grid;
        private readonly TextBox quantityBox;
        private bool quantityInputStarted;

        public ProductUnitOption SelectedUnit { get; private set; }
        public decimal Quantity { get; private set; }

        public UnitSelectionWindow(IList<ProductUnitOption> units, decimal initialQuantity)
        {
            Title = "Pilih satuan dan harga";
            Width = 760;
            Height = 480;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.CanResize;

            var root = new Grid();
            root.Margin = new Thickness(12);
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Content = root;
            Loaded += OnLoaded;
            PreviewKeyDown += OnPreviewKeyDown;
            PreviewTextInput += OnPreviewTextInput;

            var productLabel = new TextBlock();
            productLabel.Text = "Pilih satuan yang akan digunakan untuk penjualan.";
            productLabel.FontSize = 15;
            productLabel.Margin = new Thickness(0, 0, 0, 8);
            root.Children.Add(productLabel);

            grid = new DataGrid { RowHeight = 42, FontSize = 15 };
            grid.AutoGenerateColumns = false;
            grid.IsReadOnly = true;
            grid.SelectionMode = DataGridSelectionMode.Single;
            grid.ItemsSource = units;
            grid.Columns.Add(Column("Satuan", "UnitName", 230));
            grid.Columns.Add(Column("Faktor", "ConversionFactor", 110));
            grid.Columns.Add(MoneyColumn("Harga / satuan", "Price", 165));
            grid.SelectionChanged += OnSelectionChanged;
            Grid.SetRow(grid, 1);
            root.Children.Add(grid);

            var qtyPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 8) };
            qtyPanel.Children.Add(new TextBlock { Text = "Kuantitas:", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
            quantityBox = new TextBox { Width = 120, Height = 32, FontSize = 15, VerticalContentAlignment = VerticalAlignment.Center, Text = initialQuantity.ToString(CultureInfo.CurrentCulture) };
            qtyPanel.Children.Add(quantityBox);
            Grid.SetRow(qtyPanel, 2);
            root.Children.Add(qtyPanel);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var ok = new Button { Content = "OK", Width = 100, Height = 34, FontSize = 14, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
            var cancel = new Button { Content = "Batal", Width = 100, Height = 34, FontSize = 14, IsCancel = true };
            ok.Click += OnOk;
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            Grid.SetRow(buttons, 3);
            root.Children.Add(buttons);

            var hint = new TextBlock
            {
                Text = "Panah atas/bawah: pilih satuan   |   Enter: OK   |   Esc: Batal",
                Foreground = System.Windows.Media.Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            };
            Grid.SetRow(hint, 4);
            root.Children.Add(hint);

            if (units != null && units.Count > 0) grid.SelectedIndex = 0;
        }

        private static DataGridTextColumn Column(string header, string path, double width)
        {
            return new DataGridTextColumn { Header = header, Binding = new System.Windows.Data.Binding(path), Width = width };
        }

        private static DataGridTextColumn MoneyColumn(string header, string path, double width)
        {
            return new DataGridTextColumn
            {
                Header = header, Width = width,
                Binding = new System.Windows.Data.Binding(path) { Converter = Money }
            };
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            quantityInputStarted = false;
            if (grid.Items.Count > 0)
            {
                grid.SelectedIndex = 0;
                grid.Focus();
            }
            else
            {
                quantityBox.Focus();
            }
        }

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.OriginalSource is TextBox || string.IsNullOrEmpty(e.Text))
                return;
            string input = e.Text;
            bool numeric = true;
            bool decimalPoint = false;
            foreach (char c in input)
            {
                if (char.IsDigit(c)) continue;
                if ((c == '.' || c == ',') && !decimalPoint) { decimalPoint = true; continue; }
                numeric = false;
                break;
            }
            if (!numeric) return;
            string current = quantityBox.Text ?? "";
            if (!quantityInputStarted)
            {
                quantityBox.Text = input;
                quantityInputStarted = true;
            }
            else
            {
                bool hasSeparator = current.IndexOf('.') >= 0 || current.IndexOf(',') >= 0;
                if (decimalPoint && hasSeparator) return;
                quantityBox.Text = current + input;
            }
            e.Handled = true;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OnOk(this, new RoutedEventArgs());
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Back && !(e.OriginalSource is TextBox))
            {
                string text = quantityBox.Text ?? "";
                quantityBox.Text = text.Length > 0 ? text.Substring(0, text.Length - 1) : "0";
                quantityInputStarted = true;
                e.Handled = true;
                return;
            }
            if (e.Key != Key.Up && e.Key != Key.Down)
                return;
            if (e.OriginalSource is TextBox)
                return;
            int index = grid.SelectedIndex + (e.Key == Key.Down ? 1 : -1);
            if (index < 0) index = 0;
            if (index >= grid.Items.Count) index = grid.Items.Count - 1;
            grid.SelectedIndex = index;
            grid.ScrollIntoView(grid.Items[index]);
            grid.Focus();
            e.Handled = true;
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedUnit = grid.SelectedItem as ProductUnitOption;
        }

        private static bool TryQuantity(string text, out decimal value)
        {
            string normalized = (text ?? "").Trim().Replace(',', '.');
            return decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out value);
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            decimal qty;
            if (SelectedUnit == null || !TryQuantity(quantityBox.Text, out qty) || qty <= 0)
            {
                MessageBox.Show(this, "Pilih satuan dan masukkan kuantitas lebih besar dari nol.", "Multi-UOM", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            Quantity = qty;
            DialogResult = true;
        }
    }
}
