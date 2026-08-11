using TMPro;
using UnityEngine;

/// <summary>
/// Owns the end-of-race screen. Each client resolves the result relative to its
/// own player, so the same broadcast renders as a win on one side and a loss on
/// the other.
/// </summary>
public class UiManager : MonoBehaviour
{
    public static UiManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI victoryMesh;

    private GameObject _victoryTextObject;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        _victoryTextObject = victoryMesh.gameObject;
        _victoryTextObject.SetActive(false);
    }

    public void SetVictoryScreen(Player winner)
    {
        bool localPlayerWon = winner == Player.LocalPlayer;

        _victoryTextObject.SetActive(true);
        victoryMesh.text = localPlayerWon ? "You Win!" : "You Lose!";
        victoryMesh.color = localPlayerWon ? Color.green : Color.red;
    }
}
