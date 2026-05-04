using Microsoft.VisualBasic.FileIO;
using System.IO;

namespace DeskPet.App.Platform;

public sealed class FileFeedService
{
    private readonly string _appDirectory;
    private readonly string _assetDirectory;

    public FileFeedService(string appDirectory, string assetDirectory)
    {
        _appDirectory = Path.GetFullPath(appDirectory);
        _assetDirectory = Path.GetFullPath(assetDirectory);
    }

    public FeedValidationResult Validate(IEnumerable<string> paths)
    {
        var list = paths.Where(path => !string.IsNullOrWhiteSpace(path)).Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (list.Count == 0)
        {
            return FeedValidationResult.Fail("没有可投喂的文件。");
        }

        foreach (var path in list)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                return FeedValidationResult.Fail($"路径不存在：{path}");
            }

            if (IsDangerousPath(path))
            {
                return FeedValidationResult.Fail($"这个路径不能投喂：{path}");
            }
        }

        return FeedValidationResult.Success(list);
    }

    public FeedResult MoveToRecycleBin(IEnumerable<string> paths)
    {
        var successes = 0;
        var failures = new List<string>();

        foreach (var path in paths)
        {
            try
            {
                if (File.Exists(path))
                {
                    FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    successes++;
                }
                else if (Directory.Exists(path))
                {
                    FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    successes++;
                }
            }
            catch (Exception ex)
            {
                failures.Add($"{path}: {ex.Message}");
            }
        }

        return new FeedResult(successes, failures);
    }

    private bool IsDangerousPath(string path)
    {
        var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        return IsSameOrChild(fullPath, windows)
            || IsSameOrChild(fullPath, programFiles)
            || IsSameOrChild(fullPath, programFilesX86)
            || IsSameOrChild(fullPath, _appDirectory)
            || IsSameOrChild(fullPath, _assetDirectory);
    }

    private static bool IsSameOrChild(string path, string parent)
    {
        if (string.IsNullOrWhiteSpace(parent))
        {
            return false;
        }

        var normalizedParent = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return path.Equals(normalizedParent, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(normalizedParent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record FeedValidationResult(bool IsValid, IReadOnlyList<string> Paths, string? Error)
{
    public static FeedValidationResult Success(IReadOnlyList<string> paths) => new(true, paths, null);

    public static FeedValidationResult Fail(string error) => new(false, Array.Empty<string>(), error);
}

public sealed record FeedResult(int SuccessCount, IReadOnlyList<string> Failures);
