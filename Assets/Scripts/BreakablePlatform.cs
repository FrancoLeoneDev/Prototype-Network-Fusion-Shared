using System.Collections;
using Fusion;
using UnityEngine;

/// <summary>
/// Platform that collapses shortly after a player steps on it and reappears
/// once the same delay has elapsed again.
/// </summary>
public class BreakablePlatform : NetworkBehaviour
{
    [SerializeField] private float breakDelay = 1f;

    private BoxCollider _platformCollider;
    private MeshRenderer _meshRenderer;
    private bool _isCollapsing;

    private void Awake()
    {
        // Cached in Awake so a state RPC arriving before Spawned() still applies.
        _platformCollider = GetComponent<BoxCollider>();
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_isCollapsing || !collision.gameObject.CompareTag("Player")) return;

        // Latched before the delay starts: the previous version set this only
        // after the wait, so every extra collision stacked another coroutine.
        _isCollapsing = true;
        StartCoroutine(CollapseThenRestore());
    }

    private IEnumerator CollapseThenRestore()
    {
        yield return new WaitForSeconds(breakDelay);
        SetBrokenRpc(true);

        yield return new WaitForSeconds(breakDelay);
        SetBrokenRpc(false);

        _isCollapsing = false;
    }

    /// <summary>
    /// Mirrors the collapsed state to every client. Sourced from All because in
    /// Shared Mode the player that triggers the collapse owns only itself, not
    /// the platform.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void SetBrokenRpc(bool isBroken)
    {
        if (_platformCollider != null) _platformCollider.enabled = !isBroken;
        if (_meshRenderer != null) _meshRenderer.enabled = !isBroken;
    }
}
