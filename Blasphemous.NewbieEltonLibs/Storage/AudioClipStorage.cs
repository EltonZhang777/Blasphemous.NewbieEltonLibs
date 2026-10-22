using Blasphemous.ModdingAPI.Files;
using Blasphemous.NewbieEltonLibs.Extensions.ModdingAPI;
using Blasphemous.NewbieEltonLibs.Extensions.System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Blasphemous.NewbieEltonLibs.Storage;

/// <summary>Stores named AudioClip references for one consuming mod without owning their lifetime.</summary>
public class AudioClipStorage
{
    private readonly Dictionary<string, AudioClip> _clips = [];
    private readonly HashSet<string> _pendingImports = [];
    private readonly Func<AudioClip, bool> _validateClip;

    /// <summary>Creates an empty AudioClip storage.</summary>
    public AudioClipStorage()
        : this(IsValidClip)
    {
    }

    internal AudioClipStorage(Func<AudioClip, bool> validateClip)
    {
        _validateClip = validateClip;
    }

    /// <summary>Gets the required AudioClip registered under a name.</summary>
    /// <param name="name">The registered AudioClip name.</param>
    /// <returns>The AudioClip registered under <paramref name="name" />.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the name is not registered.</exception>
    public AudioClip this[string name]
    {
        get
        {
            if (_clips.TryGetValue(name, out AudioClip? clip))
            {
                return clip;
            }

            throw new KeyNotFoundException("AudioClip '" + name + "' is not registered.");
        }
    }

    /// <summary>Registers an already-created AudioClip under a new name.</summary>
    /// <param name="name">The name that identifies the AudioClip.</param>
    /// <param name="clip">The AudioClip to register.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name" /> or <paramref name="clip" /> is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="name" /> is registered or has an import in progress.</exception>
    public void Register(string name, AudioClip clip)
    {
        ValidateName(name, true);
        ValidateClip(clip, true);

        if (_clips.ContainsKey(name) || _pendingImports.Contains(name))
        {
            throw new InvalidOperationException("AudioClip '" + name + "' is already registered or being imported.");
        }

        _clips.Add(name, clip);
    }

    /// <summary>Replaces an AudioClip that is already registered under a name.</summary>
    /// <param name="name">The name of the AudioClip to replace.</param>
    /// <param name="clip">The replacement AudioClip.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name" /> or <paramref name="clip" /> is invalid.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when <paramref name="name" /> is not registered.</exception>
    public void Replace(string name, AudioClip clip)
    {
        ValidateName(name, true);
        ValidateClip(clip, true);

        if (!_clips.ContainsKey(name))
        {
            throw new KeyNotFoundException("AudioClip '" + name + "' is not registered.");
        }

        _clips[name] = clip;
    }

    /// <summary>Attempts to register an AudioClip without throwing for expected failures.</summary>
    /// <param name="name">The name that identifies the AudioClip.</param>
    /// <param name="clip">The AudioClip to register.</param>
    /// <returns><see langword="true" /> when registered; otherwise, <see langword="false" /> when the name is invalid, registered, importing, or the clip is invalid.</returns>
    public bool TryRegister(string name, AudioClip clip)
    {
        if (!ValidateName(name, false) || !ValidateClip(clip, false) || _clips.ContainsKey(name) || _pendingImports.Contains(name))
        {
            return false;
        }

        _clips.Add(name, clip);
        return true;
    }

    /// <summary>Attempts to replace a registered AudioClip without throwing for expected failures.</summary>
    /// <param name="name">The name of the AudioClip to replace.</param>
    /// <param name="clip">The replacement AudioClip.</param>
    /// <returns><see langword="true" /> when replaced; otherwise, <see langword="false" />.</returns>
    public bool TryReplace(string name, AudioClip clip)
    {
        if (!ValidateName(name, false) || !ValidateClip(clip, false) || !_clips.ContainsKey(name))
        {
            return false;
        }

        _clips[name] = clip;
        return true;
    }

    /// <summary>Attempts to get a registered AudioClip without throwing when it is absent.</summary>
    /// <param name="name">The name of the AudioClip to find.</param>
    /// <param name="clip">The registered AudioClip when found; otherwise, <see langword="null" />.</param>
    /// <returns><see langword="true" /> when found; otherwise, <see langword="false" />.</returns>
    public bool TryGet(string name, out AudioClip? clip)
    {
        if (!ValidateName(name, false))
        {
            clip = null;
            return false;
        }

        return _clips.TryGetValue(name, out clip);
    }

    /// <summary>Creates and registers an AudioClip asynchronously from this mod's Data directory.</summary>
    /// <param name="fileHandler">The consuming mod's file handler.</param>
    /// <param name="name">The name under which to register the imported AudioClip.</param>
    /// <param name="relativePath">An MP3 or PCM WAV path relative to the mod's Data directory.</param>
    /// <param name="onSuccess">Called once with the requested name and registered AudioClip after a successful import.</param>
    /// <param name="onFailure">Called once with the requested name and a failure message when import fails or its coroutine is stopped before completion.</param>
    /// <returns>A coroutine that the consuming mod must start with Unity.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a callback is <see langword="null" />.</exception>
    public IEnumerator ImportAsync(FileHandler fileHandler, string name, string relativePath,
        Action<string, AudioClip> onSuccess, Action<string, string> onFailure)
    {
        if (onSuccess == null)
        {
            throw new ArgumentNullException(nameof(onSuccess));
        }

        if (onFailure == null)
        {
            throw new ArgumentNullException(nameof(onFailure));
        }

        if (!TryReserveImportName(name, out string reservationFailure))
        {
            return FailImport(name, reservationFailure, onFailure);
        }

        return ImportCore(fileHandler, name, relativePath, onSuccess, onFailure);
    }

    private IEnumerator ImportCore(FileHandler fileHandler, string name, string relativePath,
        Action<string, AudioClip> onSuccess, Action<string, string> onFailure)
    {
        WWW? request = null;
        AudioClip? clip = null;
        bool reservationHeld = true;
        bool callbackDelivered = false;

        try
        {
            string fullPath = null!;
            AudioType audioType = AudioType.UNKNOWN;
            string failure = null!;
            bool prepared = false;

            try
            {
                prepared = TryPrepareImport(fileHandler, name, relativePath, out fullPath, out audioType, out failure);
            }
            catch (Exception exception)
            {
                failure = "Could not prepare the audio import: " + exception.Message;
            }

            if (!prepared)
            {
                _pendingImports.Remove(name);
                reservationHeld = false;
                callbackDelivered = true;
                onFailure(name, failure);
                yield break;
            }

            try
            {
                request = new WWW(new Uri(fullPath).AbsoluteUri);
            }
            catch (Exception exception)
            {
                failure = "Could not start the audio import: " + exception.Message;
            }

            if (request == null)
            {
                _pendingImports.Remove(name);
                reservationHeld = false;
                callbackDelivered = true;
                onFailure(name, failure ?? "Unity could not create the audio request.");
                yield break;
            }

            yield return request;

            failure = null!;
            try
            {
                if (!string.IsNullOrEmpty(request.error))
                {
                    failure = "Could not load the audio file: " + request.error;
                }
                else
                {
                    clip = request.GetAudioClip(false, false, audioType);
                }
            }
            catch (Exception exception)
            {
                failure = "Could not decode the audio file: " + exception.Message;
            }

            try
            {
                request.Dispose();
            }
            catch (Exception exception)
            {
                if (failure == null)
                {
                    failure = "Could not release the audio request: " + exception.Message;
                }
            }
            request = null;

            if (failure != null)
            {
                DestroyUnregisteredClip(clip);
                _pendingImports.Remove(name);
                reservationHeld = false;
                callbackDelivered = true;
                onFailure(name, failure);
                yield break;
            }

            if (clip == null)
            {
                _pendingImports.Remove(name);
                reservationHeld = false;
                callbackDelivered = true;
                onFailure(name, "Unity could not decode the audio file.");
                yield break;
            }

            if (!TryCompleteImport(name, clip))
            {
                DestroyUnregisteredClip(clip);
                _pendingImports.Remove(name);
                reservationHeld = false;
                callbackDelivered = true;
                onFailure(name, "The AudioClip name is invalid or already registered.");
                yield break;
            }

            reservationHeld = false;
            callbackDelivered = true;
            onSuccess(name, clip);
        }
        finally
        {
            if (reservationHeld && !callbackDelivered)
            {
                if (request != null)
                {
                    try
                    {
                        request.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }

                if (clip != null && !_clips.ContainsKey(name))
                {
                    DestroyUnregisteredClip(clip);
                }

                _pendingImports.Remove(name);
                reservationHeld = false;
                callbackDelivered = true;
                onFailure(name, "The audio import was cancelled or interrupted before completion.");
            }
        }
    }

    private bool TryReserveImportName(string name, out string failure)
    {
        if (!ValidateName(name, false))
        {
            failure = "The AudioClip name is empty.";
            return false;
        }

        if (_clips.ContainsKey(name) || !_pendingImports.Add(name))
        {
            failure = "An AudioClip with that name is already registered or being imported.";
            return false;
        }

        failure = null!;
        return true;
    }

    private bool TryCompleteImport(string name, AudioClip clip)
    {
        if (!_pendingImports.Contains(name) || _clips.ContainsKey(name) || !ValidateClip(clip, false))
        {
            return false;
        }

        _clips.Add(name, clip);
        _pendingImports.Remove(name);
        return true;
    }

    private static IEnumerator FailImport(string name, string failure, Action<string, string> onFailure)
    {
        onFailure(name, failure);
        yield break;
    }

    private static void DestroyUnregisteredClip(AudioClip? clip)
    {
        if (clip != null)
        {
            UnityEngine.Object.Destroy(clip);
        }
    }

    private bool TryPrepareImport(FileHandler fileHandler, string name, string relativePath,
        out string fullPath, out AudioType audioType, out string failure)
    {
        fullPath = null!;
        audioType = AudioType.UNKNOWN;
        failure = null!;

        if (!ValidateName(name, false))
        {
            failure = "The AudioClip name is empty.";
            return false;
        }

        if (_clips.ContainsKey(name))
        {
            failure = "An AudioClip with that name is already registered.";
            return false;
        }

        if (fileHandler == null)
        {
            failure = "The consuming mod's file handler is null.";
            return false;
        }

        if (string.IsNullOrEmpty(relativePath) || Path.IsPathRooted(relativePath))
        {
            failure = "The audio path must be relative to the mod's Data directory.";
            return false;
        }

        string[] segments = relativePath.Split(new[] { '/', '\\' }, StringSplitOptions.None);
        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] == "..")
            {
                failure = "The audio path cannot contain a parent directory segment.";
                return false;
            }
        }

        string dataPath = fileHandler.GetDataPath();
        if (string.IsNullOrEmpty(dataPath))
        {
            failure = "The consuming mod's Data directory is unavailable.";
            return false;
        }

        string rootPath = Path.GetFullPath(dataPath);
        string localPath = relativePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        fullPath = Path.GetFullPath(Path.Combine(rootPath, localPath));

        string rootPrefix = rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (rootPrefix.Length == 0)
        {
            rootPrefix = Path.GetPathRoot(rootPath)!;
        }

        if (!rootPrefix.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        {
            rootPrefix += Path.DirectorySeparatorChar;
        }

        StringComparison pathComparison = Path.DirectorySeparatorChar == '\\'
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!fullPath.StartsWith(rootPrefix, pathComparison))
        {
            failure = "The audio path resolves outside the consuming mod's Data directory.";
            return false;
        }

        if (HasReparsePointInPath(rootPath, rootPrefix, fullPath))
        {
            failure = "The audio path cannot pass through a symbolic link or junction.";
            return false;
        }

        string extension = Path.GetExtension(fullPath);
        if (string.Equals(extension, ".mp3", StringComparison.OrdinalIgnoreCase))
        {
            audioType = AudioType.MPEG;
        }
        else if (string.Equals(extension, ".wav", StringComparison.OrdinalIgnoreCase))
        {
            audioType = AudioType.WAV;
        }
        else
        {
            failure = "Only MP3 and PCM WAV files are supported.";
            return false;
        }

        if (!File.Exists(fullPath))
        {
            failure = "The audio file does not exist in the consuming mod's Data directory.";
            return false;
        }

        if (audioType == AudioType.WAV && !IsPcmWaveFile(fullPath))
        {
            failure = "The WAV file is malformed or does not contain PCM audio.";
            return false;
        }

        return true;
    }

    private static bool HasReparsePointInPath(string rootPath, string rootPrefix, string fullPath)
    {
        string relativePath = fullPath.Substring(rootPrefix.Length);
        string currentPath = rootPath;
        string[] segments = relativePath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < segments.Length; i++)
        {
            currentPath = Path.Combine(currentPath, segments[i]);
            try
            {
                if ((File.GetAttributes(currentPath) & FileAttributes.ReparsePoint) != 0)
                {
                    return true;
                }
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch (DirectoryNotFoundException)
            {
                return false;
            }
            catch (IOException)
            {
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPcmWaveFile(string path)
    {
        using (FileStream stream = File.OpenRead(path))
        using (BinaryReader reader = new BinaryReader(stream))
        {
            if (stream.Length < 12 || Encoding.ASCII.GetString(reader.ReadBytes(4)) != "RIFF")
            {
                return false;
            }

            uint riffSize = reader.ReadUInt32();
            if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "WAVE")
            {
                return false;
            }

            long riffEnd = 8L + riffSize;
            if (riffEnd < 12 || riffEnd > stream.Length)
            {
                return false;
            }

            while (stream.Position + 8 <= riffEnd)
            {
                string chunkName = Encoding.ASCII.GetString(reader.ReadBytes(4));
                uint chunkSize = reader.ReadUInt32();
                long chunkEnd = stream.Position + chunkSize;
                if (chunkEnd > riffEnd || chunkEnd > stream.Length)
                {
                    return false;
                }

                if (chunkName == "fmt ")
                {
                    return chunkSize >= 16 && reader.ReadUInt16() == 1;
                }

                stream.Position = chunkEnd + (chunkSize % 2 == 0 ? 0 : 1);
                if (stream.Position > riffEnd || stream.Position > stream.Length)
                {
                    return false;
                }
            }

            return false;
        }
    }

    private static bool ValidateName(string name, bool throwError)
    {
        return ValidationUtils.Validate(name, value => !string.IsNullOrEmpty(value), logToModLog: false, throwError: throwError);
    }

    private bool ValidateClip(AudioClip clip, bool throwError)
    {
        return ValidationUtils.Validate(clip, _validateClip, logToModLog: false, throwError: throwError);
    }

    private static bool IsValidClip(AudioClip value)
    {
        return value is not null && value != null;
    }
}
