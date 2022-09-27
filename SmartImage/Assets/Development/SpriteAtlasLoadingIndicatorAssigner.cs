using UnityEngine;
using UnityEngine.U2D;

namespace SmartImage.Develop
{
    public class SpriteAtlasLoadingIndicatorAssigner : MonoBehaviour
    {
        [SerializeField]
        private SpriteAtlas _spriteAtlas = null!;

        [SerializeField]
        private SmartImageManager _smartImageManager = null!; 

        [SerializeField, Min(0.01f)]
        private float _delay;
        
        private void Awake()
        {
            var sprites = new Sprite[_spriteAtlas.spriteCount];
            _spriteAtlas.GetSprites(sprites);

            _smartImageManager.LoadingIndicator = SmartSprite.Create(sprites, _delay);
        }
    }
}