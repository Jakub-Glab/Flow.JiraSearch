using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Flow.JiraSearch.Settings;

public partial class SettingsView
{
    private bool _isInitializingTokenBox;

    public SettingsView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => SyncTokenBoxWithSettings();
        Loaded += (_, _) => SyncTokenBoxWithSettings();
    }

    private void TokenBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isInitializingTokenBox)
            return;

        if (DataContext is SettingsViewModel vm)
            vm.Settings.ApiToken = ((PasswordBox)sender).Password;
    }

    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !int.TryParse(e.Text, out _);
    }

    private void SyncTokenBoxWithSettings()
    {
        if (DataContext is not SettingsViewModel vm)
            return;

        _isInitializingTokenBox = true;
        TokenBox.Password = vm.Settings.ApiToken ?? string.Empty;
        _isInitializingTokenBox = false;
    }
}
