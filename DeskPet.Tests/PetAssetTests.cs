using DeskPet.App.Assets;
using DeskPet.App.Platform;
using DeskPet.App.Storage;
using System.IO;

namespace DeskPet.Tests;

public sealed class PetAssetTests
{
    [Fact]
    public void OrangeCatManifestLoadsAndMatchesSpriteSheets()
    {
        var asset = new PetAssetLoader().Load(FindOrangeCatDirectory());

        Assert.Equal(1, asset.Manifest.FormatVersion);
        Assert.Equal("Mimi", asset.Manifest.Name);
        Assert.Equal(64, asset.Manifest.Canvas.Width);
        Assert.Equal(64, asset.Manifest.Canvas.Height);

        AssertAnimation(asset, "idle", 4, 4, true);
        AssertAnimation(asset, "walk_right", 6, 8, true);
        AssertAnimation(asset, "walk_left", 6, 8, true);
        AssertAnimation(asset, "sleep", 4, 3, true);
        AssertAnimation(asset, "play", 6, 10, false);
        AssertAnimation(asset, "play_left", 6, 10, false);
        AssertAnimation(asset, "eat", 6, 8, false);
        AssertAnimation(asset, "reject", 3, 5, false);
        AssertAnimation(asset, "dragged", 2, 4, true);
    }

    [Fact]
    public void FeedValidatorRejectsAppAndAssetDirectories()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var assetDirectory = FindOrangeCatDirectory();
        var service = new FileFeedService(AppContext.BaseDirectory, assetDirectory);

        Assert.False(service.Validate([AppContext.BaseDirectory]).IsValid);
        Assert.False(service.Validate([assetDirectory]).IsValid);
        Assert.True(service.Validate([Path.Combine(root, "桌面宠物.md")]).IsValid);
    }

    [Fact]
    public void PetCatalogScansOrangeCatAsCat()
    {
        var catalog = new PetCatalogService().LoadCatalog();

        var cat = catalog.Cats.FirstOrDefault(pet => pet.Manifest.PetId == "orange_cat_builtin");
        Assert.NotNull(cat);
        Assert.Equal("orange_cat_builtin", cat.Manifest.PetId);
        Assert.Equal("cat", cat.Manifest.Species);
        Assert.Empty(catalog.Failures);
    }

    [Fact]
    public void LegacySettingsMigrateToActivePetSettings()
    {
        var settings = new AppSettings
        {
            Left = 100,
            Top = 200,
            Scale = 1.5,
            AnimationSpeed = 1.25
        };

        settings.EnsureMigrated();

        Assert.Equal("orange_cat_builtin", settings.ActivePetId);
        var pet = settings.ActivePet();
        Assert.Equal(100, pet.Left);
        Assert.Equal(200, pet.Top);
        Assert.Equal(1.5, pet.Scale);
        Assert.Equal(1.25, pet.AnimationSpeed);
    }

    private static void AssertAnimation(PetAsset asset, string name, int frames, int fps, bool loop)
    {
        var animation = Assert.Contains(name, asset.Animations);
        Assert.Equal(64, animation.Definition.FrameWidth);
        Assert.Equal(64, animation.Definition.FrameHeight);
        Assert.Equal(frames, animation.Definition.Frames);
        Assert.Equal(fps, animation.Definition.Fps);
        Assert.Equal(loop, animation.Definition.Loop);
        Assert.Equal(frames * 64, animation.Sheet.PixelWidth);
        Assert.Equal(64, animation.Sheet.PixelHeight);
    }

    private static string FindOrangeCatDirectory()
    {
        var directory = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(directory, "assets", "pets", "orange-cat");
            if (File.Exists(Path.Combine(candidate, "manifest.json")))
            {
                return candidate;
            }

            var parent = Directory.GetParent(directory);
            if (parent is null)
            {
                break;
            }

            directory = parent.FullName;
        }

        throw new DirectoryNotFoundException("Could not find assets/pets/orange-cat.");
    }
}
