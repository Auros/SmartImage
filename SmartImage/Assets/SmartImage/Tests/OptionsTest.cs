using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SmartImage.Tests
{
    public class OptionsTest
    {
        [UnityTest]
        public IEnumerator DoNotBuildSprites() => UniTask.ToCoroutine(async () =>
        {
            var options = new ImageLoadingOptions().WithoutSprites();
            var sim = TestHelpers.GetSIM();
            
            var sprite = await sim.LoadAsync(TestHelpers.AurosGitHubProfilePictureUrl, options);

            Assert.IsNotNull(sprite);
            Assert.IsNull(sprite.Active.Sprite);
        });
        
        [UnityTest]
        public IEnumerator UseRepeatWrapMode() => UniTask.ToCoroutine(async () =>
        {
            var options = new ImageLoadingOptions().WithWrapMode(TextureWrapMode.Repeat);
            
            var sim = TestHelpers.GetSIM();
            var sprite = await sim.LoadAsync(TestHelpers.AurosGitHubProfilePictureUrl, options);

            Assert.IsNotNull(sprite);
            Assert.AreEqual(TextureWrapMode.Repeat, sprite.Active.Texture.wrapMode);
        });
    }
}