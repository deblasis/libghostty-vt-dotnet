using System.Windows;

namespace WpfDirectExample;

public partial class MainWindow : Window
{
    private GhosttyTerminal? _terminal;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _terminal = new GhosttyTerminal();
        TerminalGrid.Children.Add(_terminal);
        _terminal.Focus();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_terminal != null)
        {
            TerminalGrid.Children.Remove(_terminal);
            _terminal.Dispose();
            _terminal = null;
        }
    }
}
