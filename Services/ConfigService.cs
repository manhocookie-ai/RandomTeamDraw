using System.IO;
using System.Text.Json;
using RandomTeamDraw.Models;

namespace RandomTeamDraw.Services;

public sealed class ConfigService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public string ConfigPath { get; } = Path.Combine(AppContext.BaseDirectory, "config.json");

    public ConfigLoadResult Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
                return new(new AppConfig(), "config.json을 찾지 못해 기본 조 이름을 사용합니다.");

            var config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath), _jsonOptions)
                         ?? new AppConfig();
            config.Team1Name = Normalize(config.Team1Name, "1조");
            config.Team2Name = Normalize(config.Team2Name, "2조");
            config.Team3Name = Normalize(config.Team3Name, "3조");
            config.Team4Name = Normalize(config.Team4Name, "4조");
            return new(config, null);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return new(new AppConfig(), $"config.json을 읽지 못해 기본 조 이름을 사용합니다.\n{ex.Message}");
        }
    }

    private static string Normalize(string? name, string fallback)
    {
        var normalized = name?.Trim();
        return string.IsNullOrEmpty(normalized) || normalized.Length > 20 ? fallback : normalized;
    }
}

public sealed record ConfigLoadResult(AppConfig Config, string? Warning);
