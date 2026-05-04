namespace DeskPet.App.Storage;

public sealed class AppSettings
{
    public double? Left { get; set; }

    public double? Top { get; set; }

    public double Scale { get; set; } = 2;

    public double AnimationSpeed { get; set; } = 1;

    public bool IsClickThrough { get; set; }

    public bool IsFeedingEnabled { get; set; } = true;

    public bool ConfirmBeforeFeeding { get; set; } = true;
}
