using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UnityEngine;
using Color = UnityEngine.Color;

namespace SmartImage.Internal
{
    internal class SmartFrameConstruction : IDisposable
    {
        /// <summary>
        /// The amount of pixels in this frame.
        /// </summary>
        public int Size { get; }
        
        /// <summary>
        /// The width (in pixels) of this frame.
        /// </summary>
        public int Width { get; }
        
        /// <summary>
        /// The height (in pixels) of this frame.
        /// </summary>
        public int Height { get; }
        
        /// <summary>
        /// The delay of this frame, in milliseconds.
        /// </summary>
        public float Delay { get; }

        /// <summary>
        /// The pixel data for this frame.
        /// </summary>
        public Color32[] Pixels { get; }

        public SmartFrameConstruction(ImageFrame<RgbaVector> frame)
        {
            Width = frame.Width;
            Height = frame.Height;
            Size = Width * Height;

            Pixels = new Color32[Size];
            
            // Copy the pixel data from ImageSharp frame to the Unity Color32 array.
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    Pixels[Width * (Height - y - 1) + x] = RgbaToColor(frame[x, y]);
            
            var gif = frame.Metadata.GetGifMetadata();
            Delay = gif.FrameDelay / 100f; // Convert to seconds
        }
        
        private static Color RgbaToColor(RgbaVector vector)
            => new(vector.R, vector.G, vector.B, vector.A);

        public void Dispose()
        {
            
        }
    }
}