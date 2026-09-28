using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

public static class ProjectHome
{
    const string ProjectUrl = "https://github.com/rianlu/handshaker-windows-maintained";

    public static void Attach(Window window)
    {
        var image = window.FindName("image") as Image;
        var header = image == null ? null : image.Parent as StackPanel;
        var host = header == null ? null : header.Parent as StackPanel;
        if (image == null || header == null || host == null || header.Children.Count < 2) return;

        var text = header.Children[1] as StackPanel;
        if (text == null) return;
        foreach (var child in text.Children)
        {
            var block = child as FrameworkElement;
            if (block != null) block.Margin = new Thickness(0, 0, block.Margin.Right, 0);
        }
        int index = host.Children.IndexOf(header);
        header.Children.Clear();

        var grid = new Grid { Margin = new Thickness(0, 40, 0, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        image.Margin = new Thickness(0, 0, 30, 0);
        image.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(image, 0);
        grid.Children.Add(image);

        text.Margin = new Thickness(0);
        text.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);

        var button = new Button { Width = 102, VerticalAlignment = VerticalAlignment.Center };
        button.Content = CultureInfo.CurrentUICulture.Name.StartsWith("zh") ? "项目主页" : "Project Page";
        var style = window.TryFindResource("MsgShowButtonStyle") as Style;
        if (style != null) button.Style = style;
        button.Click += Open;
        Grid.SetColumn(button, 2);
        grid.Children.Add(button);

        host.Children.Remove(header);
        host.Children.Insert(index, grid);
    }

    static void Open(object sender, RoutedEventArgs e)
    {
        Process.Start(ProjectUrl);
    }
}
