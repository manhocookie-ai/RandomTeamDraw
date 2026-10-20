using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RandomTeamDraw.Models;
using RandomTeamDraw.Services;

namespace RandomTeamDraw.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly TeamAssignmentService _assignmentService;
    private readonly SessionStorageService _storageService;
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private string _newParticipantName = string.Empty;
    private string? _validationMessage;
    private string _saveStatus = "불러오는 중";
    private Guid? _lastAssignedId;
    private readonly int[] _teamWeights = new int[4];
    private readonly string[] _teamNames = new string[4];

    public MainViewModel()
        : this(new TeamAssignmentService(), new SessionStorageService(), new ConfigService().Load())
    {
    }

    internal MainViewModel(
        TeamAssignmentService assignmentService,
        SessionStorageService storageService,
        ConfigLoadResult configResult)
    {
        _assignmentService = assignmentService;
        _storageService = storageService;
        _teamNames[0] = configResult.Config.Team1Name;
        _teamNames[1] = configResult.Config.Team2Name;
        _teamNames[2] = configResult.Config.Team3Name;
        _teamNames[3] = configResult.Config.Team4Name;
        StartupWarning = configResult.Warning;
        AddParticipantCommand = new AsyncRelayCommand(AddParticipantAsync);
        BeginEditCommand = new RelayCommand<Participant>(participant =>
        {
            CancelAllEdits(participant);
            participant.BeginEdit();
        });
        SaveEditCommand = new RelayCommand<Participant>(participant => _ = SaveEditAsync(participant));
        CancelEditCommand = new RelayCommand<Participant>(participant => participant.CancelEdit());
        DeleteParticipantCommand = new RelayCommand<Participant>(participant => _ = DeleteParticipantAsync(participant));
    }

    public ObservableCollection<Participant> Participants { get; } = [];

    public string NewParticipantName
    {
        get => _newParticipantName;
        set
        {
            if (SetField(ref _newParticipantName, value))
                ValidationMessage = null;
        }
    }

    public string? ValidationMessage
    {
        get => _validationMessage;
        private set => SetField(ref _validationMessage, value);
    }

    public string SaveStatus
    {
        get => _saveStatus;
        private set
        {
            if (SetField(ref _saveStatus, value)) OnPropertyChanged(nameof(IsSaveError));
        }
    }

    public bool IsSaveError => SaveStatus == "저장 실패";
    public Guid? LastAssignedId => _lastAssignedId;
    public Participant? LastAssignedParticipant => Participants.FirstOrDefault(p => p.Id == _lastAssignedId);
    public string Team1Name => _teamNames[0];
    public string Team2Name => _teamNames[1];
    public string Team3Name => _teamNames[2];
    public string Team4Name => _teamNames[3];
    public int TotalCount => Participants.Count;
    public int Team1Count => GetTeamCount(1);
    public int Team2Count => GetTeamCount(2);
    public int Team3Count => GetTeamCount(3);
    public int Team4Count => GetTeamCount(4);
    public IEnumerable<Participant> Team1Participants => GetTeam(1);
    public IEnumerable<Participant> Team2Participants => GetTeam(2);
    public IEnumerable<Participant> Team3Participants => GetTeam(3);
    public IEnumerable<Participant> Team4Participants => GetTeam(4);
    public string? StartupWarning { get; private set; }

    public ICommand AddParticipantCommand { get; }
    public ICommand BeginEditCommand { get; }
    public ICommand SaveEditCommand { get; }
    public ICommand CancelEditCommand { get; }
    public ICommand DeleteParticipantCommand { get; }

    public async Task InitializeAsync()
    {
        try
        {
            var result = await _storageService.LoadAsync();
            Array.Copy(result.Session.GetTeamNames(_teamNames), _teamNames, 4);
            foreach (var participant in result.Session.Participants)
            {
                participant.DisplayTeamName = GetTeamName(participant.TeamNumber);
                participant.IsHighlighted = participant.Id == result.Session.LastAssignedId;
                Participants.Add(participant);
            }
            _lastAssignedId = result.Session.LastAssignedId;
            Array.Copy(result.Session.GetTeamWeights(), _teamWeights, 4);
            StartupWarning = CombineWarnings(StartupWarning, result.Warning);
            SaveStatus = result.Warning is null ? "저장됨" : "복구됨";
            NotifyRosterChanged();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StartupWarning = CombineWarnings(StartupWarning, $"저장 데이터를 불러오지 못했습니다.\n{ex.Message}");
            SaveStatus = "저장 실패";
        }
    }

    public async Task ResetAsync()
    {
        Participants.Clear();
        _lastAssignedId = null;
        ValidationMessage = null;
        NotifyRosterChanged();
        await SaveAsync(synchronizeBackup: true);
    }

    public int[] GetTeamWeights() => (int[])_teamWeights.Clone();
    public string[] GetTeamNames() => (string[])_teamNames.Clone();

    public async Task SetTeamSettingsAsync(IReadOnlyList<string> names, IReadOnlyList<int> weights)
    {
        if (names.Count != 4) throw new ArgumentException("조 이름은 4개여야 합니다.", nameof(names));
        if (weights.Count != 4) throw new ArgumentException("조 가중치는 4개여야 합니다.", nameof(weights));
        for (var index = 0; index < 4; index++)
        {
            var name = names[index].Trim();
            if (name.Length is 0 or > 20)
                throw new ArgumentException("조 이름은 공백이 아닌 20자 이하 문자열이어야 합니다.", nameof(names));
            _teamNames[index] = name;
            _teamWeights[index] = Math.Clamp(weights[index], -10, 10);
        }

        foreach (var participant in Participants)
            participant.DisplayTeamName = GetTeamName(participant.TeamNumber);
        NotifyTeamNamesChanged();
        await SaveAsync();
    }

    private async Task AddParticipantAsync()
    {
        var name = NormalizeAndValidate(NewParticipantName, null);
        if (name is null) return;

        var participant = new Participant(Guid.NewGuid(), name, _assignmentService.ChooseTeam(Participants, _teamWeights));
        participant.DisplayTeamName = GetTeamName(participant.TeamNumber);
        foreach (var existing in Participants) existing.IsHighlighted = false;
        participant.IsHighlighted = true;
        Participants.Add(participant);
        _lastAssignedId = participant.Id;
        NewParticipantName = string.Empty;
        ValidationMessage = null;
        NotifyRosterChanged();
        await SaveAsync();
    }

    private async Task SaveEditAsync(Participant participant)
    {
        var name = NormalizeAndValidate(participant.DraftName, participant.Id);
        if (name is null) return;

        participant.CommitName(name);
        ValidationMessage = null;
        NotifyRosterChanged();
        await SaveAsync();
    }

    private async Task DeleteParticipantAsync(Participant participant)
    {
        Participants.Remove(participant);
        if (_lastAssignedId == participant.Id) _lastAssignedId = null;
        ValidationMessage = null;
        NotifyRosterChanged();
        await SaveAsync();
    }

    private async Task SaveAsync(bool synchronizeBackup = false)
    {
        await _saveLock.WaitAsync();
        SaveStatus = "저장 중";
        try
        {
            var snapshot = new SessionData
            {
                Participants = Participants
                    .Select(p => new Participant(p.Id, p.Name, p.TeamNumber))
                    .ToList(),
                LastAssignedId = _lastAssignedId,
                Team1Weight = _teamWeights[0],
                Team2Weight = _teamWeights[1],
                Team3Weight = _teamWeights[2],
                Team4Weight = _teamWeights[3],
                Team1Name = _teamNames[0],
                Team2Name = _teamNames[1],
                Team3Name = _teamNames[2],
                Team4Name = _teamNames[3]
            };
            await _storageService.SaveAsync(snapshot, synchronizeBackup);
            SaveStatus = "저장됨";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            SaveStatus = "저장 실패";
            ValidationMessage = $"저장하지 못했습니다: {ex.Message}";
        }
        finally
        {
            _saveLock.Release();
        }
    }

    private string? NormalizeAndValidate(string? input, Guid? editingId)
    {
        var name = input?.Trim() ?? string.Empty;
        if (name.Length == 0)
        {
            ValidationMessage = "이름을 입력해 주세요.";
            return null;
        }
        if (name.Length > 30)
        {
            ValidationMessage = "이름은 30자까지 입력할 수 있습니다.";
            return null;
        }
        if (Participants.Any(p => p.Id != editingId && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            ValidationMessage = "이미 등록된 이름입니다.";
            return null;
        }
        return name;
    }

    private void CancelAllEdits(Participant except)
    {
        foreach (var participant in Participants.Where(p => p != except && p.IsEditing))
            participant.CancelEdit();
    }

    private IEnumerable<Participant> GetTeam(int teamNumber) =>
        Participants.Where(p => p.TeamNumber == teamNumber);

    private int GetTeamCount(int teamNumber) => Participants.Count(p => p.TeamNumber == teamNumber);

    private string GetTeamName(int teamNumber) =>
        teamNumber is >= 1 and <= 4 ? _teamNames[teamNumber - 1] : $"{teamNumber}조";

    private void NotifyTeamNamesChanged()
    {
        OnPropertyChanged(nameof(Team1Name));
        OnPropertyChanged(nameof(Team2Name));
        OnPropertyChanged(nameof(Team3Name));
        OnPropertyChanged(nameof(Team4Name));
        OnPropertyChanged(nameof(LastAssignedParticipant));
    }

    private static string? CombineWarnings(string? first, string? second)
    {
        if (string.IsNullOrWhiteSpace(first)) return second;
        if (string.IsNullOrWhiteSpace(second)) return first;
        return $"{first}\n\n{second}";
    }

    private void NotifyRosterChanged()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(Team1Count));
        OnPropertyChanged(nameof(Team2Count));
        OnPropertyChanged(nameof(Team3Count));
        OnPropertyChanged(nameof(Team4Count));
        OnPropertyChanged(nameof(Team1Participants));
        OnPropertyChanged(nameof(Team2Participants));
        OnPropertyChanged(nameof(Team3Participants));
        OnPropertyChanged(nameof(Team4Participants));
        OnPropertyChanged(nameof(LastAssignedId));
        OnPropertyChanged(nameof(LastAssignedParticipant));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
