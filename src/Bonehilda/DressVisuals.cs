using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Jotunn.Managers;

namespace Bonehilda;

internal static class DressVisuals
{
    private static Material[]? dresses;
    private static Material? linen;

    internal static void SetTier(Transform skeleton, int stars)
    {
        var outfit = skeleton.Find("BH_Outfit");
        if (!outfit || dresses == null) return;
        foreach (var renderer in outfit.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (renderer.name == "BH fitted dress") renderer.sharedMaterial = dresses[Mathf.Clamp(stars, 0, 2)];
    }

    internal static void Build(Transform skeleton)
    {
        if (skeleton.Find("BH_Outfit")) return;
        var source = skeleton.GetComponentsInChildren<SkinnedMeshRenderer>(true)
            .FirstOrDefault(r => r.sharedMesh && r.bones.Length > 0 && r.sharedMesh.bindposes.Length == r.bones.Length);
        if (!source) throw new InvalidOperationException("Skeleton has no usable skin binding for her dress.");
        var template = Visuals.Template(PrefabManager.Instance.GetPrefab("piece_chest_wood"));
        if (dresses == null)
        {
            dresses = new[]
            {
                Visuals.Material(template, "BH red dress", new Color(0.48f, 0.035f, 0.045f)),
                Visuals.Material(template, "BH black dress", new Color(0.035f, 0.032f, 0.045f)),
                Visuals.Material(template, "BH blue dress", new Color(0.045f, 0.065f, 0.19f))
            };
            linen = Visuals.Material(template, "BH fitted ivory linen", new Color(0.82f, 0.78f, 0.68f));
        }
        var bind = source.sharedMesh.bindposes;
        int Bone(string name)
        {
            var index = Array.FindIndex(source.bones, b => b && b.name == name);
            if (index < 0) throw new InvalidOperationException("Dress binding is missing " + name);
            return index;
        }
        var hips = Bone("Hips"); var chest = Bone("Spine2"); var head = Bone("Head");
        var leftArm = Bone("LeftArm"); var rightArm = Bone("RightArm");
        var leftFoot = Bone("LeftFoot"); var rightFoot = Bone("RightFoot");
        // Invert the native bind matrices, rather than measuring already-animated
        // transforms during Start. This keeps the garment aligned in every pose.
        Vector3 Rest(int bone) => skeleton.InverseTransformPoint(source.transform.TransformPoint(bind[bone].inverse.MultiplyPoint3x4(Vector3.zero)));
        var hip = Rest(hips); var upper = Rest(chest); var skull = Rest(head);
        var left = Rest(leftArm); var right = Rest(rightArm);
        var foot = (Rest(leftFoot) + Rest(rightFoot)) * 0.5f;
        var height = Mathf.Max(0.5f, skull.y - foot.y);
        var shoulderWidth = Mathf.Abs(left.x - right.x);
        if (shoulderWidth < height * 0.15f) shoulderWidth = height * 0.30f;
        var waistY = hip.y + height * 0.015f;
        var topY = (left.y + right.y) * 0.5f - height * 0.035f;
        var hemY = waistY - height * 0.30f;
        var waistRadius = shoulderWidth * 0.37f;
        var topRadius = shoulderWidth * 0.47f;
        var hemRadius = shoulderWidth * 0.59f;

        var outfit = new GameObject("BH_Outfit").transform;
        outfit.SetParent(skeleton, false);
        var cloth = new SkinBuilder(skeleton, source, outfit, "BH fitted dress", dresses[1]);
        var apron = new SkinBuilder(skeleton, source, outfit, "BH fitted apron", linen!);
        var trim = new SkinBuilder(skeleton, source, outfit, "BH fitted lace", linen!);
        BoneWeight Weight(int bone) => new BoneWeight { boneIndex0 = bone, weight0 = 1f };
        BoneWeight BodyWeight(Vector3 p)
        {
            var blend = Mathf.Clamp01((p.y - hip.y) / Mathf.Max(0.01f, upper.y - hip.y));
            return new BoneWeight { boneIndex0 = hips, weight0 = 1 - blend, boneIndex1 = chest, weight1 = blend };
        }
        Vector3 Center(float y)
        {
            var blend = Mathf.Clamp01((y - hip.y) / Mathf.Max(0.01f, upper.y - hip.y));
            var center = Vector3.Lerp(hip, upper, blend); center.y = y; return center;
        }
        void Ring(SkinBuilder builder, float y1, float r1, float y2, float r2, bool front, Func<Vector3, BoneWeight> weight, float lift = 0)
        {
            const int segments = 24;
            var start = front ? -0.85f : 0;
            var span = front ? 1.7f : Mathf.PI * 2;
            Vector3 Point(float a, float y, float radius)
            {
                // The skeleton's ribcage is deeper than the waist. Keep the bodice
                // outside the ribs, then flatten the flare gradually toward its hem.
                var depth = Mathf.Lerp(0.76f, 0.98f, Mathf.Clamp01((y - hemY) / (waistY - hemY)));
                return Center(y) + new Vector3(Mathf.Sin(a) * radius, 0, Mathf.Cos(a) * (radius * depth + lift));
            }
            for (var i = 0; i < segments; i++)
            {
                var a = start + span * i / segments; var b = start + span * (i + 1) / segments;
                builder.Quad(Point(a, y1, r1), Point(a, y2, r2), Point(b, y2, r2), Point(b, y1, r1), weight);
            }
        }
        // Multiple rings follow the ribcage and waist instead of a box suspended from one joint.
        var midY = Mathf.Lerp(waistY, topY, 0.62f);
        Ring(cloth, topY, topRadius, midY, topRadius * 0.98f, false, BodyWeight);
        Ring(cloth, midY, topRadius * 0.98f, waistY, waistRadius, false, BodyWeight);
        Ring(cloth, waistY, waistRadius, hemY, hemRadius, false, _ => Weight(hips));
        Ring(apron, topY - height * 0.02f, topRadius + height * 0.004f, midY, topRadius, true, BodyWeight, height * 0.006f);
        Ring(apron, midY, topRadius, waistY, waistRadius + height * 0.004f, true, BodyWeight, height * 0.006f);
        Ring(apron, waistY, waistRadius + height * 0.005f, hemY + height * 0.035f, hemRadius * 0.94f, true, _ => Weight(hips), height * 0.009f);
        Ring(trim, waistY + height * 0.008f, waistRadius * 1.03f, waistY - height * 0.012f, waistRadius * 1.06f, false, _ => Weight(hips));
        Ring(trim, hemY + height * 0.018f, hemRadius * 0.99f, hemY, hemRadius * 1.012f, false, _ => Weight(hips));
        Ring(trim, topY + height * 0.009f, topRadius * 1.005f, topY - height * 0.008f, topRadius * 1.009f, false, BodyWeight);
        cloth.Ellipsoid(left, new Vector3(height * 0.065f, height * 0.068f, height * 0.065f), Weight(leftArm));
        cloth.Ellipsoid(right, new Vector3(height * 0.065f, height * 0.068f, height * 0.065f), Weight(rightArm));
        trim.Ellipsoid(skull + Vector3.up * height * 0.18f, new Vector3(height * 0.095f, height * 0.018f, height * 0.075f), Weight(head));
        for (var i = -4; i <= 4; i++)
            trim.Ellipsoid(skull + new Vector3(i * height * 0.018f, height * 0.195f, height * 0.027f), new Vector3(height * 0.014f, height * 0.017f, height * 0.025f), Weight(head));
        foreach (var side in new[] { -1f, 1f })
            trim.Ellipsoid(hip + new Vector3(side * height * 0.045f, height * 0.012f, -waistRadius * 0.78f), new Vector3(height * 0.046f, height * 0.026f, height * 0.018f), Weight(hips));
        var renderers = new[] { cloth.Finish(), apron.Finish(), trim.Finish() };
        var lod = skeleton.GetComponentInChildren<LODGroup>(true);
        if (lod)
        {
            var levels = lod.GetLODs();
            for (var i = 0; i < levels.Length; i++) levels[i].renderers = levels[i].renderers.Concat(renderers).ToArray();
            lod.SetLODs(levels); lod.RecalculateBounds();
        }
    }

    private sealed class SkinBuilder
    {
        private readonly Transform skeleton;
        private readonly SkinnedMeshRenderer source;
        private readonly Transform parent;
        private readonly string name;
        private readonly Material material;
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<BoneWeight> weights = new List<BoneWeight>();
        private readonly List<int> triangles = new List<int>();
        private readonly List<Vector2> uv = new List<Vector2>();
        internal SkinBuilder(Transform skeleton, SkinnedMeshRenderer source, Transform parent, string name, Material material)
        { this.skeleton = skeleton; this.source = source; this.parent = parent; this.name = name; this.material = material; }

        internal void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Func<Vector3, BoneWeight> weight)
        {
            var n = vertices.Count;
            foreach (var p in new[] { a, b, c, d })
            {
                vertices.Add(source.transform.InverseTransformPoint(skeleton.TransformPoint(p)));
                weights.Add(weight(p)); uv.Add(new Vector2(p.x * 4, p.y * 4));
            }
            triangles.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
        }

        internal void Ellipsoid(Vector3 center, Vector3 radius, BoneWeight weight)
        {
            const int rings = 8; const int sides = 12;
            Vector3 Point(int ring, int side)
            {
                var a = Mathf.PI * ring / rings; var b = Mathf.PI * 2 * side / sides;
                return center + Vector3.Scale(radius, new Vector3(Mathf.Sin(a) * Mathf.Sin(b), Mathf.Cos(a), Mathf.Sin(a) * Mathf.Cos(b)));
            }
            for (var ring = 0; ring < rings; ring++)
            for (var side = 0; side < sides; side++) Quad(Point(ring, side), Point(ring + 1, side), Point(ring + 1, side + 1), Point(ring, side + 1), _ => weight);
        }

        internal SkinnedMeshRenderer Finish()
        {
            // Distinct back-face vertices give fabric a finished inner surface and valid normals.
            var count = vertices.Count;
            vertices.AddRange(vertices.ToArray()); weights.AddRange(weights.ToArray()); uv.AddRange(uv.ToArray());
            var front = triangles.ToArray();
            for (var i = 0; i < front.Length; i += 3) triangles.AddRange(new[] { count + front[i + 2], count + front[i + 1], count + front[i] });
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.SetUVs(0, uv);
            mesh.SetColors(Enumerable.Repeat(Color.white, vertices.Count).ToList());
            mesh.boneWeights = weights.ToArray(); mesh.bindposes = source.sharedMesh.bindposes;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var obj = new GameObject(name).transform;
            obj.SetParent(parent, false);
            // Match the native mesh's coordinate system exactly, including imported model scale.
            obj.position = source.transform.position; obj.rotation = source.transform.rotation;
            var rootScale = parent.lossyScale; var skinScale = source.transform.lossyScale;
            obj.localScale = new Vector3(skinScale.x / rootScale.x, skinScale.y / rootScale.y, skinScale.z / rootScale.z);
            var renderer = obj.gameObject.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = mesh; renderer.bones = source.bones; renderer.rootBone = source.rootBone;
            renderer.sharedMaterial = material; renderer.updateWhenOffscreen = true;
            renderer.localBounds = mesh.bounds;
            return renderer;
        }
    }
}
