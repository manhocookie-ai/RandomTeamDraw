namespace RandomTeamDraw.Models;

public sealed class SessionData
{
    public int Version { get; set; } = 1;
    public List<Participant> Participants { get; set; } = [];
    public Guid? LastAssignedId { get; set; }
    public int Team1Weight { get; set; }
    public int Team2Weight { get; set; }
    public int Team3Weight { get; set; }
    public int Team4Weight { get; set; }
    public string? Team1Name { get; set; }
    public string? Team2Name { get; set; }
    public string? Team3Name { get; set; }
    public string? Team4Name { get; set; }

    public int[] GetTeamWeights() =>
        [Team1Weight, Team2Weight, Team3Weight, Team4Weight];

    public string[] GetTeamNames(IReadOnlyList<string> fallbacks)
    {
        var names = new[] { Team1Name, Team2Name, Team3Name, Team4Name };
        return names.Select((name, index) =>
        {
            var normalized = name?.Trim();
            return string.IsNullOrEmpty(normalized) || normalized.Length > 20
                ? fallbacks[index]
                : normalized;
        }).ToArray();
    }
}
