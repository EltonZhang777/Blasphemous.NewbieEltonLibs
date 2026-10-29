using Blasphemous.ModdingAPI;
using Blasphemous.ModdingAPI.Files;
using Blasphemous.NewbieEltonLibs.Extensions.ModdingAPI;
using FMOD;
using FMODUnity;
using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Blasphemous.NewbieEltonLibs.Audio;

/// <summary>Imports raw FMOD sounds from a consuming mod's data directory.</summary>
public static class FMODSoundImporter
{
    /// <summary>Asynchronously imports an audio file through the game's low-level FMOD system.</summary>
    /// <param name="lowlevelSystem">The initialized FMOD Core System used to create the sound.</param>
    /// <param name="fileHandler">The consuming mod's file handler.</param>
    /// <param name="fileName">A relative audio file path inside the mod's data directory.</param>
    /// <param name="callback">The callback receiving the sound or failure information.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="callback" /> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a worker cannot be queued for the request.</exception>
    /// <remarks>The callback runs on a worker thread. A successful sound transfers to the consuming mod when the callback returns normally; a sound is released if the callback throws.</remarks>
    public static void ImportAsync(FMOD.System lowlevelSystem, FileHandler fileHandler, string fileName, Action<FMODSoundImportResult> callback)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        string? dataPath = null;
        Exception? dataPathError = null;
        try
        {
            if (fileHandler == null)
            {
                throw new ArgumentNullException(nameof(fileHandler));
            }

            dataPath = fileHandler.GetDataPath();
        }
        catch (Exception exception)
        {
            dataPathError = exception;
        }

        ImportRequest request = new(lowlevelSystem, dataPath, dataPathError, fileName, callback);
        if (!ThreadPool.QueueUserWorkItem(CompleteImport, request))
        {
            throw new InvalidOperationException("Unable to queue the FMOD sound import request.");
        }
    }

    private static void CompleteImport(object? state)
    {
        ImportRequest request = (ImportRequest)state!;
        FMODSoundImportResult result;
        try
        {
            if (request.DataPathError != null)
            {
                result = FMODSoundImportResult.Failure(request.DataPathError.Message);
            }
            else
            {
                result = Import(request.LowlevelSystem, request.DataPath!, request.FileName);
            }
        }
        catch (Exception exception)
        {
            result = FMODSoundImportResult.Failure(exception.Message);
        }

        try
        {
            request.Callback(result);
        }
        catch (Exception exception)
        {
            if (result.Succeeded && result.Sound.hasHandle())
            {
                try
                {
                    RESULT releaseResult = result.Sound.release();
                    if (releaseResult != RESULT.OK)
                    {
                        ModLog.Error($"FMOD sound import callback failed and its sound could not be released: {releaseResult}.");
                    }
                }
                catch (Exception releaseException)
                {
                    ModLog.Error($"FMOD sound import callback failed and releasing its sound threw: {releaseException}");
                }
            }

            ModLog.Error($"FMOD sound import callback failed: {exception}");
        }
    }

    private static FMODSoundImportResult Import(FMOD.System lowlevelSystem, string dataPath, string fileName)
    {
        if (string.IsNullOrEmpty(dataPath) || dataPath.Trim().Length == 0)
        {
            return FMODSoundImportResult.Failure("The mod data directory is unavailable.");
        }
        if (string.IsNullOrEmpty(fileName) || fileName.Trim().Length == 0)
        {
            return FMODSoundImportResult.Failure("The audio file path is empty.");
        }
        if (Path.IsPathRooted(fileName))
        {
            return FMODSoundImportResult.Failure("The audio file path must be relative to the mod data directory.");
        }

        string rootPath = Path.GetFullPath(dataPath);
        string rootPrefix = rootPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            || rootPath.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            ? rootPath
            : rootPath + Path.DirectorySeparatorChar;
        string filePath = Path.GetFullPath(Path.Combine(rootPath, fileName));
        if (!filePath.StartsWith(rootPrefix, StringComparison.Ordinal))
        {
            return FMODSoundImportResult.Failure("The audio file path resolves outside the mod data directory.");
        }
        if (!File.Exists(filePath))
        {
            return FMODSoundImportResult.Failure($"Audio file '{fileName}' was not found in the mod data directory.");
        }

        using (SafeFileHandle rootHandle = CreateFile(rootPath, FileReadAttributes, ShareRead | ShareWrite | ShareDelete,
            IntPtr.Zero, OpenExisting, FileFlagBackupSemantics, IntPtr.Zero))
        {
            if (rootHandle.IsInvalid)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            string finalRootPath = GetFinalPathNameByHandle(rootHandle);
            using (FileStream stream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                string finalFilePath = GetFinalPathNameByHandle(stream.SafeFileHandle);
                if (!IsWithinDirectory(finalRootPath, finalFilePath))
                {
                    return FMODSoundImportResult.Failure("The audio file resolves outside the mod data directory.");
                }

                if (stream.Length > int.MaxValue)
                {
                    return FMODSoundImportResult.Failure("The audio file is too large to import from memory.");
                }

                byte[] data = new byte[(int)stream.Length];
                int bytesRead = 0;
                while (bytesRead < data.Length)
                {
                    int count = stream.Read(data, bytesRead, data.Length - bytesRead);
                    if (count == 0)
                    {
                        return FMODSoundImportResult.Failure("The audio file changed while it was being read.");
                    }

                    bytesRead += count;
                }

                CREATESOUNDEXINFO exInfo = new();
                exInfo.cbsize = Marshal.SizeOf(typeof(CREATESOUNDEXINFO));
                exInfo.length = (uint)data.Length;
                RESULT result = lowlevelSystem.createSound(data, MODE.OPENMEMORY, ref exInfo, out Sound sound);
                return result == RESULT.OK
                    ? FMODSoundImportResult.Success(sound)
                    : FMODSoundImportResult.Failure($"FMOD failed to import '{fileName}': {result}.");
            }
        }
    }

    private static bool IsWithinDirectory(string directoryPath, string filePath)
    {
        string prefix = directoryPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            ? directoryPath
            : directoryPath + Path.DirectorySeparatorChar;
        // Windows directories can opt into case-sensitive lookup, so final handle paths must preserve case.
        return filePath.StartsWith(prefix, StringComparison.Ordinal);
    }

    private static string GetFinalPathNameByHandle(SafeFileHandle handle)
    {
        const int BufferSize = 32768;
        StringBuilder path = new(BufferSize);
        uint length = GetFinalPathNameByHandleNative(handle, path, BufferSize, 0);
        if (length == 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        if (length >= BufferSize)
        {
            throw new IOException("The resolved audio path is too long.");
        }

        return path.ToString();
    }

    private const uint FileReadAttributes = 0x80;
    private const uint ShareRead = 0x1;
    private const uint ShareWrite = 0x2;
    private const uint ShareDelete = 0x4;
    private const uint OpenExisting = 3;
    private const uint FileFlagBackupSemantics = 0x02000000;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CreateFileW")]
    private static extern SafeFileHandle CreateFile(string fileName, uint desiredAccess, uint shareMode,
        IntPtr securityAttributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "GetFinalPathNameByHandleW")]
    private static extern uint GetFinalPathNameByHandleNative(SafeFileHandle handle, StringBuilder path, int bufferLength, uint flags);

    private sealed class ImportRequest
    {
        public ImportRequest(FMOD.System lowlevelSystem, string? dataPath, Exception? dataPathError, string fileName, Action<FMODSoundImportResult> callback)
        {
            LowlevelSystem = lowlevelSystem;
            DataPath = dataPath;
            DataPathError = dataPathError;
            FileName = fileName;
            Callback = callback;
        }

        public FMOD.System LowlevelSystem { get; }
        public string? DataPath { get; }
        public Exception? DataPathError { get; }
        public string FileName { get; }
        public Action<FMODSoundImportResult> Callback { get; }
    }
}

/// <summary>Contains the result of an asynchronous FMOD sound import.</summary>
public sealed class FMODSoundImportResult
{
    private FMODSoundImportResult(bool succeeded, Sound sound, string? errorMessage)
    {
        Succeeded = succeeded;
        Sound = sound;
        ErrorMessage = errorMessage;
    }

    /// <summary>Gets whether the sound was imported successfully.</summary>
    public bool Succeeded { get; }

    /// <summary>Gets the imported sound, owned by the consuming mod after its callback returns normally when <see cref="Succeeded" /> is true.</summary>
    public Sound Sound { get; }

    /// <summary>Gets the failure information, or null when the import succeeded.</summary>
    public string? ErrorMessage { get; }

    internal static FMODSoundImportResult Success(Sound sound)
    {
        return new FMODSoundImportResult(true, sound, null);
    }

    internal static FMODSoundImportResult Failure(string errorMessage)
    {
        return new FMODSoundImportResult(false, default, errorMessage);
    }
}
