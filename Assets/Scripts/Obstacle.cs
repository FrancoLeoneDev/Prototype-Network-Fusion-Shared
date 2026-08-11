using System.Collections;
using Fusion;
using UnityEngine;

/// <summary>
/// Hazard that sweeps up and down on a fixed interval. Touching it sends the
/// player back to its spawn point (handled by <see cref="Player"/>).
/// </summary>
public class Obstacle : NetworkBehaviour
{
    private const float DirectionFlipInterval = 1f;
    private const float MoveSpeed = 3f;

    [SerializeField] private bool inverse;

    private int _direction = 1;

    public override void Spawned()
    {
        if (!HasStateAuthority) return;

        if (inverse)
        {
            _direction = -_direction;
        }

        StartCoroutine(FlipDirectionLoop());
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        transform.position += Vector3.up * (_direction * MoveSpeed * Runner.DeltaTime);
    }

    private IEnumerator FlipDirectionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(DirectionFlipInterval);
            _direction *= -1;
        }
    }
}
