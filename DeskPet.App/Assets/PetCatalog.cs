namespace DeskPet.App.Assets;

public sealed class PetCatalog
{
    public PetCatalog(IReadOnlyList<PetAsset> pets, IReadOnlyList<PetCatalogFailure> failures)
    {
        Pets = pets;
        Failures = failures;
    }

    public IReadOnlyList<PetAsset> Pets { get; }

    public IReadOnlyList<PetCatalogFailure> Failures { get; }

    public IReadOnlyList<PetAsset> Cats => Pets
        .Where(pet => string.Equals(pet.Manifest.Species, "cat", StringComparison.OrdinalIgnoreCase))
        .OrderBy(pet => pet.Manifest.Name)
        .ToList();

    public PetAsset? Find(string petId) => Pets.FirstOrDefault(
        pet => string.Equals(pet.Manifest.PetId, petId, StringComparison.OrdinalIgnoreCase));
}

public sealed class PetCatalogFailure
{
    public PetCatalogFailure(string directory, string reason)
    {
        Directory = directory;
        Reason = reason;
    }

    public string Directory { get; }

    public string Reason { get; }
}
