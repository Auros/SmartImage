using System.Collections;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace SmartImage.Tests
{
    public class CacheTest
    {
        [UnityTest]
        public IEnumerator ImageFromCache() => UniTask.ToCoroutine(async () =>
        {
            var options = new ImageLoadingOptions().WithoutSprites();
            
            var sim = TestHelpers.GetSIM();
            
            var sw = Stopwatch.StartNew();
            _ = await sim.LoadAsync(TestHelpers.AurosGitHubProfilePictureUrl, options);
            sw.Stop();
            
            // We are requesting through the web... it realistically should take at least this long
            Assert.That(sw.ElapsedMilliseconds > 25);
            
            sw.Restart();
            _ = await sim.LoadAsync(TestHelpers.AurosGitHubProfilePictureUrl, options);
            sw.Stop();
            
            // This should be a quick dictionary lookup
            Assert.That(sw.ElapsedMilliseconds < 1);
        });
    }
}