using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Pool;

namespace SmartImage.Internal
{
    internal class SmartConstruction : IDisposable
    {
        public SmartSprite Sprite { get; }
        public CancellationToken Token { get; }
        public ImageLoadingOptions Options { get; }
        public List<SmartFrameConstruction> Frames { get; }
        
        public int FramesBuilt { get; set; }
        
        public SmartConstruction(SmartSprite texture, List<SmartFrameConstruction> frames, ImageLoadingOptions options, CancellationToken token)
        {
            Token = token;
            Frames = frames;
            Options = options;
            Sprite = texture;
            texture.Frames = new SmartFrame[frames.Count];
        }

        public void Dispose()
        {
            ListPool<SmartFrameConstruction>.Release(Frames);
        }
    }
}