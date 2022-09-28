using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        
        public SmartSprite? LoadingIndicator { get; set; }
        
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
            
            try
            {
                
                // Now we try to get the image source.
                ISourceStreamBuilder? sourceStreamBuilder = null;
                foreach (var builder in _sources)
                {
                    if (!builder.IsSourceValid(source))
                        continue;

                    sourceStreamBuilder = builder;
                    break;
                }

                // If the source to the image isn't valid, we immediately return.
                if (sourceStreamBuilder == null)
                    return null;
                
                // As this method is designed with multithreading in mind, we make sure only one thread can read the dictionary
                // at once. I could use a ConcurrentDictionary, but I don't want a SmartSprite to be "lost" if one of the same
                // key was added first.
                await _semaphore.WaitAsync(token);

                // If the sprite is already in the cache, and it's valid, return it.
                if (_sprites.TryGetValue(id, out var smartSprite) && smartSprite.State == MediaState.Valid)
                    return smartSprite;

                // Later we want to check if this caller was the one that created the sprite.
                bool createdNewSprite = false;

                var hasLoadingIndicator = LoadingIndicator?.State == MediaState.Valid;
                
                // If there was no sprite at all in the cache, create a new one, set it as loading, and add it to the cache.
                if (smartSprite == null)
                {
                    createdNewSprite = true;
                    smartSprite = new SmartSprite { State = MediaState.Loading };
                    
                    // If we have a loading indicator, assign it here.
                    if (hasLoadingIndicator)
                    {
                        smartSprite.SetLoadingFrames(LoadingIndicator!.Frames);
                        smartSprite.SetActiveFrame(0); // We can safely assume that we have at least one sprite.
                        
                        // If the loading indicator is animated, add it.
                        await UniTask.SwitchToMainThread();
                        if (_animationController != null && smartSprite.Frames.Length > 1)
                            _animationController.Add(smartSprite);
                        await UniTask.SwitchToThreadPool();
                    }
                    
                    _sprites.Add(id, smartSprite);
                }

                // Exit this locker
                _semaphore.Release();
                
                var spriteBuildTask = BuildSprite(smartSprite, id, sourceStreamBuilder, source, createdNewSprite, options, token);

                // If a loading indicator has been not set, we can just await the task.
                if (!hasLoadingIndicator)
                    return await spriteBuildTask;
                
                // However, if we do have a loading indicator, we fire off the loading in the background
                // and return the recently created sprite immediately.
                _ = UniTask.RunOnThreadPool(() => spriteBuildTask, cancellationToken: token);
                return smartSprite;

            }
            catch (Exception e)
            {
                _sprites.Remove(id);
                _currentlyBuilding.RemoveAll(c => c.Id == id);
                
                if (e is TaskCanceledException)
                    return null;
                
                Debug.LogError(e);
            }
            return null;
        }

        private async UniTask<SmartSprite?> BuildSprite(SmartSprite smartSprite, int id, ISourceStreamBuilder builder, string source, bool isNew, ImageLoadingOptions options, CancellationToken token)
        {
            // Checks to see if we created the sprite, and if not, just wait
                // until the that handler finishes building (or failed) the sprite.
                if (!isNew)
                {
                    // Wait every frame until the sprite isn't loading
                    await UniTask.WaitUntil(() => smartSprite.State is not MediaState.Loading, PlayerLoopTiming.Update,
                        token);

                    // If the sprite's state is now valid (sprite is ready), we return it. If not, we return null.
                    return smartSprite.State is MediaState.Valid ? smartSprite : null;
                }

                
                await using var imageStream = await builder.GetStreamAsync(source, token);
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

                var construction = new SmartConstruction(id, smartSprite, pool, options, token);

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
                                _taskQueue.Enqueue(() => DestroyFrame(alreadyBuiltFrame, true));
                        }

                        var frame = construction.Frames[index];

                        Texture2D tex = new(frame.Width, frame.Height);

                        tex.SetPixels32(frame.Pixels);
                        tex.wrapMode = options.WrapMode;
                        tex.Apply();

                        var sprite = options.DoNotBuildSprite
                            ? null
                            : Sprite.Create(
                                tex, 
                                new Rect(0, 0, tex.width, tex.height),
                                Vector2.zero, 
                                10, 
                                0,
                                options.MeshType
                            );
                        
                        var smartFrame = smartSprite.SetFrame(new SmartFrame
                        {
                            Texture = tex,
                            Sprite = sprite!,
                            Delay = frame.Delay
                        }, index);
                        
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
                
                finalSprite?.SetActiveFrame(0); // We can safely assume there's at least one sprite.
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
            // Void all enqueued tasks (although this really isn't that necessary)
            _taskQueue.Clear();
            
            // We destroy all the sprites and textures we generated and have stored.
            foreach (var sprite in _sprites.Values)
            {
                // If the sprite was already destroyed for... some reason, or it hasn't finished loading, skip.
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (sprite?.Frames == null)
                    continue;
                
                if (_animationController != null && sprite.Frames.Length > 1)
                    _animationController.Remove(sprite);

                // Mark the sprite as invalid.
                sprite.State = MediaState.Invalid;
                
                // Destroy all the contained frames.
                foreach (var frame in sprite.Frames)
                    DestroyFrame(frame, false);
            }

            foreach (var currentlyBuilding in _currentlyBuilding)
                foreach (var frame in currentlyBuilding.Sprite.Frames)
                    DestroyFrame(frame, false);
        }


        private static void DestroyFrame(SmartFrame? smartFrame, bool immediately)
        {
            // Nothing to destroy!
            if (smartFrame == null)
                return;
        
            if (immediately)
            {
                if (smartFrame.Sprite != null)
                    DestroyImmediate(smartFrame.Sprite);
            
                if (smartFrame.Texture != null)
                    DestroyImmediate(smartFrame.Texture);
            }
            else
            {
                if (smartFrame.Sprite != null)
                    Destroy(smartFrame.Sprite);
            
                if (smartFrame.Texture != null)
                    Destroy(smartFrame.Texture);
            }
        }
    }
}