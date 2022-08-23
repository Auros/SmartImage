using UnityEngine;

namespace SmartImage
{
    public class SmartFrame
    {
        public Sprite Sprite { get; internal set; } = null!;

        public Texture2D Texture { get; internal set; } = null!;
        
        /// <summary>
        /// Delay for this frame, in seconds.
        /// </summary>
        internal float Delay { get; set; }
    }
}