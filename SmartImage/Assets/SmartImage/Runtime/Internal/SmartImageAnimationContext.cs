namespace SmartImage.Internal
{
    internal class SmartImageAnimationContext
    {
        public SmartSprite Sprite { get; }
        public int CurrentFrame { get; set; }
        public float TimeSinceLastUpdated { get; set; }

        public SmartImageAnimationContext(SmartSprite smartSprite)
        {
            Sprite = smartSprite;
        }
    }
}