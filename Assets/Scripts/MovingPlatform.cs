using Fusion;
using UnityEngine;

/// <summary>
/// Platform that shuttles between its starting position and a configured target
/// point. Only the state authority simulates the motion; the other clients
/// receive it through the NetworkTransform on the prefab.
/// </summary>
public class MovingPlatform : NetworkBehaviour
{
    private const float ArrivalThreshold = 0.001f;

    [SerializeField] private Vector3 pointA;
    [SerializeField] private Vector3 pointB;
    [SerializeField] private float speed = 1f;

    private Vector3 _targetPoint;

    public override void Spawned()
    {
        if (!HasStateAuthority) return;

        // pointA is wherever the platform was authored in the scene; pointB is
        // the only end of the path that needs configuring.
        pointA = transform.position;
        _targetPoint = pointB;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        float step = speed * Runner.DeltaTime;
        transform.position = Vector3.MoveTowards(transform.position, _targetPoint, step);

        if (Vector3.Distance(transform.position, _targetPoint) < ArrivalThreshold)
        {
            _targetPoint = _targetPoint == pointA ? pointB : pointA;
        }
    }
}
