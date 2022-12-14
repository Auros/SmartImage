using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SmartImage.Sources
{
    public abstract class MonoSourceStreamBuilder : MonoBehaviour, ISourceStreamBuilder
    {
        public abstract bool IsSourceValid(string source);

        public abstract UniTask<Stream?> GetStreamAsync(string source, CancellationToken token = default);
    }
}