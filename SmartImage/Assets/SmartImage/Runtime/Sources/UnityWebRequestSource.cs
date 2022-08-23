using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace SmartImage.Sources
{
    public class UnityWebRequestSource : MonoSourceStreamBuilder
    {
        public override bool IsSourceValid(string source)
        {
            var valid = source.StartsWith("https://") || File.Exists(source) || source.StartsWith("http://");
            return valid;
        }

        public override async UniTask<Stream?> GetStreamAsync(string source, CancellationToken token = default)
        {
            await UniTask.SwitchToMainThread();
            using var req = await UnityWebRequest.Get(source).SendWebRequest().WithCancellation(token);
            var bytes = req.downloadHandler.data;
            await UniTask.SwitchToThreadPool();
            return new MemoryStream(bytes);
        }
    }
}