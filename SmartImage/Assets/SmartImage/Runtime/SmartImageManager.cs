using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SmartImage.Sources;
using UnityEngine;
using UnityEngine.Pool;
using Color = UnityEngine.Color;
using Debug = UnityEngine.Debug;

namespace SmartImage
{
    public class SmartImageManager : MonoBehaviour
    {
        [SerializeField, Tooltip("The time in milliseconds that this instance can take to build textures and sprites this frame.")]
        private float _incrementalLimit = 5f;
        
        [SerializeField]
        private MonoSourceStreamBuilder[] _sources = Array.Empty<MonoSourceStreamBuilder>();

        private readonly SemaphoreSlim _semaphore = new(1);
        private static readonly ImageLoadingOptions _defaultOptions = new();

        private readonly Stopwatch _stopwatch = new();
        private readonly Dictionary<int, SmartTexture> _textures = new();
        private readonly List<SmartTextureConstruction> _texturesToBuild = new();

        [PublicAPI]
        public UniTask<SmartTexture?> LoadAsync(string source) => LoadAsync(source, null, default);
        
        [PublicAPI]
        public UniTask<SmartTexture?> LoadAsync(string source, CancellationToken token) => LoadAsync(source, null, token);
        
        [PublicAPI]
        public UniTask<SmartTexture?> LoadAsync(string source, ImageLoadingOptions options) => LoadAsync(source, options, default);
        
        [PublicAPI]
        public async UniTask<SmartTexture?> LoadAsync(string source, ImageLoadingOptions? options, CancellationToken token)
        {
            options ??= _defaultOptions;

            // Build a unique hash to handle caching.
            var id = source.GetHashCode() ^ options.GetHashCode();

            bool inCache = false;
            await _semaphore.WaitAsync(token);
            if (_textures.TryGetValue(id, out var smartTexture))
            {
                inCache = true;
                if (!smartTexture.IsLoading && smartTexture.IsValid)
                    return smartTexture;
            }
            else
            {
                smartTexture = new SmartTexture { IsLoading = true };
                _textures.Add(id, smartTexture);
            }
            _semaphore.Release();

            // If the texture from the cache is already being loaded.
            if (inCache && smartTexture.IsLoading)
            {
                await UniTask.WaitUntil(() => smartTexture.IsLoading, PlayerLoopTiming.Update, token);
                return smartTexture.IsValid ? smartTexture : null;
            }
            
            ISourceStreamBuilder? sourceStreamBuilder = null;
            foreach (var builder in _sources)
            {
                if (!builder.IsSourceValid(source))
                    continue;

                sourceStreamBuilder = builder;
                break;
            }

            // If the builder wasn't acquired earlier, we can't build the image.
            if (sourceStreamBuilder == null)
                return null;

            await using var imageStream = await sourceStreamBuilder.GetStreamAsync(source, token);
            if (imageStream == null) // If the stream is null, that means the source builder failed to get the stream.
                return null;

            Image image;
            try
            {
                image = await Image.LoadAsync<RgbaVector>(imageStream, token);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return null;
            }

            var pool = ListPool<ImageFrameConstructionInfo>.Get();
            var frames = (image.Frames as ImageFrameCollection<RgbaVector>)!;
            for (int i = 0; i < image.Frames.Count; i++)
            {
                var frame = frames[i];
                ImageFrameConstructionInfo frameConstruct = new();
                frameConstruct.Init(frame);
                pool.Add(frameConstruct);
            }

            await _semaphore.WaitAsync(token);
            _texturesToBuild.Add(new SmartTextureConstruction(smartTexture, pool, options, token));
            _semaphore.Release();

            await UniTask.WaitUntil(() => !smartTexture.IsLoading, cancellationToken: token);
            image.Dispose();
            
            return !smartTexture.IsValid ? null : smartTexture;
        }
        
        private void Update()
        {
            _stopwatch.Start();
            var toRemove = ListPool<SmartTextureConstruction>.Get();
            foreach (var item in _texturesToBuild)
            {
                if (item.Token.IsCancellationRequested)
                {
                    // TODO: Schedule already made textures for deletion.
                    toRemove.Add(item);
                    continue;
                }

                bool triedToBuildAnything = false;
                for (int i = item.Cursor; i < item.Frames.Count; i++)
                {
                    item.Cursor++;
                    var frame = item.Frames[i];

                    if (!frame.TryBuild)
                        continue;

                    triedToBuildAnything = true;
                    frame.TryBuild = false;

                    Texture2D tex = new(frame.Width, frame.Height);

                    for (int c = 0; c < frame.Size; c++)
                        item.Copier[c] = frame.Pixels[c];
                    
                    tex.SetPixels32(item.Copier);
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.Apply();
                    
                    item.Texture.Frames[i] = new SmartFrame<Texture2D>
                    {
                        Delay = frame.Delay,
                        IsValid = true,
                        Value = tex
                    };

                    if (i == 0)
                        item.Texture.Active = item.Texture.Frames[i].Value;

                    frame.Dispose();
                    if (_stopwatch.Elapsed.TotalMilliseconds > _incrementalLimit)
                        break;
                }

                // If we couldn't build anything this frame, this image is... probably complete...
                if (!triedToBuildAnything)
                {
                    toRemove.Add(item);
                    continue;
                }
                
                if (_stopwatch.Elapsed.TotalMilliseconds > _incrementalLimit)
                    break;
            }
            _stopwatch.Stop();
            _stopwatch.Reset();

            foreach (var construct in toRemove)
            {
                construct.Dispose();
                _texturesToBuild.Remove(construct);
                construct.Texture.IsLoading = false;
                construct.Texture.IsValid = true;
            }
            
            ListPool<SmartTextureConstruction>.Release(toRemove);
        }

        private class SmartTextureConstruction
        {
            public SmartTexture Texture { get; }
            public CancellationToken Token { get; }
            public ImageLoadingOptions Options { get; }
            public List<ImageFrameConstructionInfo> Frames { get; }
            
            public Color32[] Copier { get; }
            public int Cursor { get; set; }
            
            public SmartTextureConstruction(SmartTexture texture, List<ImageFrameConstructionInfo> frames, ImageLoadingOptions options, CancellationToken token)
            {
                Token = token;
                Frames = frames;
                Options = options;
                Texture = texture;

                Copier = new Color32[frames[0].Width * frames[0].Height];
                texture.Frames = new SmartFrame<Texture2D>[frames.Count];
            }

            public void Dispose()
            {
                ListPool<ImageFrameConstructionInfo>.Release(Frames);
            }
        }

        private class ImageFrameConstructionInfo
        {
            public Color32[] Pixels { get; private set; } = null!;
            
            public int Size { get; private set; }
            public int Width { get; private set; }
            public int Height { get; private set; }
            
            /// <summary>
            /// The delay of this frame, in milliseconds.
            /// </summary>
            public int Delay { get; private set; }
            
            public bool TryBuild { get; set; }

            public void Init(ImageFrame<RgbaVector> frame)
            {
                Width = frame.Width;
                Height = frame.Height;
                Size = Width * Height;
                // Rent a color array that we'll use to store the pixel data for the image frame(s).
                Pixels = ArrayPool<Color32>.Shared.Rent(Size);
                for (int x = 0; x < frame.Width; x++)
                    for (int y = 0; y < frame.Height; y++)
                        Pixels[frame.Width * (frame.Height - y - 1) + x] = RgbaToColor(frame[x, y]);

                var gif = frame.Metadata.GetGifMetadata();
                Delay = gif.FrameDelay;

                TryBuild = true;
            }

            private static Color RgbaToColor(RgbaVector vector) => new(vector.R, vector.G, vector.B, vector.A);
            
            public void Dispose()
            {
                ArrayPool<Color32>.Shared.Return(Pixels);
                Pixels = null!;
            }
        }
    }
}