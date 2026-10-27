using System.Text.Json;

namespace ProductivityTools.TechBricks.Data.Auth
{
    /// <summary>
    /// Persists the long-lived Firebase refresh token, so the user signs in
    /// through the browser only once. The file is readable only by the current
    /// user (on Windows the user profile directory is already private).
    /// </summary>
    public class TokenStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public string FilePath { get; }

        public TokenStore() : this(DefaultPath())
        {
        }

        public TokenStore(string filePath)
        {
            FilePath = filePath;
        }

        public StoredSession? Load()
        {
            if (!File.Exists(FilePath))
            {
                return null;
            }
            try
            {
                var session = JsonSerializer.Deserialize<StoredSession>(File.ReadAllText(FilePath));
                return string.IsNullOrEmpty(session?.RefreshToken) ? null : session;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public void Save(StoredSession session)
        {
            string directory = Path.GetDirectoryName(FilePath)!;
            Directory.CreateDirectory(directory);

            string tempPath = FilePath + ".tmp";
            File.WriteAllText(tempPath, string.Empty);
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(tempPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            File.WriteAllText(tempPath, JsonSerializer.Serialize(session, JsonOptions));
            File.Move(tempPath, FilePath, overwrite: true);
        }

        public bool Delete()
        {
            if (!File.Exists(FilePath))
            {
                return false;
            }
            File.Delete(FilePath);
            return true;
        }

        private static string DefaultPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create);
            return Path.Combine(appData, "ProductivityTools.TechBricks", "session.json");
        }
    }

    public class StoredSession
    {
        public string RefreshToken { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}
