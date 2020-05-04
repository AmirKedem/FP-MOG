using UnityEngine;

public class ProxyPlayer : MonoBehaviour
{
    /// <summary>
    /// A strip down container for the player
    /// used to "show" differnet mods of using the server's data
    /// for example: 
    ///     raw data simply display the state as soon as possible
    ///     interpolated data the interpolation result of the raw data
    ///     
    /// The CSP client is simply another player, not a proxy player.
    /// it simulates the inputs and show the result.
    /// </summary>
    public GameObject playerContainer;
    public GameObject playerGameobject;
    public Rigidbody2D rb;

    public PlayerState GetState()
    {
        return new PlayerState(0, playerGameobject.transform.eulerAngles.z, playerGameobject.transform.position);
    }

    public void FromState(PlayerState ps)
    {
        playerGameobject.transform.position = new Vector2(ps.pos[0], ps.pos[1]);
        playerGameobject.transform.eulerAngles = new Vector3(0, 0, ps.zAngle);
    }
}
