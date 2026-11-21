using UnityEngine;

namespace Bonehilda;

// Only the free bow tails use this joint. Sample the animated knot in world
// space so both locomotion and a reaching pose can displace the ribbons.
internal sealed class ApronRibbon : MonoBehaviour
{
    private Vector3 previous;
    private Vector3 tilt;
    private Vector3 velocity;

    private void OnEnable()
    {
        previous = transform.position;
        tilt = velocity = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    private void LateUpdate()
    {
        var delta = transform.position - previous;
        previous = transform.position;
        var dt = Time.deltaTime;
        if (dt <= 0) return;
        var motion = delta.sqrMagnitude < 1f ? delta / dt : Vector3.zero;
        var target = Vector3.ClampMagnitude(Vector3.Cross(Vector3.up, motion) * 5f, 16f);
        tilt = Vector3.SmoothDamp(tilt, target, ref velocity, .18f, 160f, dt);
        var spin = Quaternion.AngleAxis(tilt.magnitude, tilt.normalized);
        var parent = transform.parent.rotation;
        transform.localRotation = Quaternion.Inverse(parent) * spin * parent;
    }
}
