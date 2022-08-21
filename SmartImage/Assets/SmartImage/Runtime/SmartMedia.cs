using System;

namespace SmartImage
{
    public class SmartMedia<T>
    {
        public event Action<T> ActiveFrameUpdated;
        
        public T Active { get; internal set; }
        
        internal bool IsValid { get; set; }
        internal bool IsLoading { get; set; }
        internal SmartFrame<T>[] Frames { get; set; }
    }
}