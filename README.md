# FP MOG

## Fast Paced Multiplayer Online Game

A simple game with client side prediction, interpolation and lag compensation.<br>

The game is built with unity both the server and client are created from the same unity project and are currently target 64-bit windows machines.

#### Setup

The project is using unity version 2019.2.6f1

#### Project Managment

- Project Tools
To build the project and move between the scenes use the project tools window in the editor.

- Launch Tools
To launch it use the launch tool, add an entry using the Add Entry button configure it as you like and if you have built the project the correct build will run from the builds folder.

## Short Demo

In the following gif there is a demonstration of lag compensation taken from the project.
In the right side of the gif (The Client's window) there is two tanks one is the client predicted entity (red tank), and another one which shows the player from the snapshots that were received from the server (purple tank). By Using Clumsy I introudced 500 ms of lag (250ms for inbound and another 250ms for outbound hence 500ms) therefore, we can see a significant delay between the red tank and the purple tank.

[<img src="Lag Compensation Demo.gif" width="100%">](#readme)

This demonstration is not the actual project, it was taken on early stages of development.

[<img src="Lag Compensation.png" width="100%">](#readme)

## Netcode

For real-time games, an authoritative server controls the game state. Clients are only responsible for sending commands ('Left', 'Right', 'Shoot', etc.) and rendering the state.

While this approach prevents cheating and allows lower-end machines to run the game, it introduces some delay between clients. Furthermore, the server can't send 60fps updates to every player in the room.

Therefore, game devs have developed some neat algorithms to make the game experience smoother and lower the perceived lag by the players.

## Algorithms:
    - Lag Compensation
    - Client Side Interpolation
    - Client Side Prediction

![Imgur](https://i.imgur.com/nIhGLDz.gif)

In this demo, some delay is introduced to simulate a real client-server communication.

## Theory

### Client-Side Interpolation

#### Smooth WORLD movement

Clients rely on interpolation to render a fluid and smooth game experience: instead of rendering immediately every new state, the client buffers them and then performs linear interpolation between the last 2.

Interpolation ensures that objects will move between valid positions only and will produce good results with constant delay and no loss. The downside of interpolation is that it causes the world to be rendered with additional latency, increasing the need for some form of lag compensation to be implemented.

Client-side Interpolation is performed only on the other players, or enemies, since the player's entity is using client side prediction.


### Client-Side Prediction

#### Smooth PLAYER movement

In order to allow smooth gameplay, the client is allowed to do minor changes to the game state, which means he can preform physics actions on his character. While the server may ultimately keep track of ammunition, health, position, etc., the client may be allowed to predict the new server-side game state based on the player's actions, such as allowing a player to start moving before the server has responded to the command. These changes will generally be accepted under normal conditions and make delay mostly transparent. Problems will arise only in the case of high delays or losses, when the client's predictions are very noticeably undone by the server. Sometimes, in the case of minor differences, the server may even allow "incorrect" changes to the state based on updates from the client.

The problem with extrapolating positions is fairly obvious: it is impossible to accurately predict the future. It will render movement correctly only if the movement is constant, but this will not always be the case. Players may change both speed and direction at random. This may result in a small amount of "warping" as new updates arrive and the estimated positions are corrected, and also cause problems for hit detection as players may be rendered in positions they are not actually in.

Client-side prediction is performed only on the entity of the client, or his character.


### Lag Compensation

#### Server Side
Unlike clients, the server knows the exact current game state, and as such prediction is unnecessary. The main purpose of server-side lag compensation is instead to provide accurate effects of client actions (Some client trust is needed). This is important because by the time a player's command has arrived time will have moved on, and the world will no longer be in the state that the player saw when issuing their command. 

A very explicit example of this is hit detection for weapons fired in first-person shooters, where margins are small and can potentially cause significant problems if not properly handled, i.e., lag compensation enables shooter games to have headshoots and accurate hit detection. This is especially important and noticeable by clients when the hit detection system is hitscan or ray cast.

#### Rewind time
One way to address the issue is to store past game states for a certain length of time, then rewind player locations when processing a shooting command. The server uses the latency of the player (including any inherent delay due to interpolation; see above) to rewind time by an appropriate amount in order to determine what the shooting client saw at the time the shot was fired. This will usually result in the server seeing the client firing at the target's old position and thus hitting. In the worst case, a player will be so far behind that the server runs out of historical data and they have to start leading their targets.


## Resources
https://github.com/MFatihMAR/Game-Networking-Resources

## Status

This project is no longer under active development,<br>
but code contributions are more than welcome.
