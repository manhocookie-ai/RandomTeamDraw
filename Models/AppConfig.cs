namespace RandomTeamDraw.Models;

public sealed class AppConfig
{
    public string Team1Name { get; set; } = "1조";
    public string Team2Name { get; set; } = "2조";
    public string Team3Name { get; set; } = "3조";
    public string Team4Name { get; set; } = "4조";

    public string GetTeamName(int teamNumber) => teamNumber switch
    {
        1 => Team1Name,
        2 => Team2Name,
        3 => Team3Name,
        4 => Team4Name,
        _ => $"{teamNumber}조"
    };
}
