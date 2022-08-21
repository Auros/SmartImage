using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SmartImage.Develop
{
    public class TestLoading : MonoBehaviour
    {
        [SerializeField]
        private SmartImageManager _smartImageManager = null!;
        
        [SerializeField]
        private string _source = string.Empty;

        private async UniTaskVoid Start()
        {
            await UniTask.SwitchToThreadPool();
            var smartTexture = await _smartImageManager.LoadAsync(_source);
            await UniTask.SwitchToMainThread();
            print(smartTexture);
        }
    }
}