using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Smoothly trails a target transform. The local player assigns
/// <see cref="Target"/> once it spawns.
/// </summary>
public class CameraBehavior : MonoBehaviour
{
    [FormerlySerializedAs("pLerp")]
    [SerializeField, Range(0.001f, 1f)]
    [Tooltip("Interpolation factor applied every frame. Lower trails further behind.")]
    private float followLerp = 0.02f;

    /// <summary>Transform the camera follows. Null until the local player spawns.</summary>
    public Transform Target { get; set; }

    private void LateUpdate()
    {
        if (Target == null) return;

        transform.position = Vector3.Lerp(transform.position, Target.position, followLerp);
        transform.rotation = Target.rotation;
    }
}
