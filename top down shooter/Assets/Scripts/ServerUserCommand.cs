using System.Collections.Generic;

/// <summary>
/// This class is used in the server only, when a player sends input to the server 
/// the server wraps the input with this class, it stores the receive Time of the commands
/// with is then used when applying them in the simulation throught the ticks.
/// </summary>
public class ServerUserCommand
{
    public Player player;
    public float serverRecTime;
    public InputEvent ie;

    public ServerUserCommand(Player player, float serverRecTime, InputEvent ie)
    {
        this.player = player;
        this.serverRecTime = serverRecTime;
        this.ie = ie;
    }

    public static List<ServerUserCommand> CreaetUserCommands(Player player, ClientInput ci)
    {
        List<ServerUserCommand> ret = new List<ServerUserCommand>();
        float currTime = StopWacthTime.Time;

        foreach (InputEvent ie in ci.inputEvents)
        {
            ret.Add(new ServerUserCommand(player, currTime + ie.deltaTime, ie));
        }

        return ret;
    }
}
