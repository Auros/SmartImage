using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SmartImage.Develop
{
    public class TestLoading : MonoBehaviour
    {
        [SerializeField]
        private SmartImageManager _smartImageManager = null!;
        
        [SerializeField]
        private string _source = string.Empty;

        [SerializeField]
        private Image _image;
        
        private async UniTaskVoid Start()
        {
            await UniTask.SwitchToThreadPool();
            var smartTexture = await _smartImageManager.LoadAsync(_source);
            await UniTask.SwitchToMainThread();
            print(smartTexture);
            
            _image.sprite = Sprite.Create(smartTexture.Active, new Rect(0, 0, smartTexture.Active.width, smartTexture.Active.height), Vector2.zero, 100);
        }
    }
}