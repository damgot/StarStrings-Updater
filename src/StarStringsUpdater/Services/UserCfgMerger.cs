namespace StarStringsUpdater.Services;

/// <summary>
/// Pure USER.cfg merge/cleanup logic (no UI dependency), applying the rules:
/// - if the target file doesn't exist, the zip's file is copied as-is;
/// - if a "g_language" line already exists in the target file, it is replaced by the zip's line;
/// - otherwise, the zip's file content is appended to the end of the target file.
/// On uninstall, the "g_language" line is removed but the file itself is kept.
/// </summary>
public static class UserCfgMerger
{
    private const string LanguageKey = "g_language";

    public static void Apply(string sourceUserCfgPath, string targetUserCfgPath)
    {
        if (!File.Exists(targetUserCfgPath))
        {
            File.Copy(sourceUserCfgPath, targetUserCfgPath);
            return;
        }

        var sourceLines = File.ReadAllLines(sourceUserCfgPath);
        var targetLines = File.ReadAllLines(targetUserCfgPath).ToList();

        var targetLanguageLineIndex = targetLines.FindIndex(IsLanguageLine);

        if (targetLanguageLineIndex < 0)
        {
            targetLines.AddRange(sourceLines);
        }
        else
        {
            var sourceLanguageLineIndex = Array.FindIndex(sourceLines, IsLanguageLine);
            if (sourceLanguageLineIndex >= 0)
            {
                targetLines[targetLanguageLineIndex] = sourceLines[sourceLanguageLineIndex];
            }
        }

        File.WriteAllLines(targetUserCfgPath, targetLines);
    }

    public static void RemoveLanguageLine(string userCfgPath)
    {
        var lines = File.ReadAllLines(userCfgPath);
        var remainingLines = lines.Where(line => !IsLanguageLine(line)).ToArray();

        if (remainingLines.Length != lines.Length)
        {
            File.WriteAllLines(userCfgPath, remainingLines);
        }
    }

    private static bool IsLanguageLine(string line) =>
        line.TrimStart().StartsWith(LanguageKey, StringComparison.OrdinalIgnoreCase);
}
