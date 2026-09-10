using Blasphemous.Framework.Levels;

namespace Blasphemous.NewbieEltonLibs.Storage;

/// <summary>
/// Describes how a consuming mod imports a sprite.
/// </summary>
public class SpriteImportInfo
{
    /// <summary>
    /// Creates sprite import information with default pixel density and pivot values.
    /// </summary>
    public SpriteImportInfo()
    {
    }

    /// <summary>
    /// Gets or sets the unique identifier of the sprite.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets how many pixels occupy one unit of world space.
    /// </summary>
    public int PixelsPerUnit { get; set; } = 32;

    /// <summary>
    /// Gets or sets the normalized position where the sprite is anchored.
    /// </summary>
    public Vector Pivot { get; set; } = new Vector(0.5f, 0.5f, 0.5f);
}
