using Fusion;
using UnityEngine;

/// <summary>
/// Visual state of a player that has to stay in sync across clients. The colour
/// is a networked property, so late joiners receive the current value on spawn
/// and <see cref="OnColorChanged"/> reapplies it without an explicit RPC.
/// </summary>
public class PlayerView : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Renderer _renderer;

    [Networked]
    [OnChangedRender(nameof(OnColorChanged))]
    [HideInInspector]
    public Color NetworkedColor { get; set; }

    public override void Spawned()
    {
        if (_renderer == null)
        {
            _renderer = GetComponentInChildren<Renderer>();
        }

        if (HasStateAuthority)
        {
            NetworkedColor = Color.white;
        }
    }

    private void OnColorChanged()
    {
        if (_renderer != null)
        {
            _renderer.material.color = NetworkedColor;
        }
    }
}
