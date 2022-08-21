using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace SmartImage.Sources
{
    /// <summary>
    /// Defines the operation to build an image stream from a source.
    /// </summary>
    public interface ISourceStreamBuilder
    {
        /// <summary>
        /// Checks to see if the image source is valid based on the style of the stream.
        /// </summary>
        /// <param name="source">The source of the image stream.</param>
        /// <returns>Whether or not the image source is valid.</returns>
        bool IsSourceValid(string source);

        /// <summary>
        /// Gets the image stream from the source.
        /// </summary>
        /// <param name="source">The source of the image stream.</param>
        /// <param name="token">Cancellation token to end early.</param>
        /// <returns>The stream of the image source, or null if there was an issue with acquiring the stream.</returns>
        UniTask<Stream?> GetStreamAsync(string source, CancellationToken token = default);
    }
}