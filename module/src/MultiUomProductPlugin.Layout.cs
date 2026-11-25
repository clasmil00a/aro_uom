using System.Windows;
using System.Windows.Controls;

namespace Aronium.Pos.MultiUom
{
    public sealed partial class MultiUomProductPlugin
    {
        private void BuildLayout()
        {
            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var root = new StackPanel { Margin = new Thickness(12) };
            scroll.Content = root;
            pageScroller = scroll;
            Content = scroll;

            root.Children.Add(new TextBlock { Text = "Satuan produk dan harga bertingkat", FontSize = 18, FontWeight = FontWeights.SemiBold });
            root.Children.Add(new TextBlock
            {
                Text = "Klik sel untuk mengedit. Satuan baru dapat diketik langsung; satuan dasar ditentukan otomatis dari faktor 1.",
                Margin = new Thickness(0, 4, 0, 10)
            });

            root.Children.Add(new TextBlock { Text = "Satuan produk", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 4, 0, 4) });
            root.Children.Add(unitGrid);
            root.Children.Add(UnitButtons());

            root.Children.Add(new TextBlock { Text = "Harga bertingkat", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 12, 0, 4) });
            root.Children.Add(tierGrid);
            root.Children.Add(TierButtons());
        }

        private FrameworkElement UnitButtons()
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 4) };
            var add = new Button { Content = "Tambah baris", Width = 105, Margin = new Thickness(0, 0, 6, 0) };
            var delete = new Button { Content = "Hapus baris", Width = 105 };
            add.Click += OnAddUnitRow; delete.Click += OnDeleteUnitRow;
            panel.Children.Add(add); panel.Children.Add(delete);
            return panel;
        }

        private FrameworkElement TierButtons()
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 4) };
            var add = new Button { Content = "Tambah tier", Width = 105, Margin = new Thickness(0, 0, 6, 0) };
            var delete = new Button { Content = "Hapus tier", Width = 105 };
            add.Click += OnAddTierRow; delete.Click += OnDeleteTierRow;
            panel.Children.Add(add); panel.Children.Add(delete);
            return panel;
        }
    }
}
