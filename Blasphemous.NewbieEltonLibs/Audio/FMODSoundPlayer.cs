using FMOD;

namespace Blasphemous.NewbieEltonLibs.Audio;

/// <summary>Starts playback for imported FMOD Sounds.</summary>
public static class FMODSoundPlayer
{
    /// <summary>Plays an FMOD Sound once through the low-level FMOD system that created it.</summary>
    /// <param name="lowlevelSystem">The low-level FMOD system that created the sound.</param>
    /// <param name="sound">The FMOD Sound to play.</param>
    /// <returns>The FMOD result of starting playback.</returns>
    public static RESULT PlayOnce(FMOD.System lowlevelSystem, Sound sound)
    {
        return lowlevelSystem.playSound(sound, default(ChannelGroup), false, out _);
    }
}
