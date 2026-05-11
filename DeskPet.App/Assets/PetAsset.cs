using DeskPet.App.Domain;
using System.IO;
using System.Windows.Media.Imaging;

namespace DeskPet.App.Assets;

public sealed class PetAsset
{
    public PetAsset(string rootDirectory, PetManifest manifest, IReadOnlyDictionary<string, SpriteAnimation> animations)
    {
        RootDirectory = rootDirectory;
        Manifest = manifest;
        Animations = animations;
    }

    public string RootDirectory { get; }

    public PetManifest Manifest { get; }

    public IReadOnlyDictionary<string, SpriteAnimation> Animations { get; }

    public string PreviewPath => Path.Combine(RootDirectory, Manifest.Preview.Replace('/', Path.DirectorySeparatorChar));
}

public sealed class SpriteAnimation
{
    public SpriteAnimation(string name, AnimationDefinition definition, BitmapImage sheet)
    {
        Name = name;
        Definition = definition;
        Sheet = sheet;
    }

    public string Name { get; }

    public AnimationDefinition Definition { get; }

    public BitmapImage Sheet { get; }
}
