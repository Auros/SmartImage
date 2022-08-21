using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace SmartImage
{
    public class UnityWebRequestSource : MonoSourceStreamBuilder
    {
        public override bool IsSourceValid(string source)
        {
            var valid = source.StartsWith("https://") || File.Exists(source);

            // For now, we don't let http (unsecure) urls be used in production.
            // If this is an issue, feel free to make a PR and make your case as to why this behaviour should change.
            if (!valid && (Debug.isDebugBuild || Application.isEditor))
                valid = source.StartsWith("http://");

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