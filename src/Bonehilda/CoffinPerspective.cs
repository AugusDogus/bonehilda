using UnityEngine;

namespace Bonehilda;

// Shorten the visible stride inside the shallow coffin. The skeleton, clothes
// and equipment share these transforms. The character's physics stays on its root.
internal sealed class CoffinPerspective : MonoBehaviour
{
    private ZNetView? view;
    private bool compressed;
    private int spawnFrame;

    internal static void Attach(Animator animator)
    {
        var rig = animator.transform;
        var depth = Visuals.Child(rig.parent, "BH_CoffinPerspective");
        var facing = Visuals.Child(depth, "Rig facing");
        rig.SetParent(facing, false);
        depth.gameObject.AddComponent<CoffinPerspective>();
    }

    private void Awake()
    {
        view = GetComponentInParent<ZNetView>();
        spawnFrame = Time.frameCount;
    }

    // Unity retargets humanoid hips in the animator's world scale. Give it the
    // unmodified rig for pose evaluation and apply perspective only afterward.
    private void Update() => Restore();
    private void FixedUpdate() => Restore();

    private Coffin? Passage()
    {
        if (view == null || !view.IsValid() || !ZNetScene.instance) return null;
        var data = view.GetZDO();
        if (!Maid.InCoffinPassage(data)) return null;
        var home = ZNetScene.instance.FindInstance(data.GetZDOID(Maid.HomeKey));
        return home ? home.GetComponent<Coffin>() : null;
    }

    private void LateUpdate()
    {
        // The first humanoid pose must initialize at native scale.
        if (Time.frameCount == spawnFrame) return;
        var coffin = Passage();
        if (coffin == null) return;
        var z = coffin.transform.InverseTransformPoint(transform.position).z / CoffinVisuals.Size(coffin.Stars);
        var depth = Mathf.SmoothStep(.40f, 1f, Mathf.InverseLerp(.10f, .64f, z));
        if (depth >= 1f) return;

        // Align the compression axis with the cabinet, then undo that rotation
        // beneath the scale so the maid can still turn naturally in the doorway.
        var rotation = Quaternion.Inverse(transform.parent.rotation) * coffin.transform.rotation;
        transform.localRotation = rotation;
        transform.GetChild(0).localRotation = Quaternion.Inverse(rotation);
        transform.localScale = new Vector3(1f, 1f, depth);
        compressed = true;
    }

    private void OnDisable() => Restore();

    private void Restore()
    {
        if (!compressed) return;
        transform.localRotation = Quaternion.identity;
        transform.GetChild(0).localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        compressed = false;
    }
}
