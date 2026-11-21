using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bonehilda;

internal static class CoffinVisuals
{
    private static readonly Vector2[] Outline =
    {
        new Vector2(-0.53f, 0.12f), new Vector2(0.53f, 0.12f),
        new Vector2(0.62f, 2.12f), new Vector2(0f, 2.55f), new Vector2(-0.62f, 2.12f)
    };
    private static readonly Color[] Colors = { new Color(0.43f, 0.06f, 0.07f), new Color(0.13f, 0.095f, 0.08f), new Color(0.08f, 0.12f, 0.29f) };

    internal static void Build(GameObject prefab, int stars)
    {
        var template = Visuals.Template(prefab);
        var wood = Visuals.Material(template, "BH cabinet wood " + stars, new Color(0.22f, 0.12f, 0.075f));
        // Keep the game's referenced wood texture and UVs on the cabinet, including the door interior.
        if (template.HasProperty("_MainTex")) wood.SetTexture("_MainTex", template.GetTexture("_MainTex"));
        var panel = Visuals.Material(template, "BH cabinet panel " + stars, Colors[stars]);
        var lining = Visuals.Material(template, "BH padded lining " + stars, stars == 0 ? new Color(0.57f, 0.46f, 0.34f) : new Color(0.35f, 0.055f, 0.055f));
        var bone = Visuals.Material(template, "BH cabinet bone " + stars, new Color(0.74f, 0.68f, 0.50f));
        var brass = Visuals.Material(template, "BH cabinet brass " + stars, new Color(0.63f, 0.39f, 0.09f));
        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        foreach (var collider in prefab.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        var root = new GameObject("BonehildaCoffin").transform;
        root.SetParent(prefab.transform, false);

        var back = Panel(root, "Solid wooden back " + stars, Outline, -0.34f, 0.10f, wood);
        Collider(back, new Vector3(0f, 1.25f, -0.34f), new Vector3(1.22f, 2.5f, 0.1f));
        Panel(root, "Padded cabinet interior " + stars, Inset(0.88f), -0.275f, 0.025f, lining);
        for (var i = 0; i < Outline.Length; i++)
        {
            var a = Outline[i];
            var b = Outline[(i + 1) % Outline.Length];
            var wall = Beam(root, i == 0 ? "BH_CoffinFloor" : "Wood cabinet wall", a, b, -0.005f, 0.10f, 0.72f, wood);
            var collider = wall.AddComponent<BoxCollider>();
            collider.size = Vector3.one;
            wall.layer = LayerMask.NameToLayer("piece");
            Beam(root, "Ivory frame", a, b, 0.40f, 0.055f, 0.10f, bone);
        }
        for (var side = -1; side <= 1; side += 2)
        {
            Visuals.Primitive(root, "Bone foot", PrimitiveType.Sphere, new Vector3(side * 0.52f, 0.08f, 0.20f), new Vector3(0.16f, 0.16f, 0.22f), bone);
            Visuals.Primitive(root, "Bone finial", PrimitiveType.Sphere, new Vector3(side * 0.62f, 2.12f, 0.40f), Vector3.one * 0.11f, bone);
        }
        Visuals.Primitive(root, "Crown bone", PrimitiveType.Sphere, new Vector3(0, 2.56f, 0.40f), new Vector3(0.14f, 0.16f, 0.1f), bone);

        var hinge = new GameObject("BH_DoorHinge").transform;
        hinge.SetParent(root, false);
        hinge.localPosition = new Vector3(-0.59f, 0f, 0.43f);
        var doorOutline = Inset(0.94f).Select(p => p + Vector2.right * 0.59f).ToArray();
        var door = Panel(hinge, "Solid coffin door " + stars, doorOutline, 0f, 0.08f, wood);
        Collider(door, new Vector3(0.59f, 1.26f, 0f), new Vector3(1.08f, 2.28f, 0.08f));
        var decoration = new GameObject("Door decoration").transform;
        decoration.SetParent(hinge, false);
        decoration.localPosition = Vector3.right * 0.59f;
        Panel(decoration, "Colored front panel " + stars, Inset(0.84f), 0.052f, 0.018f, panel);
        // Explicit closed solids with front AND back caps: no invisible door underside.
        Panel(decoration, "Finished inside door " + stars, Inset(0.85f), -0.052f, 0.018f, lining);
        for (var i = 0; i < Outline.Length; i++)
        {
            var inner = Inset(0.86f);
            Beam(decoration, "Door trim", inner[i], inner[(i + 1) % inner.Length], 0.074f, 0.028f, 0.026f, bone);
        }
        Visuals.Box(decoration, "Stained glass frame", new Vector3(0, 1.77f, 0.08f), new Vector3(0.22f, 0.37f, 0.035f), bone);
        Visuals.Box(decoration, "Ruby glass", new Vector3(-0.045f, 1.8f, 0.103f), new Vector3(0.07f, 0.25f, 0.012f), lining);
        Visuals.Box(decoration, "Amber glass", new Vector3(0.045f, 1.8f, 0.103f), new Vector3(0.07f, 0.25f, 0.012f), brass);
        Visuals.Primitive(decoration, "Brass door handle", PrimitiveType.Sphere, new Vector3(0.38f, 1.15f, 0.115f), new Vector3(0.065f, 0.065f, 0.07f), brass);
        for (var i = 0; i < stars; i++)
            Visuals.Primitive(decoration, "Tier emblem", PrimitiveType.Sphere, new Vector3((i - (stars - 1) * 0.5f) * 0.13f, 2.12f, 0.095f), new Vector3(0.075f, 0.075f, 0.025f), brass);

        var chest = prefab.GetComponent<Container>();
        // Coffin animates a real hinge; the vanilla chest toggles two static lid objects.
        chest.m_closed = null!;
        chest.m_open = null!;
        var wear = prefab.GetComponent<WearNTear>();
        wear.m_new = null!; wear.m_worn = null!; wear.m_broken = null!;
        wear.m_fragmentRoots = new[] { root.gameObject };
        EntryRamp(root);
        prefab.GetComponent<Piece>().m_icon = CoffinIcons.Create(stars);
    }

    private static Vector2[] Inset(float scale) => Outline.Select(p => new Vector2(p.x * scale, (p.y - 1.3f) * scale + 1.3f)).ToArray();

    private static void Collider(GameObject obj, Vector3 center, Vector3 size)
    {
        var collider = obj.AddComponent<BoxCollider>(); collider.center = center; collider.size = size;
        obj.layer = LayerMask.NameToLayer("piece");
    }

    private static GameObject Beam(Transform parent, string name, Vector2 a, Vector2 b, float z, float width, float depth, Material material)
    {
        var mid = (a + b) * 0.5f;
        return Visuals.Box(parent, name, new Vector3(mid.x, mid.y, z), new Vector3(width, Vector2.Distance(a, b), depth), material,
            new Vector3(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg - 90f));
    }

    private static GameObject Panel(Transform parent, string name, Vector2[] outline, float z, float thickness, Material material)
    {
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        var center = outline.Aggregate(Vector2.zero, (sum, p) => sum + p) / outline.Length;
        void Triangle(Vector3 a, Vector3 b, Vector3 c)
        {
            var n = vertices.Count; vertices.AddRange(new[] { a, b, c }); triangles.AddRange(new[] { n, n + 1, n + 2 });
        }
        for (var i = 0; i < outline.Length; i++)
        {
            var a = outline[i]; var b = outline[(i + 1) % outline.Length];
            var af = new Vector3(a.x, a.y, z + thickness / 2); var bf = new Vector3(b.x, b.y, z + thickness / 2);
            var ab = new Vector3(a.x, a.y, z - thickness / 2); var bb = new Vector3(b.x, b.y, z - thickness / 2);
            Triangle(new Vector3(center.x, center.y, z + thickness / 2), af, bf);
            Triangle(new Vector3(center.x, center.y, z - thickness / 2), bb, ab);
            Triangle(af, ab, bb); Triangle(af, bb, bf);
        }
        var obj = Visuals.MeshObject(parent, name, vertices, triangles, material);
        obj.GetComponent<MeshFilter>().sharedMesh.SetUVs(0, vertices.Select(v => new Vector2(v.x * 2f, v.y * 2f)).ToList());
        return obj;
    }

    private static void EntryRamp(Transform parent)
    {
        // A shallow physical threshold, without a visible plank in front of the cabinet.
        var obj = new GameObject("BH_EntryRamp");
        obj.transform.SetParent(parent, false);
        obj.layer = LayerMask.NameToLayer("piece");
        var mesh = new Mesh { name = "BH coffin threshold wedge" };
        mesh.vertices = new[] {
            new Vector3(-.46f, -.10f, .08f), new Vector3(.46f, -.10f, .08f),
            new Vector3(.46f, -.10f, 1.4f), new Vector3(-.46f, -.10f, 1.4f),
            new Vector3(-.46f, .20f, .08f), new Vector3(.46f, .20f, .08f),
            new Vector3(.46f, .01f, 1.4f), new Vector3(-.46f, .01f, 1.4f)
        };
        mesh.triangles = new[] { 0,1,2, 0,2,3, 4,6,5, 4,7,6, 0,5,1, 0,4,5, 3,6,7, 3,2,6, 0,7,4, 0,3,7, 1,6,2, 1,5,6 };
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var collider = obj.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
        collider.convex = true;
    }
}
