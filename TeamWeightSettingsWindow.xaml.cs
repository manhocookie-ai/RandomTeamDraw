using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace RandomTeamDraw;

public partial class TeamWeightSettingsWindow : Window
{
    public TeamWeightSettingsWindow(string team1, string team2, string team3, string team4, IReadOnlyList<int> weights)
    {
        InitializeComponent();
        Rows = new[]
        {
            new WeightRow(team1, weights[0]),
            new WeightRow(team2, weights[1]),
            new WeightRow(team3, weights[2]),
            new WeightRow(team4, weights[3])
        };
        DataContext = this;
    }

    public IReadOnlyList<WeightRow> Rows { get; }
    public int[] TeamWeights => Rows.Select(row => row.Weight).ToArray();

    private static WeightRow? GetRow(object sender) => (sender as Button)?.Tag as WeightRow;

    private void Decrease_OnClick(object sender, RoutedEventArgs e)
    {
        var row = GetRow(sender);
        if (row is not null) row.Weight = Math.Max(-10, row.Weight - 1);
    }

    private void Increase_OnClick(object sender, RoutedEventArgs e)
    {
        var row = GetRow(sender);
        if (row is not null) row.Weight = Math.Min(10, row.Weight + 1);
    }

    private void Save_OnClick(object sender, RoutedEventArgs e) => DialogResult = true;
    private void Cancel_OnClick(object sender, RoutedEventArgs e) => DialogResult = false;

    public sealed class WeightRow : INotifyPropertyChanged
    {
        private int _weight;

        public WeightRow(string teamName, int weight)
        {
            TeamName = teamName;
            _weight = Math.Clamp(weight, -10, 10);
        }

        public string TeamName { get; }
        public int Weight
        {
            get => _weight;
            set
            {
                if (_weight == value) return;
                _weight = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Weight)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
