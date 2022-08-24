using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SmartImage.Internal;
using SmartImage.Sources;
using UnityEngine;
using UnityEngine.Pool;
using Debug = UnityEngine.Debug;

namespace SmartImage
{
    public class SmartImageManager : MonoBehaviour
    {
        [SerializeField, Tooltip("The time in milliseconds that this instance can take to build textures and sprites this frame.")]
        private float _incrementalLimit = 5f;

        [SerializeField]
        private SmartImageAnimationController? _animationController;
        
        [SerializeField]
        private MonoSourceStreamBuilder[] _sources = Array.Empty<MonoSourceStreamBuilder>();
        
        private readonly SemaphoreSlim _semaphore = new(1);
        private static readonly ImageLoadingOptions _defaultOptions = new();

        private readonly Stopwatch _stopwatch = new();
        private readonly Dictionary<int, SmartSprite> _sprites = new();
        private readonly List<SmartConstruction> _currentlyBuilding = new();

        private readonly Queue<Action> _taskQueue = new();
        
        [PublicAPI]
        public UniTask<SmartSprite?> LoadAsync(string source) => LoadAsync(source, null, default);
        
        [PublicAPI]
        public UniTask<SmartSprite?> LoadAsync(string source, CancellationToken token) => LoadAsync(source, null, token);
        
        [PublicAPI]
        public UniTask<SmartSprite?> LoadAsync(string source, ImageLoadingOptions options) => LoadAsync(source, options, default);
        
        [PublicAPI]
        public async UniTask<SmartSprite?> LoadAsync(string source, ImageLoadingOptions? options, CancellationToken token)
        {
            options ??= _defaultOptions;

            // Build a unique hash to handle caching.
            var id = source.GetHashCode() ^ options.GetHashCode();

            // As this method is designed with multithreading in mind, we make sure only one thread can read the dictionary
            // at once. I could use a ConcurrentDictionary, but I don't want a SmartSprite to be "lost" if one of the same
            // key was added first.
            await _semaphore.WaitAsync(token);
            
            // If the sprite is already in the cache, and it's valid, return it.
            if (_sprites.TryGetValue(id, out var smartSprite) && smartSprite.State == MediaState.Valid)
                return smartSprite;
            
            // Later we want to check if this caller was the one that created the sprite.
            bool createdNewSprite = false;
    
            // If there was no sprite at all in the cache, create a new one, set it as loading, and add it to the cache.
            if (smartSprite == null)
            {
                createdNewSprite = true;
                smartSprite = new SmartSprite { State = MediaState.Loading };
                _sprites.Add(id, smartSprite);
            }
            
            // Exit this locker
            _semaphore.Release();

            // Checks to see if we created the sprite, and if not, just wait
            // until the that handler finishes building (or failed) the sprite.
            if (!createdNewSprite)
            {
                // Wait every frame until the sprite isn't loading
                await UniTask.WaitUntil(() => smartSprite.State is not MediaState.Loading, PlayerLoopTiming.Update, token);
                
                // If the sprite's state is now valid (sprite is ready), we return it. If not, we return null.
                return smartSprite.State is MediaState.Valid ? smartSprite : null;
            }
            
            // Now we try to get the image source.
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

            var pool = ListPool<SmartFrameConstruction>.Get();
            var frames = (image.Frames as ImageFrameCollection<RgbaVector>)!;
            for (int i = 0; i < image.Frames.Count; i++)
            {
                var frame = frames[i];
                SmartFrameConstruction frameConstruct = new(frame);
                pool.Add(frameConstruct);
            }
            
            // After this point, we've copied all the data from the ImageSharp frames, so we can dispose it.
            image.Dispose();

            await _semaphore.WaitAsync(token);

            var construction = new SmartConstruction(smartSprite, pool, options, token);

            for (int i = 0; i < construction.Frames.Count; i++)
            {
                var index = i;
                _taskQueue.Enqueue(() =>
                {
                    if (construction.Sprite.State == MediaState.Invalid)
                        return;

                    // If the sprite building wants to be cancelled...
                    if (construction.Token.IsCancellationRequested)
                    {
                        // First, set the state to invalid so more frames cannot be built.
                        construction.Sprite.State = MediaState.Invalid;

                        // Remove it from the cache.
                        _sprites.Remove(id);
                        
                        // Then add the deletion of each frame to the scheduler.
                        foreach (var alreadyBuiltFrame in construction.Sprite.Frames)
                        {
                            _taskQueue.Enqueue(() =>
                            {
                                DestroyImmediate(alreadyBuiltFrame.Sprite);
                                DestroyImmediate(alreadyBuiltFrame.Texture);
                            });
                        }
                    }
                    
                    var frame = construction.Frames[index];
                    
                    Texture2D tex = new(frame.Width, frame.Height);

                    tex.SetPixels32(frame.Pixels);
                    tex.wrapMode = options.WrapMode;
                    tex.Apply();

                    var sprite = options.DoNotBuildSprite ? null : Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero, 10, 0, options.MeshType);
                    var smartFrame = smartSprite.Frames[index] = new SmartFrame
                    {
                        Texture = tex,
                        Sprite = sprite!,
                        Delay = frame.Delay
                    };
                    frame.Dispose();
                    construction.FramesBuilt++;

                    if (index == 0)
                        construction.Sprite.Active = smartFrame;

                    if (construction.Frames.Count != construction.FramesBuilt)
                        return;
                    
                    construction.Sprite.State = MediaState.Valid;
                    _currentlyBuilding.Remove(construction);
                });
            }
            
            _currentlyBuilding.Add(construction);
            _semaphore.Release();

            await UniTask.WaitUntil(() => smartSprite.State is not MediaState.Loading, cancellationToken: token);
            
            var finalSprite = smartSprite.State is MediaState.Valid ? smartSprite : null;
            if (_animationController != null && finalSprite != null && finalSprite.Frames.Length > 1)
                _animationController.Add(finalSprite);
            return finalSprite;
        }
        
        private void Update()
        {
            _stopwatch.Start();

            while (_incrementalLimit > _stopwatch.Elapsed.TotalMilliseconds && _taskQueue.TryDequeue(out var task))
                task.Invoke();
                
            _stopwatch.Stop();
            _stopwatch.Reset();
        }

        private void OnDestroy()
        {
            if (_animationController == null)
                return;
            
            foreach (var sprite in _sprites.Values)
                if (sprite.Frames.Length > 1)
                    _animationController.Remove(sprite);
        }
    }
}