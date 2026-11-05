namespace XisfSharp;

/// <summary>
/// Receives one row of image samples while an image is streamed.
/// </summary>
/// <remarks>
/// For <see cref="PixelStorage.Planar"/> images a row holds <c>width</c> samples of a single channel, and rows
/// arrive channel by channel. For <see cref="PixelStorage.Normal"/> images a row holds <c>width * channels</c>
/// interleaved samples and <paramref name="channel"/> is always 0. The span is only valid during the call.
/// </remarks>
/// <typeparam name="T">The sample type, which must match the image's <see cref="SampleFormat"/>.</typeparam>
/// <param name="channel">The zero-based channel of the row.</param>
/// <param name="y">The zero-based row index.</param>
/// <param name="samples">The samples of the row in native byte order.</param>
public delegate void XisfRowAction<T>(int channel, int y, ReadOnlySpan<T> samples)
    where T : unmanaged;
