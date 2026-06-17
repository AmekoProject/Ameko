// SPDX-License-Identifier: MPL-2.0

using System.Buffers.Text;
using System.Globalization;
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Holo.IO;
using Holo.Providers;
using Microsoft.Extensions.Logging;

namespace Holo.Media.Providers;

/// <summary>
/// Interop with the Mizuki source provider
/// </summary>
public unsafe class MizukiSourceProvider(
    IFileSystem fileSystem,
    ILogger<MizukiSourceProvider> logger
) : ISourceProvider
{
    private static readonly External.LogCallback LogDelegate = Log;

    private static readonly ILogger MizukiLogger = StaticLoggerFactory.GetLogger("Mizuki");
    private GlobalContext* _context = null;
    private GCHandle? _progressHandle;

    /// <summary>
    /// If this provider is initialized
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <inheritdoc />
    public int FrameCount { get; private set; }

    /// <inheritdoc />
    public Rational Sar { get; }

    /// <inheritdoc />
    public bool ValidateDependencies()
    {
        string[] ffms2Candidates =
            OperatingSystem.IsWindows() ? ["ffms2.dll", "libffms2.dll"]
            : OperatingSystem.IsMacOS() ? ["libffms2.dylib", "ffms2.dylib"]
            : OperatingSystem.IsLinux() ? ["libffms2.so", "ffms2.so"]
            : [];

        string[] libassCandidates =
            OperatingSystem.IsWindows() ? ["libass.dll", "ass.dll", "ass-9.dll"]
            : OperatingSystem.IsMacOS() ? ["libass.dylib", "ass.dylib"]
            : OperatingSystem.IsLinux() ? ["libass.so", "ass.so"]
            : [];

        var ffms2Exists = false;
        var libassExists = false;

        foreach (var candidate in ffms2Candidates)
        {
            if (!NativeLibrary.TryLoad(candidate, out var handle))
                continue;
            NativeLibrary.Free(handle);
            ffms2Exists = true;
            break;
        }
        foreach (var candidate in libassCandidates)
        {
            if (!NativeLibrary.TryLoad(candidate, out var handle))
                continue;
            NativeLibrary.Free(handle);
            libassExists = true;
            break;
        }

        if (!ffms2Exists)
            logger.LogWarning("Dependency ffms2 not found!");
        if (!libassExists)
            logger.LogWarning("Dependency libass not found!");

        return ffms2Exists && libassExists;
    }

    /// <inheritdoc />
    public int Initialize()
    {
        if (!ValidateDependencies())
        {
            logger.LogWarning("One or more dependencies was not found. Cancelling initialization!");
            return -1;
        }

        External.SetLoggerCallback(LogDelegate);
        var result = External.Initialize();

        if (result == 0) // OK
        {
            IsInitialized = true;
        }
        return result;
    }

    /// <inheritdoc />
    public int LoadVideo(
        string filename,
        ISourceProvider.IndexingProgressCallback? progressCallback
    )
    {
        if (_context != null)
            CloseVideo();

        _context = External.CreateContext();

        External.IndexingProgressCallback? nativeCb = null;
        if (progressCallback != null)
        {
            nativeCb = (current, total, _) =>
            {
                progressCallback(current, total);
                return 0;
            };
            _progressHandle = GCHandle.Alloc(nativeCb);
        }

        var status = External.LoadVideo(
            _context,
            filename,
            GetCachePath(filename),
            "bgra",
            nativeCb
        );
        if (status == 0)
        {
            FrameCount = External.GetFrameCount(_context);
        }

        _progressHandle?.Free();
        return status;
    }

    /// <inheritdoc />
    public int LoadAudio(string filename, int? trackNumber)
    {
        var status = External.LoadAudio(
            _context,
            filename,
            GetCachePath(filename),
            (int)trackNumber!
        );
        return status;
    }

    /// <inheritdoc />
    public int LoadKeyframes(string filename)
    {
        return External.LoadKeyframes(_context, filename);
    }

    /// <inheritdoc />
    public TrackInfo[] GetAudioTrackInfo(string filename)
    {
        var ptr = External.GetAudioTrackInfo(_context, filename);
        return ptr.ToTrackInfoArray();
    }

    /// <inheritdoc />
    public int CloseVideo()
    {
        var result = External.CloseVideo(_context);
        External.DestroyContext(_context);
        _context = null;
        return result;
    }

    /// <inheritdoc />
    public int CloseAudio()
    {
        return External.CloseAudio(_context);
    }

    /// <inheritdoc />
    public int SetSubtitles(string data, string? codePage)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        return External.SetSubtitles(_context, bytes, bytes.Length, codePage);
    }

    /// <inheritdoc />
    public int AllocateBuffers(int numBuffers, int maxCacheSize)
    {
        return External.AllocateBuffers(_context, numBuffers, maxCacheSize);
    }

    /// <inheritdoc />
    public int AllocateAudioBuffer()
    {
        return External.AllocateAudioBuffer(_context);
    }

    /// <inheritdoc />
    public int FreeBuffers()
    {
        return External.FreeBuffers(_context);
    }

    /// <inheritdoc />
    public FrameGroup* GetFrame(int frameNumber, long timestamp, bool raw)
    {
        return External.GetFrame(_context, frameNumber, timestamp, raw ? 1 : 0);
    }

    /// <inheritdoc />
    public AudioFrame* GetAudio(ISourceProvider.IndexingProgressCallback? progressCallback = null)
    {
        External.IndexingProgressCallback? nativeCb = null;
        if (progressCallback != null)
        {
            nativeCb = (current, total, _) =>
            {
                progressCallback(current, total);
                return 0;
            };
            _progressHandle = GCHandle.Alloc(nativeCb);
        }

        var result = External.GetAudio(_context, nativeCb);
        _progressHandle?.Free();
        return result;
    }

    /// <inheritdoc />
    public Bitmap* GetVisualization(
        int width,
        int height,
        double pixelsPerMs,
        double amplitudeScale,
        long startTime,
        long videoTime,
        long audioTime,
        long* eventBounds,
        int eventBoundsLength
    )
    {
        return External.GetVisualization(
            _context,
            width,
            height,
            pixelsPerMs,
            amplitudeScale,
            startTime,
            videoTime,
            audioTime,
            eventBounds,
            eventBoundsLength
        );
    }

    /// <inheritdoc />
    public int[] GetKeyframes()
    {
        var ptr = External.GetKeyframes(_context);
        return ptr.ToIntArray();
    }

    /// <inheritdoc />
    public long[] GetTimecodes()
    {
        var ptr = External.GetTimecodes(_context);
        return ptr.ToLongArray();
    }

    /// <inheritdoc />
    public long[] GetMidcodes()
    {
        var ptr = External.GetMidcodes(_context);
        return ptr.ToLongArray();
    }

    /// <inheritdoc />
    public long[] GetFrameIntervals()
    {
        var ptr = External.GetFrameIntervals(_context);
        return ptr.ToLongArray();
    }

    /// <inheritdoc />
    public int GetChannelCount()
    {
        return External.GetChannelCount(_context);
    }

    /// <inheritdoc />
    public int GetSampleRate()
    {
        return External.GetSampleRate(_context);
    }

    /// <inheritdoc />
    public long GetSampleCount()
    {
        return External.GetSampleCount(_context);
    }

    /// <inheritdoc />
    public void CleanCache(uint days)
    {
        // Delete all indexes that haven't been accessed in 8 days
        logger.LogInformation("Cleaning index cache...");
        long sizeBytes = 0;
        foreach (
            var file in fileSystem
                .Directory.GetFiles(Directories.CacheHome)
                .Where(f => Path.GetExtension(f) == ".ffindex")
        )
        {
            try
            {
                if (
                    fileSystem.File.GetLastAccessTime(file)
                    >= DateTime.Now - TimeSpan.FromDays(days)
                )
                    continue;

                sizeBytes += fileSystem.FileInfo.New(file).Length;
                fileSystem.File.Delete(file);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while cleaning index");
            }
        }
        logger.LogInformation("Cleaned {SizeKb}kb of cached indexes", sizeBytes / 1024);
    }

    /// <summary>
    /// Callback for handling logs emitted by Mizuki
    /// </summary>
    /// <param name="level">Log level</param>
    /// <param name="ptr">Pointer to the c-string</param>
    private static void Log(int level, nint ptr)
    {
        var msg = Marshal.PtrToStringAnsi(ptr);
        if (msg is null)
            return;
        switch (level)
        {
            case 0:
                MizukiLogger.LogTrace("{MizukiMessage}", msg);
                break;
            case 1:
                MizukiLogger.LogDebug("{MizukiMessage}", msg);
                break;
            case 2:
                MizukiLogger.LogInformation("{MizukiMessage}", msg);
                break;
            case 3:
                MizukiLogger.LogWarning("{MizukiMessage}", msg);
                break;
            case 4:
                MizukiLogger.LogError("{MizukiMessage}", msg);
                break;
        }
    }

    /// <summary>
    /// Construct the cache path
    /// </summary>
    /// <param name="filePath">File being loaded</param>
    /// <returns>Filepath</returns>
    private static string GetCachePath(string filePath)
    {
        // Try to get the last-modified time
        var modified = File.Exists(filePath)
            ? new FileInfo(filePath).LastWriteTimeUtc.ToString(CultureInfo.InvariantCulture)
            : string.Empty;

        var bytes = MD5.HashData(Encoding.UTF8.GetBytes($"{filePath}-{modified}"));
        var encoded = Base64Url.EncodeToString(bytes);

        return Path.Combine(Directories.CacheHome, $"{encoded}.ffindex");
    }
}

/// <summary>
/// External methods
/// </summary>
internal static unsafe partial class External
{
    [LibraryImport("mizuki")]
    internal static partial int Initialize();

    [LibraryImport("mizuki")]
    internal static unsafe partial GlobalContext* CreateContext();

    [LibraryImport("mizuki")]
    internal static partial void DestroyContext(GlobalContext* context);

    [LibraryImport("mizuki", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int LoadVideo(
        GlobalContext* context,
        string fileName,
        string cacheFileName,
        string colorMatrix,
        IndexingProgressCallback? progressCallback
    );

    [LibraryImport("mizuki", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int LoadKeyframes(GlobalContext* context, string fileName);

    [LibraryImport("mizuki", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int LoadAudio(
        GlobalContext* context,
        string fileName,
        string cacheFileName,
        int trackNumber
    );

    [LibraryImport("mizuki", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial UnmanagedArray GetAudioTrackInfo(
        GlobalContext* context,
        string fileName
    );

    [LibraryImport("mizuki")]
    internal static partial int CloseVideo(GlobalContext* context);

    [LibraryImport("mizuki")]
    internal static partial int CloseAudio(GlobalContext* context);

    [LibraryImport("mizuki", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int SetSubtitles(
        GlobalContext* context,
        byte[] data,
        int dataLen,
        string? codePage
    );

    [LibraryImport("mizuki")]
    internal static partial int AllocateBuffers(
        GlobalContext* context,
        int numBuffers,
        int maxCacheSize
    );

    [LibraryImport("mizuki")]
    internal static partial int AllocateAudioBuffer(GlobalContext* context);

    [LibraryImport("mizuki")]
    internal static partial int FreeBuffers(GlobalContext* context);

    [LibraryImport("mizuki")]
    internal static unsafe partial FrameGroup* GetFrame(
        GlobalContext* context,
        int frameNumber,
        long timestamp,
        int raw
    );

    [LibraryImport("mizuki")]
    internal static unsafe partial AudioFrame* GetAudio(
        GlobalContext* context,
        IndexingProgressCallback? progressCallback
    );

    [LibraryImport("mizuki")]
    internal static unsafe partial Bitmap* GetVisualization(
        GlobalContext* context,
        int width,
        int height,
        double pixelsPerMs,
        double amplitudeScale,
        long startTime,
        long frameTime,
        long audioTime,
        long* eventBounds,
        int eventBoundsLength
    );

    [LibraryImport("mizuki")]
    internal static partial int GetFrameCount(GlobalContext* context);

    [LibraryImport("mizuki")]
    internal static partial UnmanagedArray GetKeyframes(GlobalContext* context);

    [LibraryImport("mizuki")]
    internal static partial UnmanagedArray GetTimecodes(GlobalContext* context);

    [LibraryImport("mizuki")]
    internal static partial UnmanagedArray GetMidcodes(GlobalContext* context);

    [LibraryImport("mizuki")]
    internal static partial int GetChannelCount(GlobalContext* context);

    [LibraryImport("mizuki")]
    internal static partial int GetSampleRate(GlobalContext* context);

    [LibraryImport("mizuki")]
    internal static partial long GetSampleCount(GlobalContext* context);

    [LibraryImport("mizuki")]
    internal static partial UnmanagedArray GetFrameIntervals(GlobalContext* context);

    [LibraryImport("mizuki")]
    internal static partial void SetLoggerCallback(LogCallback callback);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void LogCallback(int level, nint message);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int IndexingProgressCallback(long current, long total, void* unused);
}

/// <summary>
/// Used for contextualization
/// </summary>
internal struct GlobalContext;

/// <summary>
/// An unmanaged integer array
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct UnmanagedArray
{
    public nint Pointer;
    public nuint Length;
}

internal static class UnmanagedArrayExtensions
{
    /// <param name="array">Unmanaged array</param>
    extension(UnmanagedArray array)
    {
        /// <summary>
        /// Copy an unmanaged <see cref="UnmanagedArray"/> to a managed <c>int[]</c>
        /// </summary>
        /// <returns>Managed array</returns>
        public int[] ToIntArray()
        {
            var managed = new int[array.Length];
            Marshal.Copy(array.Pointer, managed, 0, managed.Length);
            return managed;
        }

        /// <summary>
        /// Copy an unmanaged <see cref="UnmanagedArray"/> to a managed <c>long[]</c>
        /// </summary>
        /// <returns>Managed array</returns>
        public long[] ToLongArray()
        {
            var managed = new long[array.Length];
            Marshal.Copy(array.Pointer, managed, 0, managed.Length);
            return managed;
        }

        /// <summary>
        /// Copy an unmanaged <see cref="UnmanagedArray"/> to a managed <c>TrackInfo[]</c>
        /// </summary>
        /// <returns>Managed array</returns>
        public TrackInfo[] ToTrackInfoArray()
        {
            var managed = new TrackInfo[array.Length];
            var size = Marshal.SizeOf<TrackInfo>();

            for (var i = 0; i < (int)array.Length; i++)
            {
                var ptr = array.Pointer + (i * size);
                managed[i] = Marshal.PtrToStructure<TrackInfo>(ptr);
            }
            return managed;
        }
    }
}
