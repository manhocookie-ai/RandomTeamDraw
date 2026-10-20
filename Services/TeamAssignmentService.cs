using System.Security.Cryptography;
using RandomTeamDraw.Models;

namespace RandomTeamDraw.Services;

public sealed class TeamAssignmentService
{
    public int ChooseTeam(IEnumerable<Participant> participants, IReadOnlyList<int>? weights = null)
    {
        var counts = new int[4];
        foreach (var participant in participants)
        {
            if (participant.TeamNumber is >= 1 and <= 4)
                counts[participant.TeamNumber - 1]++;
        }

        var scores = Enumerable.Range(0, 4)
            .Select(index => counts[index] + (weights is not null && index < weights.Count ? weights[index] : 0))
            .ToArray();
        var minimum = scores.Min();
        var candidates = Enumerable.Range(1, 4)
            .Where(team => scores[team - 1] == minimum)
            .ToArray();

        return candidates[RandomNumberGenerator.GetInt32(candidates.Length)];
    }
}
