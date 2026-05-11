using System.IO;

namespace DeskPet.App.Assets;

public sealed class PetCatalogService
{
    private readonly PetAssetLoader _loader = new();

    public PetCatalog LoadCatalog()
    {
        var root = ResolvePetsRootDirectory();
        var pets = new List<PetAsset>();
        var failures = new List<PetCatalogFailure>();
        var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in Directory.EnumerateDirectories(root).OrderBy(path => path))
        {
            if (!File.Exists(Path.Combine(directory, "manifest.json")))
            {
                continue;
            }

            try
            {
                var asset = _loader.Load(directory);
                if (!seenIds.Add(asset.Manifest.PetId))
                {
                    failures.Add(new PetCatalogFailure(directory, $"Duplicate pet id: {asset.Manifest.PetId}."));
                    continue;
                }

                pets.Add(asset);
            }
            catch (Exception ex)
            {
                failures.Add(new PetCatalogFailure(directory, ex.Message));
            }
        }

        return new PetCatalog(pets, failures);
    }

    public static string ResolvePetsRootDirectory()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "assets", "pets"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "assets", "pets")),
            Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "assets", "pets"))
        };

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("Could not locate assets/pets.");
    }
}
