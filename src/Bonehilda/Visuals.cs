using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bonehilda;

internal static class Visuals
{

    private static readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
    private static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();

    internal static Material Template(GameObject source)
    {
        var materials = source.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Where(m => m && m.shader).ToArray();
        // The null graphics device cannot reflect bundled shader properties, but
        // still retains the referenced materials for CPU geometry validation.
        return materials.FirstOrDefault(m => m.HasProperty("_Color")) ?? materials.First();
    }

    internal static Material Material(Material template, string name, Color color)
    {
        // Valheim loads its shaders from asset bundles. Shader.Find cannot reliably see them
        // during prefab registration, and Standard is absent from current game builds.
        // Clone a referenced vanilla material so Unity retains the bundled shader.
        var material = new Material(template) { name = name };
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", Texture2D.whiteTexture);
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
        owned.Add(material);
        return material;
    }

    internal static void BuildCoffin(GameObject prefab, int stars) => CoffinVisuals.Build(prefab, stars);

    internal static void Dress(Transform skeleton) => DressVisuals.Build(skeleton);

    internal static GameObject MeshObject(Transform parent, string name, List<Vector3> vertices, List<int> triangles, Material material, bool doubleSided = false)
    {
        if (doubleSided)
        {
            var offset = vertices.Count;
            vertices.AddRange(vertices.ToArray());
            var reverse = triangles.ToArray();
            for (var i = 0; i < reverse.Length; i += 3) triangles.AddRange(new[] { offset+reverse[i+2],offset+reverse[i+1],offset+reverse[i] });
        }
        if (!meshes.TryGetValue(name, out var mesh) || !mesh)
        {
            mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetColors(Enumerable.Repeat(Color.white, vertices.Count).ToList());
            mesh.SetUVs(0, vertices.Select(vertex => new Vector2(vertex.x, vertex.z)).ToList());
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            meshes[name] = mesh;
            owned.Add(mesh);
        }
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        obj.AddComponent<MeshRenderer>().sharedMaterial = material;
        return obj;
    }

    internal static GameObject Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material, Vector3 rotation = default)
    {
        var obj = Primitive(parent, name, PrimitiveType.Cube, position, scale, material);
        obj.transform.localRotation = Quaternion.Euler(rotation);
        return obj;
    }

    internal static GameObject Primitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        var obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = position;
        obj.transform.localScale = scale;
        UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
        obj.GetComponent<Renderer>().sharedMaterial = material;
        return obj;
    }
}
