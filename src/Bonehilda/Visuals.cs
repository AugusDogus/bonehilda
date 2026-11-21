using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bonehilda;

internal static class Visuals
{
    private static readonly Dictionary<int, Material> palettes = new Dictionary<int, Material>();

    internal static Material Template(GameObject source)
    {
        var materials = source.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Where(m => m && m.shader).ToArray();
        // The null graphics device cannot reflect bundled shader properties, but
        // still retains the referenced materials for CPU geometry validation.
        return materials.FirstOrDefault(m => m.HasProperty("_Color")) ?? materials.First();
    }

    internal static Material Palette(Material template, int stars)
    {
        if (palettes.TryGetValue(stars, out var material)) return material;
        // Valheim loads its shaders from asset bundles. Shader.Find cannot reliably see them
        // during prefab registration, and Standard is absent from current game builds.
        // Clone a referenced vanilla material so Unity retains the bundled shader.
        material = new Material(template) { name = "BH tier " + stars };
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        if (material.HasProperty("_MainTex"))
        {
            material.mainTexture = ModelArt.Texture("palette" + stars, true);
            material.mainTextureScale = Vector2.one;
            material.mainTextureOffset = Vector2.zero;
        }
        if (material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", null);
        if (material.HasProperty("_EmissionMap")) material.SetTexture("_EmissionMap", null);
        if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", Color.black);
        if (material.HasProperty("_MetallicGlossMap")) material.SetTexture("_MetallicGlossMap", null);
        if (material.HasProperty("_SpecColor")) material.SetColor("_SpecColor", Color.black);
        material.DisableKeyword("_NORMALMAP");
        material.DisableKeyword("_EMISSION");
        for (var i = 0; i < material.shader.GetPropertyCount(); i++)
        {
            var property = material.shader.GetPropertyName(i);
            var lower = property.ToLowerInvariant();
            if (!lower.Contains("gloss") && !lower.Contains("smooth") && !lower.Contains("metallic")) continue;
            var type = material.shader.GetPropertyType(i);
            if (type == UnityEngine.Rendering.ShaderPropertyType.Float || type == UnityEngine.Rendering.ShaderPropertyType.Range)
                material.SetFloat(property, 0f);
            else if (type == UnityEngine.Rendering.ShaderPropertyType.Texture) material.SetTexture(property, null);
            else if (type == UnityEngine.Rendering.ShaderPropertyType.Vector) material.SetVector(property, Vector4.zero);
        }
        palettes.Add(stars, material);
        return material;
    }

    internal static void BuildCoffin(GameObject prefab, int stars) => CoffinVisuals.Build(prefab, stars);

    internal static void Dress(Transform skeleton) => DressVisuals.Build(skeleton);

    internal static Transform Child(Transform parent, string name)
    {
        var child = new GameObject(name).transform;
        child.SetParent(parent, false);
        return child;
    }
}
