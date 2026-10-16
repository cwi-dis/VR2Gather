# VR2Gather - Typed network messages

Sometimes participants need to exchange application-specific data: synchronising game state, signalling that a participant is ready, sharing choices. VR2Gather supports this with *typed messages*: plain C# classes that are serialised to JSON and routed through the session master.

For simple "something happened, everyone react" events you usually don't need this: a `NetworkTrigger` (see [Interaction system](100-interaction.md)) is simpler. Use typed messages when you need to send data along with the event.

The API is in `VRT.OrchestratorComm` (`OrchestratorCommExtensions.cs`, `MessageForwarder.cs`). Examples in the package itself are `TilingConfigDistributor` (any participant sends) and `PersistenceManager` (only the master sends).

## How messages are routed

```
Non-master --SendTypeEventToMaster()--> Master
                                           |
                              SendTypeEventToAll(msg, true)
                                           |
                                    All other participants
```

- Only the master can send to all participants, with `SendTypeEventToAll`.
- Non-master participants send to the master, and the master forwards the message to everyone.
- The master does not receive its own `SendTypeEventToAll` messages. But when the master forwards a non-master's message, the original sender does receive it back (with the original `SenderId`). Handlers should drop these self-echoes.
- In a standalone (single-user) session, sending does nothing. Always apply the effect of your own action locally.

## Step 1: choose a type ID

Every message type needs a unique integer ID, which is sent over the network. IDs 100-199 are reserved for VR2Gather itself (the `MessageTypeID` enum in `MessageForwarder.cs`). Applications use IDs from `MessageTypeID.TID_FirstAppDefined` (200) upward:

```csharp
internal static class MyAppMsgID
{
    public const int FooEvent = 200;
    public const int BarEvent = 201;
}
```

Do not add your IDs to the `MessageTypeID` enum: that file is part of the package.

## Step 2: define the message class

A message is a class that inherits from `BaseMessage`. Public fields are the payload; they are serialised with Unity's `JsonUtility`, so the usual `JsonUtility` restrictions apply.

```csharp
using VRT.OrchestratorComm;

public class FooEventMessage : BaseMessage
{
    public string playerId;
    public long timestampMs;
}

public class BarEventMessage : BaseMessage { }   // no payload
```

`BaseMessage` already has `SenderId` (filled in when sending) and `TimeStamp`.

## Step 3: register the type

Call `RegisterEventType` once per message type before sending or receiving, for example in `Awake()`. Registering the same type with the same ID again is harmless, so it is fine for multiple instances or repeated scene loads to do this. Registering a different type with an ID that is already in use is logged as an error.

```csharp
void Awake()
{
    VRTOrchestratorSingleton.Comm.RegisterEventType(MyAppMsgID.FooEvent, typeof(FooEventMessage));
    VRTOrchestratorSingleton.Comm.RegisterEventType(MyAppMsgID.BarEvent, typeof(BarEventMessage));
}
```

## Step 4: subscribe and unsubscribe

Subscribe in `OnEnable()` and unsubscribe in `OnDisable()`, so subscriptions are always cleaned up when the component or scene goes away. The `?.` guards against the orchestrator already having been destroyed at scene unload.

```csharp
void OnEnable()
{
    VRTOrchestratorSingleton.Comm.Subscribe<FooEventMessage>(OnFooEvent);
}

void OnDisable()
{
    VRTOrchestratorSingleton.Comm?.Unsubscribe<FooEventMessage>(OnFooEvent);
}
```

## Step 5: send

If only the master ever sends this message (for example "start the game"):

```csharp
if (!VRTOrchestratorSingleton.Comm.UserIsMaster) return;
VRTOrchestratorSingleton.Comm.SendTypeEventToAll(new BarEventMessage());
ApplyBarEffect();   // the master doesn't get its own message back
```

If any participant can send it (for example a player action):

```csharp
var msg = new FooEventMessage { playerId = VRTOrchestratorSingleton.Comm.SelfUser.userId };
if (VRTOrchestratorSingleton.Comm.UserIsMaster)
    VRTOrchestratorSingleton.Comm.SendTypeEventToAll(msg);
else
    VRTOrchestratorSingleton.Comm.SendTypeEventToMaster(msg);
ApplyFooEffect(msg.playerId);   // apply locally right away
```

## Step 6: handle incoming messages

For a message that any participant can send, the handler:

1. On the master: forwards the message to everyone, passing `true` so the original `SenderId` is kept.
2. Drops self-echoes (the message was sent by this participant, and the effect has already been applied locally).
3. Applies the effect.

```csharp
void OnFooEvent(FooEventMessage msg)
{
    if (VRTOrchestratorSingleton.Comm.UserIsMaster)
        VRTOrchestratorSingleton.Comm.SendTypeEventToAll(msg, true);
    if (msg.SenderId == VRTOrchestratorSingleton.Comm.SelfUser?.userId) return;
    ApplyFooEffect(msg.playerId);
}
```

For a master-only message, only steps 2 and 3 are needed.

Back to the [Developer Overview](01-overview.md).
