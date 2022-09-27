[![openupm](https://img.shields.io/npm/v/dev.auros.smartimage?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/dev.auros.smartimage/)

# SmartImage
A smarter way with dealing with images loaded at runtime in Unity. 

## Intention
This package was built with online, web focused, XR applications in mind.

## The Problem
When Unity was first created in the late 2000s, things were different. There weren't as many web integrated games and applications.

Nowadays, there are applications who need to operate with strict frame time budgets and need to avoid stutters and hitches to prevent
user discomfort and nausea, while also needing to integrate online, social experiences that depend heavily on the internet. Some applications that
meet this description are social games like [VRChat](https://vrchat.com) and [ChilloutVR](https://store.steampowered.com/app/661130/ChilloutVR), or applications that focus on content creators like VTuber apps.

Creating images at runtime in Unity has gotten worse over the past few years, as the size and dimensions of user generated images have grown larger.
The commonality of it all makes it more difficult for developers to easily create applications that have to manage a lot of applications.

I present you a simple but useful solution.

## The Solution

Introducing **SmartImage** a library for Unity which simplifies the process of dynamically loading one or a large set of images.

## How It Works

First off, SmartImage simplifies the process for gathering the raw image data. It comes shipped with ways to pull images from the filesystem and the web, with an
easy to use component-based way to add your own image sources.

Secondly, SmartImage keeps track of the amount of time it takes to build the texture and frame (which need* to be created on the Main Thread, causing hitching
and stuttering for your users). Once a certain threshold has been met, it will defer the textures and sprites it needs to create to the next frame. This approach is similar
to how [Unity's Incremental Garbage Collector](https://docs.unity3d.com/Manual/performance-incremental-garbage-collection.html) works because it splits up the work across multiple frames.

## Features

* Load images from the web
* Load images from the filesystem
* Easy to use system for adding custom image sources
* Animated Image Support (GIF)
* Caching

## Planned Features
* Image Resizing

## Dependencies

* [UniTask](https://github.com/Cysharp/UniTask)
* [ImageSharp](https://github.com/SixLabors/ImageSharp)

## Examples

### Setting up the SmartImageManager
This component is essential to loading SmartImages, as it manages the loading, state, and cleanup of every sprite generated within a SmartImage.

1. Create a new GameObject
2. Add the `SmartImageManager` component
3. Add the image source providers. Currently SmartImage ships with the `HttpClientRequestSource` (recommended for web content) and the `UnityWebRequestSource`.
4. Order matters, it will try to match a source string with through these starting with the first.
5. (Optional) Add an animation controller to support gifs. This component handles updating the frames for each `SmartSprite`.

![Smart Image Manager Setup](https://user-images.githubusercontent.com/41306347/186303878-71d4478d-ba4e-47f3-9dea-619fb7ce8de3.gif)

### SmartImage Component

1. Add the component to any GameObject with `UnityEngine.UI.Image`
2. Add the reference to the `Smart Image Manager`
3. Add a source

![SmartImage Component](https://user-images.githubusercontent.com/41306347/186303023-b77cc527-f8f9-4bc9-ab62-9e96d578164e.gif)

### GIF Support

![GIF Support](https://user-images.githubusercontent.com/41306347/186304262-d500c47a-2acc-4d3a-b854-85908125547e.gif)

### Code Examples

#### Load an image programatically
```cs
[SerializeField]
private SmartImageManager _smartImageManager;

private async UniTaskVoid Start()
{
    // Overrides support options and cancellation!
    var sprite = await _smartImageManager.LoadAsync("https://avatars.githubusercontent.com/u/41306347");
    
    // Optional, update your media elements in case it's animated.
    sprite.AddListener((_, frame) => UpdateImage(frame));
}
```

#### Add a loading indicator
```cs
[SerializeField]
private SmartImageManager _smartImageManager;

[SerializeField]
private Sprite[] _loadingIndicatorFrames; 

private void Awake()
{
    // Must be at least one sprite.
    _smartImageManager.LoadingIndicator = SmartSprite.Create(_loadingIndicatorFrames, 0.5f);
}
```

## Installation
I highly recommend you install via [OpenUPM](https://openupm.com). It will automatically install the necessary dependencies.

Install OpenUPM CLI from their website if you haven't already.

Run this in the root of your Unity Project (Same folder that has the Assets folder).

`openupm add dev.auros.smartimage`
