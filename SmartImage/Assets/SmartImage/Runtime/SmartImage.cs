﻿using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace SmartImage
{
    [RequireComponent(typeof(Image))]
    public class SmartImage : MonoBehaviour
    {
        private Image _image = null!;
        
        [SerializeField]
        private string _source = null!;

        [SerializeField]
        private SmartImageManager _smartImageManager = null!;

        [SerializeField]
        private bool _preserveAspect = true;
        
        private SmartSprite? _sprite;
        
        private void Awake()
        {
            _image = GetComponent<Image>();
            _image.preserveAspect = _preserveAspect;
        }

        // ReSharper disable once UnusedMember.Local (UniTask will call this method!)
        private async UniTaskVoid Start()
        {
            await UniTask.SwitchToThreadPool();
            var sprite = await _smartImageManager.LoadAsync(_source);
            if (sprite == null)
                return;
            await UniTask.SwitchToMainThread();
            _image.sprite = sprite.Active.Sprite;
            _sprite = sprite;
            
            _sprite.AddListener(FrameChanged);
        }

        private void OnEnable()
        {
            _sprite?.AddListener(FrameChanged);
        }

        private void FrameChanged(SmartSprite sprite, SmartFrame frame)
        {
            _image.sprite = frame.Sprite;
        }

        private void OnDisable()
        {
            _sprite?.RemoveListener(FrameChanged);
        }
    }
}