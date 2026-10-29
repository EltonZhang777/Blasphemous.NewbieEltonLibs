using Blasphemous.ModdingAPI.Files;
using Blasphemous.NewbieEltonLibs.Audio;
using Blasphemous.NewbieEltonLibs.Extensions.GameLibs;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

namespace Blasphemous.NewbieEltonLibs.Tests;

/// <summary>Verifies the asynchronous FMOD sound import contract without a running game.</summary>
public sealed class FMODSoundImporterTests
{
    /// <summary>Verifies paths outside the mod data directory fail once on a worker thread.</summary>
    [Fact]
    public void RejectsEscapingPathExactlyOnceOffTheCallingThread()
    {
        string dataPath = PathForTestData();
        FileHandler fileHandler = CreateFileHandler(dataPath);
        int callingThreadId = Thread.CurrentThread.ManagedThreadId;
        int callbackCount = 0;
        int callbackThreadId = 0;
        FMODSoundImportResult? result = null;

        using (ManualResetEvent completed = new(false))
        {
            FMODSoundImporter.ImportAsync(default(FMOD.System), fileHandler, System.IO.Path.Combine("..", "outside.mp3"), value =>
            {
                Interlocked.Increment(ref callbackCount);
                callbackThreadId = Thread.CurrentThread.ManagedThreadId;
                result = value;
                completed.Set();
            });

            Assert.True(completed.WaitOne(TimeSpan.FromSeconds(5)));
            Thread.Sleep(100);
        }

        Assert.Equal(1, callbackCount);
        Assert.NotEqual(callingThreadId, callbackThreadId);
        Assert.NotNull(result);
        Assert.False(result!.Succeeded);
        Assert.Contains("outside", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies a missing file inside mod data returns failure asynchronously.</summary>
    [Fact]
    public void MissingInDataFileReturnsFailureOnceOffTheCallingThread()
    {
        FileHandler fileHandler = CreateFileHandler(PathForTestData());
        int callingThreadId = Thread.CurrentThread.ManagedThreadId;
        int callbackCount = 0;
        int callbackThreadId = 0;
        FMODSoundImportResult? result = null;

        using (ManualResetEvent completed = new(false))
        {
            FMODSoundImporter.ImportAsync(default(FMOD.System), fileHandler, "audio\\missing.wav", value =>
            {
                Interlocked.Increment(ref callbackCount);
                callbackThreadId = Thread.CurrentThread.ManagedThreadId;
                result = value;
                completed.Set();
            });

            Assert.True(completed.WaitOne(TimeSpan.FromSeconds(5)));
            Thread.Sleep(100);
        }

        Assert.Equal(1, callbackCount);
        Assert.NotEqual(callingThreadId, callbackThreadId);
        Assert.NotNull(result);
        Assert.False(result!.Succeeded);
        Assert.Contains("not found", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    private static string PathForTestData()
    {
        return System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NewbieEltonLibs-" + Guid.NewGuid().ToString("N"), "data");
    }

    private static FileHandler CreateFileHandler(string dataPath)
    {
        FileHandler fileHandler = (FileHandler)RuntimeHelpers.GetUninitializedObject(typeof(FileHandler));
        TraverseUtils.SetValue(ref fileHandler, "dataPath", dataPath);
        return fileHandler;
    }
}
