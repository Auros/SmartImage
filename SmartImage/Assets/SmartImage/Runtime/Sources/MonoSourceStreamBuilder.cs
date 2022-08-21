using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using SmartImage.Sources;
using UnityEngine;

namespace SmartImage
{
    public abstract class MonoSourceStreamBuilder : MonoBehaviour, ISourceStreamBuilder
    {
        public abstract bool IsSourceValid(string source);

        public abstract UniTask<Stream?> GetStreamAsync(string source, CancellationToken token = default);
    }
}