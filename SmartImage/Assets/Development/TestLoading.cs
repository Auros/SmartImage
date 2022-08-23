using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace SmartImage.Develop
{
    public class TestLoading : MonoBehaviour
    {
        [SerializeField]
        private SmartImageManager _smartImageManager = null!;
        
        [SerializeField]
        private string _source = string.Empty;

        [SerializeField]
        private Image[] _images = Array.Empty<Image>();
        
        private void Start()
        {
            //await UniTask.SwitchToThreadPool();
            //var smartTexture = await _smartImageManager.LoadAsync(_source);
            //await UniTask.SwitchToMainThread();
            //print(smartTexture);

            var allImages = new DirectoryInfo(@"C:\Users\Auros\Desktop").GetFiles("*.png", SearchOption.AllDirectories);
            
            foreach (var img in _images)
            {
                var source = allImages[Random.Range(0, allImages.Length)].FullName;
                print(source);
                UniTask.RunOnThreadPool(async () =>
                {
                    var smartTexture = await _smartImageManager.LoadAsync(source);
                    await UniTask.SwitchToMainThread();
                    img.sprite = smartTexture!.Active.Sprite;
                    smartTexture.AddListener(frame => img.sprite = frame.Sprite);
                });

            }
        }
    }
}