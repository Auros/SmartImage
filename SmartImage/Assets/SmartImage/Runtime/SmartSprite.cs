using System;
using System.Collections.Generic;
using SmartImage.Internal;

namespace SmartImage
{
    public class SmartSprite
    {
        internal MediaState State { get; set; }
        internal SmartFrame[] Frames { get; set; } = null!;
        public SmartFrame Active { get; internal set; } = null!;

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

        internal void SetNewFrame(int index)
        {
            Active = Frames[index];
            foreach (var listener in _listeners)
                listener.Invoke(this, Active);
        }
    }
}