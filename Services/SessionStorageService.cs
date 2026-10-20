using System.IO;
using System.Text.Json;
using RandomTeamDraw.Models;

namespace RandomTeamDraw.Services;

public sealed class SessionStorageService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public SessionStorageService(string? directoryPath = null)
    {
        DirectoryPath = directoryPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RandomTeamDraw");
    }

    public string DirectoryPath { get; }

    public string SessionPath => Path.Combine(DirectoryPath, "session.json");
    public string BackupPath => Path.Combine(DirectoryPath, "session.backup.json");

    public async Task<LoadSessionResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        var primary = await TryReadAsync(SessionPath, cancellationToken);
        if (primary.Data is not null)
            return new(primary.Data, null);

        var backup = await TryReadAsync(BackupPath, cancellationToken);
        if (backup.Data is not null)
        {
            ArchiveIfPresent(SessionPath, "primary-corrupt");
            Directory.CreateDirectory(DirectoryPath);
            File.Copy(BackupPath, SessionPath, true);
            return new(backup.Data, "저장 파일이 손상되어 백업에서 복구했습니다.");
        }

        if (primary.Exists || backup.Exists)
        {
            ArchiveIfPresent(SessionPath, "primary-corrupt");
            ArchiveIfPresent(BackupPath, "backup-corrupt");
            return new(new SessionData(), "저장 파일과 백업을 읽을 수 없습니다. 손상된 원본은 별도 파일로 보존했습니다.");
        }

        return new(new SessionData(), null);
    }

    public async Task SaveAsync(
        SessionData session,
        bool synchronizeBackup = false,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(DirectoryPath);
        var temporaryPath = Path.Combine(DirectoryPath, $"session.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(
                temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                4096, FileOptions.WriteThrough | FileOptions.Asynchronous))
            {
                await JsonSerializer.SerializeAsync(stream, session, _jsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Flush(true);
            }

            if (File.Exists(SessionPath))
                File.Replace(temporaryPath, SessionPath, BackupPath, true);
            else
                File.Move(temporaryPath, SessionPath);

            if (synchronizeBackup)
                File.Copy(SessionPath, BackupPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private async Task<ReadAttempt> TryReadAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path)) return new(false, null);

        try
        {
            await using var stream = File.OpenRead(path);
            var data = await JsonSerializer.DeserializeAsync<SessionData>(stream, _jsonOptions, cancellationToken);
            if (data is null || data.Version != 1 || data.Participants.Any(p => p.TeamNumber is < 1 or > 4))
                return new(true, null);
            return new(true, data);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return new(true, null);
        }
    }

    private void ArchiveIfPresent(string path, string label)
    {
        if (!File.Exists(path)) return;
        Directory.CreateDirectory(DirectoryPath);
        var archive = Path.Combine(DirectoryPath, $"{label}.{DateTime.Now:yyyyMMdd-HHmmss-fff}.json");
        File.Copy(path, archive, false);
    }

    private sealed record ReadAttempt(bool Exists, SessionData? Data);
}

public sealed record LoadSessionResult(SessionData Session, string? Warning);
