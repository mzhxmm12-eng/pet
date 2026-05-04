using DeskPet.App.Domain;
using System.IO;
using System.Text.Json;
using System.Windows.Media.Imaging;

namespace DeskPet.App.Assets;

public sealed class PetAssetLoader
{
    private static readonly string[] RequiredAnimations =
    [
        "idle",
        "walk_left",
        "walk_right",
        "sleep",
        "alert",
        "play",
        "eat",
        "reject"
    ];

    public PetAsset Load(string rootDirectory)
    {
        var manifestPath = Path.Combine(rootDirectory, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("Pet manifest was not found.", manifestPath);
        }

        var manifest = JsonSerializer.Deserialize<PetManifest>(
            File.ReadAllText(manifestPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Pet manifest is empty or invalid.");

        ValidateManifest(rootDirectory, manifest);

        var animations = new Dictionary<string, SpriteAnimation>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, definition) in manifest.Animations)
        {
            var filePath = Path.Combine(rootDirectory, definition.File.Replace('/', Path.DirectorySeparatorChar));
            var image = LoadBitmap(filePath);
            if (image.PixelWidth != definition.FrameWidth * definition.Frames ||
                image.PixelHeight != definition.FrameHeight)
            {
                throw new InvalidOperationException(
                    $"Animation '{name}' size mismatch. Expected {definition.FrameWidth * definition.Frames}x{definition.FrameHeight}, got {image.PixelWidth}x{image.PixelHeight}.");
            }

            animations[name] = new SpriteAnimation(name, definition, image);
        }

        return new PetAsset(rootDirectory, manifest, animations);
    }

    private static void ValidateManifest(string rootDirectory, PetManifest manifest)
    {
        if (manifest.FormatVersion != 1)
        {
            throw new InvalidOperationException($"Unsupported pet format version: {manifest.FormatVersion}.");
        }

        if (manifest.Canvas.Width <= 0 || manifest.Canvas.Height <= 0 || manifest.Canvas.Scale <= 0)
        {
            throw new InvalidOperationException("Canvas width, height, and scale must be positive.");
        }

        foreach (var required in RequiredAnimations)
        {
            if (!manifest.Animations.ContainsKey(required))
            {
                throw new InvalidOperationException($"Required animation is missing: {required}.");
            }
        }

        foreach (var (name, definition) in manifest.Animations)
        {
            if (definition.FrameWidth <= 0 || definition.FrameHeight <= 0 || definition.Frames <= 0 || definition.Fps <= 0)
            {
                throw new InvalidOperationException($"Animation '{name}' contains invalid playback values.");
            }

            var filePath = Path.GetFullPath(Path.Combine(rootDirectory, definition.File.Replace('/', Path.DirectorySeparatorChar)));
            var rootPath = Path.GetFullPath(rootDirectory);
            if (!filePath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Animation '{name}' points outside the pet directory.");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Animation file was not found: {name}.", filePath);
            }
        }
    }

    private static BitmapImage LoadBitmap(string filePath)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
        bitmap.UriSource = new Uri(Path.GetFullPath(filePath), UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}
