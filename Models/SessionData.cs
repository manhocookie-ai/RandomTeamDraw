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

    public int[] GetTeamWeights() =>
        [Team1Weight, Team2Weight, Team3Weight, Team4Weight];
}
