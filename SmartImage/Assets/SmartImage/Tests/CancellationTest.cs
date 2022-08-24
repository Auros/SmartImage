using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace SmartImage.Tests
{
    public class CancellationTest
    {
        [UnityTest]
        public IEnumerator Cancel() => UniTask.ToCoroutine(async () =>
        {
            var options = new ImageLoadingOptions().WithoutSprites();
            var sim = TestHelpers.GetSIM();

            using CancellationTokenSource cts = new(100);
            var sprite = await sim.LoadAsync(TestHelpers.AurosGitHubProfilePictureUrl, options, cts.Token);
            Assert.Null(sprite);
        });
    }
}