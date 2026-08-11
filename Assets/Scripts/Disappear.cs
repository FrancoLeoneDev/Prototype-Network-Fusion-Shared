using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Platform that cycles between visible and hidden on a fixed timer. The state
/// authority drives the cycle and mirrors each flip to the other clients.
/// </summary>
public class Disappear : NetworkBehaviour
{
    [SerializeField] private float activeDuration = 2f;

    [FormerlySerializedAs("desactiveDuration")]
    [SerializeField] private float inactiveDuration = 2f;

    private BoxCollider _collider;
    private MeshRenderer _meshRenderer;
    private bool _isActive = true;

    private void Awake()
    {
        // Cached in Awake so a state RPC arriving before Spawned() still applies.
        _collider = GetComponent<BoxCollider>();
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            StartCoroutine(ToggleVisibilityLoop());
        }
    }

    private IEnumerator ToggleVisibilityLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(_isActive ? activeDuration : inactiveDuration);

            _isActive = !_isActive;
            SetActiveStateRpc(_isActive);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void SetActiveStateRpc(bool isActive)
    {
        if (_collider != null) _collider.enabled = isActive;
        if (_meshRenderer != null) _meshRenderer.enabled = isActive;
    }
}
