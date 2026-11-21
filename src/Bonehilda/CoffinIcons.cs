using UnityEngine;

namespace Bonehilda;

internal static class CoffinIcons
{
    internal static Sprite Create(int stars)
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "BH isometric coffin " + stars,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.LoadRawTextureData(CoffinIconRaster.Render(stars, size));
        texture.Apply(false, false);
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f);
    }
}
