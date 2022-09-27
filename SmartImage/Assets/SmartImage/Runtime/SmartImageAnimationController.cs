using System;
using System.Collections.Generic;
using System.Linq;
using SmartImage.Internal;
using UnityEngine;

namespace SmartImage
{
    public class SmartImageAnimationController : MonoBehaviour
    {
        private readonly List<SmartImageAnimationContext> _contexts = new();

        public void Add(SmartSprite smartSprite)
        {
            if (_contexts.Any(ctx => ctx.Sprite == smartSprite))
                return;

            var ctx = new SmartImageAnimationContext(smartSprite);
            ctx.CurrentFrame = Array.IndexOf(ctx.Sprite.Frames, ctx.Sprite.Active);
            ctx.TimeSinceLastUpdated = Time.time;
            _contexts.Add(ctx);
        }

        public void Remove(SmartSprite smartSprite)
        {
            _contexts.RemoveAll(c => c.Sprite == smartSprite);
        }

        private void Update()
        {
            var currentTime = Time.time;
            foreach (var ctx in _contexts)
            {
                // If nothing is listening into this sprite, we don't need to spend time to update it.
                if (!ctx.Sprite.HasAnyListeners)
                    continue;

                // Also, if the number of sprites is equal to 1, it also means we don't need to update it.
                if (ctx.Sprite.Frames.Length == 1)
                    continue;

                // If we haven't reached the next period of time to update, we skip for this frame.
                if (ctx.Sprite.Active.Delay + ctx.TimeSinceLastUpdated > currentTime)
                    continue;

                int newFrameIndex = ctx.CurrentFrame >= ctx.Sprite.Frames.Length - 1 ? 0 : ctx.CurrentFrame + 1;

                ctx.CurrentFrame = newFrameIndex;
                ctx.Sprite.SetActiveFrame(newFrameIndex);
                ctx.TimeSinceLastUpdated = currentTime;
            }
        }
    }
}