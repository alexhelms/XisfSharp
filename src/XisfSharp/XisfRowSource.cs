namespace XisfSharp;

/// <summary>
/// Supplies one row of image samples while an image is written.
/// </summary>
/// <remarks>
/// For <see cref="PixelStorage.Planar"/> images a row holds <c>width</c> samples of a single channel, and rows are
/// requested channel by channel. For <see cref="PixelStorage.Normal"/> images a row holds <c>width * channels</c>
/// interleaved samples and <paramref name="channel"/> is always 0. The span arrives cleared and is only valid
/// during the call. A row may be requested more than once, so the same row must always produce the same samples.
/// </remarks>
/// <typeparam name="T">The sample type, which determines the image's <see cref="SampleFormat"/>.</typeparam>
/// <param name="channel">The zero-based channel of the row.</param>
/// <param name="y">The zero-based row index.</param>
/// <param name="samples">Receives the samples of the row in native byte order.</param>
public delegate void XisfRowSource<T>(int channel, int y, Span<T> samples)
    where T : unmanaged;
