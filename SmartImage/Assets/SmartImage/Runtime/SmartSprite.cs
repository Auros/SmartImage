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

        private readonly List<Action<SmartFrame>> _listeners = new();

        public bool HasAnyListeners => _listeners.Count != 0;

        public void AddListener(Action<SmartFrame> frame)
        {
            _listeners.Add(frame);
        }

        public void RemoveListener(Action<SmartFrame> frame)
        {
            _listeners.Remove(frame);
        }

        internal void SetNewFrame(int index)
        {
            Active = Frames[index];
            foreach (var listener in _listeners)
                listener.Invoke(Active);
        }
    }
}