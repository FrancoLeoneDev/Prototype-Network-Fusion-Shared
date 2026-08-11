using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

/// <summary>
/// Spawns the local player when it joins the session and gates the race start
/// until enough players are present.
/// </summary>
/// <remarks>
/// Shared Mode topology: every client spawns and holds state authority over its
/// own player object, so this callback runs per-client rather than on a host.
/// </remarks>
public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    private const int MinPlayersToStart = 2;

    public static PlayerSpawner Instance { get; private set; }

    /// <summary>Position the local player spawned at. Reused when respawning after a hit.</summary>
    public static Vector3 SpawnPosition { get; private set; }

    [SerializeField] private GameObject _playerPrefab;
    [SerializeField] private List<Transform> spawnPoints;

    private bool _gameStart;

    /// <summary>True once enough players joined for the race to begin.</summary>
    public bool GameStart => _gameStart;

    private void Awake()
    {
        Instance = this;
    }

    public void PlayerJoined(PlayerRef player)
    {
        // Spawn before evaluating the start gate. The previous early-return ran
        // first, so whenever a remote player's callback arrived before the local
        // one, the local player was never spawned at all.
        if (player == Runner.LocalPlayer)
        {
            SpawnPosition = ResolveSpawnPosition();
            Runner.Spawn(_playerPrefab, SpawnPosition, Quaternion.identity);
        }

        _gameStart |= Runner.ActivePlayers.Count() >= MinPlayersToStart;
    }

    /// <summary>
    /// Gives the joining player the spawn point matching its join order, falling
    /// back to the origin once the configured points are exhausted.
    /// </summary>
    private Vector3 ResolveSpawnPosition()
    {
        int joinIndex = Runner.ActivePlayers.Count() - 1;

        return joinIndex >= 0 && joinIndex < spawnPoints.Count
            ? spawnPoints[joinIndex].position
            : Vector3.zero;
    }
}
