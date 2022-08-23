using JetBrains.Annotations;
using UnityEngine;

namespace SmartImage
{
    public record ImageLoadingOptions
    {
        /// <summary>
        /// The texture(s)'s wrap mode.
        /// </summary>
        public TextureWrapMode WrapMode { get; set; } = TextureWrapMode.Clamp;
        
        // We default to FullRect here. Although it technically is less performant when rendering the sprite every,
        // frame, the time it takes to build the sprite are significantly faster.
        public SpriteMeshType MeshType { get; set; } = SpriteMeshType.FullRect; 
        
        /// <summary>
        /// WARNING! This is an advanced option. This is for those who don't want to build the sprite, and only want
        /// the texture. Animations will still work too. Some use cases may not require the need for a sprite.
        /// </summary>
        public bool DoNotBuildSprite { get; set; }
        
        /// <summary>
        /// The wrap mode that the generated texture is.
        /// </summary>
        /// <param name="wrapMode">The texture wrap mode. This sets the property of texture.wrapMode</param>
        [PublicAPI]
        public ImageLoadingOptions WithWrapMode(TextureWrapMode wrapMode)
        {
            WrapMode = wrapMode;
            return this;
        }

        /// <summary>
        /// The sprite's mesh type.
        /// </summary>
        [PublicAPI]
        public ImageLoadingOptions WithMeshType(SpriteMeshType meshType)
        {
            MeshType = meshType;
            return this;
        }

        /// <summary>
        /// WARNING! This is an advanced option. This is for those who don't want to build the sprite, and only want
        /// the texture. Animations will still work too. Some use cases may not require the need for a sprite.
        /// </summary>
        [PublicAPI]
        public ImageLoadingOptions WithSprites()
        {
            DoNotBuildSprite = false;
            return this;
        }

        /// <summary>
        /// WARNING! This is an advanced option. This is for those who don't want to build the sprite, and only want
        /// the texture. Animations will still work too. Some use cases may not require the need for a sprite.
        /// </summary>
        [PublicAPI]
        public ImageLoadingOptions WithoutSprites()
        {
            DoNotBuildSprite = true;
            return this;
        }
    }
}