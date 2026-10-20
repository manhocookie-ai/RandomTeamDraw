using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace RandomTeamDraw.Models;

public sealed class Participant : INotifyPropertyChanged
{
    private string _name;
    private string _draftName;
    private bool _isEditing;
    private bool _isHighlighted;
    private string _displayTeamName = string.Empty;

    [JsonConstructor]
    public Participant(Guid id, string name, int teamNumber)
    {
        Id = id;
        _name = name;
        _draftName = name;
        TeamNumber = teamNumber;
    }

    public Guid Id { get; }
    public string Name
    {
        get => _name;
        private set => SetField(ref _name, value);
    }
    public int TeamNumber { get; }

    [JsonIgnore]
    public string DraftName
    {
        get => _draftName;
        set => SetField(ref _draftName, value);
    }

    [JsonIgnore]
    public bool IsEditing
    {
        get => _isEditing;
        set => SetField(ref _isEditing, value);
    }

    [JsonIgnore]
    public bool IsHighlighted
    {
        get => _isHighlighted;
        set => SetField(ref _isHighlighted, value);
    }

    [JsonIgnore]
    public string DisplayTeamName
    {
        get => _displayTeamName;
        set => SetField(ref _displayTeamName, value);
    }

    public void BeginEdit()
    {
        DraftName = Name;
        IsEditing = true;
    }

    public void CancelEdit()
    {
        DraftName = Name;
        IsEditing = false;
    }

    public void CommitName(string name)
    {
        Name = name;
        DraftName = name;
        IsEditing = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
