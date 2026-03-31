using Microsoft.UI.Xaml;

namespace WinUI3Example;

public sealed partial class MainWindow : Window
{
    private GhosttyTerminal? _terminal;

    public MainWindow()
    {
        InitializeComponent();
        RootGrid.Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _terminal = new GhosttyTerminal(this);
        RootGrid.Children.Add(_terminal);
        _terminal.Focus(FocusState.Programmatic);
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        if (_terminal != null)
        {
            RootGrid.Children.Remove(_terminal);
            _terminal.Dispose();
            _terminal = null;
        }
    }
}
