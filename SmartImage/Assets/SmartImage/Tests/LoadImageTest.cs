using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace SmartImage.Tests
{
    public class LoadImageTest
    {
        [UnityTest]
        public IEnumerator LoadImageTestWithEnumeratorPasses() => UniTask.ToCoroutine(async () =>
        {
            var sim = TestHelpers.GetSIM();
            
            var sprite = await sim.LoadAsync(TestHelpers.AurosGitHubProfilePictureUrl);
            Assert.NotNull(sprite);
            Assert.NotNull(sprite.Active);
        });
    }
}
