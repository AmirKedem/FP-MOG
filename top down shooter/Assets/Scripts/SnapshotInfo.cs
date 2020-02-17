using UnityEngine;

public class SnapshotInfo : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private GameObject playerContainer;
    [SerializeField] private int snapshotTick;

    public Player Player { get => player; }
    public GameObject PlayerContainer { get => playerContainer; }
    public int SnapshotTick { get => snapshotTick; }

    public void Init(Player player, GameObject playerContainer, int snapshotTick)
    {
        this.player = player;
        this.playerContainer = playerContainer;
        this.snapshotTick = snapshotTick;
    }
}
