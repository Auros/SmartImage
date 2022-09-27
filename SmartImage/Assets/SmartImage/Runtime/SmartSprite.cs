using System;
using System.Collections.Generic;
using SmartImage.Internal;
using UnityEngine;

namespace SmartImage
{
    public class SmartSprite
    {
        internal MediaState State { get; set; }
        public SmartFrame Active { get; internal set; } = null!;

        internal SmartFrame[] Frames
        {
            get => State == MediaState.Loading && _loadingFrames is not null ? _loadingFrames : _frames;
            set => _frames = value;
        }

        private SmartFrame[]? _loadingFrames;
        private SmartFrame[] _frames = Array.Empty<SmartFrame>();
        private readonly List<Action<SmartSprite, SmartFrame>> _listeners = new();

        public bool HasAnyListeners => _listeners.Count != 0;

        public void AddListener(Action<SmartSprite, SmartFrame> frame)
        {
            if (_listeners.Contains(frame))
                return;
            
            _listeners.Add(frame);
        }

        public void RemoveListener(Action<SmartSprite, SmartFrame> frame)
        {
            _listeners.Remove(frame);
        }

        internal void SetActiveFrame(int index)
        {
            Active = Frames[index];
            foreach (var listener in _listeners)
                listener.Invoke(this, Active);
        }

        internal SmartFrame SetFrame(SmartFrame smartFrame, int index)
        {
            return _frames[index] = smartFrame;
        }

        internal void SetLoadingFrames(SmartFrame[] loadingFrames)
        {
            _loadingFrames = loadingFrames;
        }

        /// <summary>
        /// Creates a SmartSprite from an array of sprites. There MUST be at least one Sprite
        /// </summary>
        /// <param name="sprites">The sprites to turn into a SmartSprite</param>
        /// <param name="delay">The delay, in seconds, between each smart frame.</param>
        /// <returns>The created SmartSprite</returns>
        public static SmartSprite Create(Sprite[] sprites, float delay = 0.02f)
        {
            if (sprites.Length == 0)
                throw new InvalidOperationException("There must be at least one sprite.");
            
            SmartSprite smartSprite = new()
            {
                Frames = new SmartFrame[sprites.Length],
                State = MediaState.Valid
            };

            for (int i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                smartSprite.SetFrame(new SmartFrame
                {
                    Sprite = sprite,
                    Texture = sprite.texture,
                    Delay = delay
                }, i);
            }
            smartSprite.Active = smartSprite.Frames[0];
            return smartSprite;
        }
    }
}