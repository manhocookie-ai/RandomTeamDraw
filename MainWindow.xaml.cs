using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using RandomTeamDraw.ViewModels;

namespace RandomTeamDraw;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private bool _initialized;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_initialized) return;
        _initialized = true;
        await _viewModel.InitializeAsync();
        NameInput.Focus();

        if (!string.IsNullOrWhiteSpace(_viewModel.StartupWarning))
            MessageBox.Show(this, _viewModel.StartupWarning, "데이터 복구 안내", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void NameInput_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // IME 조합 확정에 사용된 Enter는 Key.ImeProcessed로 전달되므로 등록하지 않는다.
        if (e.Key == Key.ImeProcessed) return;
        if (e.Key != Key.Enter) return;

        e.Handled = true;
        if (_viewModel.AddParticipantCommand.CanExecute(null))
            _viewModel.AddParticipantCommand.Execute(null);
        FocusNameInput();
    }

    private void AddButton_OnClick(object sender, RoutedEventArgs e) => FocusNameInput();

    private void WeightSettings_OnExecuted(object sender, ExecutedRoutedEventArgs e) => OpenSettings();

    private void WeightSettingsButton_OnClick(object sender, RoutedEventArgs e) => OpenSettings();

    private void OpenSettings()
    {
        var window = new TeamWeightSettingsWindow(
            _viewModel.Team1Name,
            _viewModel.Team2Name,
            _viewModel.Team3Name,
            _viewModel.Team4Name,
            _viewModel.GetTeamWeights())
        {
            Owner = this
        };

        if (window.ShowDialog() == true)
            _ = SaveTeamSettingsAsync(window.TeamNames, window.TeamWeights);
    }

    private async Task SaveTeamSettingsAsync(IReadOnlyList<string> names, IReadOnlyList<int> weights)
    {
        await _viewModel.SetTeamSettingsAsync(names, weights);
        FocusNameInput();
    }

    private async void ResetButton_OnClick(object sender, RoutedEventArgs e)
    {
        var answer = MessageBox.Show(
            this,
            "모든 참가자와 조 편성 결과를 초기화할까요?",
            "전체 초기화",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);

        if (answer == MessageBoxResult.Yes)
            await _viewModel.ResetAsync();
        FocusNameInput();
    }

    private void FocusNameInput() => Dispatcher.BeginInvoke(
        DispatcherPriority.Input,
        () =>
        {
            NameInput.Focus();
            Keyboard.Focus(NameInput);
        });
}
