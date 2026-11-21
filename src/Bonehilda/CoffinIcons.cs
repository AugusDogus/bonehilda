using UnityEngine;

namespace Bonehilda;

internal static class CoffinIcons
{
    internal static Sprite Create(int stars)
    {
        var texture = ModelArt.Texture("coffin" + stars, false);
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f);
    }
}
