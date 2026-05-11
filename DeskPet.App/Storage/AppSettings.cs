namespace DeskPet.App.Storage;

public sealed class AppSettings
{
    public string ActivePetId { get; set; } = "orange_cat_builtin";

    public Dictionary<string, PetSettings> Pets { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public bool IsTopmost { get; set; } = true;

    public double? Left { get; set; }

    public double? Top { get; set; }

    public double Scale { get; set; } = 2;

    public double AnimationSpeed { get; set; } = 1;

    public bool IsClickThrough { get; set; }

    public bool IsFeedingEnabled { get; set; } = true;

    public bool ConfirmBeforeFeeding { get; set; } = true;

    public PetSettings ActivePet()
    {
        if (!Pets.TryGetValue(ActivePetId, out var settings))
        {
            settings = new PetSettings();
            Pets[ActivePetId] = settings;
        }

        return settings;
    }

    public PetSettings PetFor(string petId)
    {
        if (!Pets.TryGetValue(petId, out var settings))
        {
            settings = new PetSettings();
            Pets[petId] = settings;
        }

        return settings;
    }

    public void EnsureMigrated()
    {
        Pets = Pets is null
            ? new Dictionary<string, PetSettings>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, PetSettings>(Pets, StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(ActivePetId))
        {
            ActivePetId = "orange_cat_builtin";
        }

        if (Pets.Count == 0)
        {
            Pets[ActivePetId] = new PetSettings
            {
                Left = Left,
                Top = Top,
                Scale = Scale,
                AnimationSpeed = AnimationSpeed
            };
        }

        foreach (var pet in Pets.Values)
        {
            pet.Scale = Math.Clamp(pet.Scale <= 0 ? 2 : pet.Scale, 0.75, 4);
            pet.MoveSpeed = Math.Clamp(pet.MoveSpeed <= 0 ? 1 : pet.MoveSpeed, 0.5, 2);
            pet.AnimationSpeed = Math.Clamp(pet.AnimationSpeed <= 0 ? 1 : pet.AnimationSpeed, 0.25, 2);
            pet.Opacity = Math.Clamp(pet.Opacity <= 0 ? 1 : pet.Opacity, 0.25, 1);
            if (string.IsNullOrWhiteSpace(pet.InteractionLevel))
            {
                pet.InteractionLevel = "medium";
            }
        }
    }
}

public sealed class PetSettings
{
    public double? Left { get; set; }

    public double? Top { get; set; }

    public double Scale { get; set; } = 2;

    public double MoveSpeed { get; set; } = 1;

    public string InteractionLevel { get; set; } = "medium";

    public double AnimationSpeed { get; set; } = 1;

    public double Opacity { get; set; } = 1;
}
