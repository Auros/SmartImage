using System.IO;
using System.Net.Http;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace SmartImage.Sources
{
    public class HttpClientRequestSource : MonoSourceStreamBuilder
    {
        private readonly HttpClient _httpClient = new();
        
        public override bool IsSourceValid(string source)
        {
            var valid = source.StartsWith("https://") || source.StartsWith("http://");
            return valid;
        }

        public override async UniTask<Stream?> GetStreamAsync(string source, CancellationToken token = default)
        {
            var request = await _httpClient.GetAsync(source, token);
            if (!request.IsSuccessStatusCode)
                return null;
            return await request.Content.ReadAsStreamAsync();
        }
    }
}