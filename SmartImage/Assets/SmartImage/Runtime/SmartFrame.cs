namespace SmartImage
{
    public class SmartFrame<T>
    {
        public T Value { get; internal set; }
        
        /// <summary>
        /// Delay for this frame, in milliseconds.
        /// </summary>
        internal float Delay { get; set; }
        
        internal bool IsValid { get; set; }
    }
}