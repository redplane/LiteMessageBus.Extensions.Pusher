## LiteMessageBus.Extensions.Pusher
--- 

1. **Description**:
    A small library that uses [Pusher](https://pusher.com) to implement message bus service in the system to exchange information between background tasks. This library works only on ASP.Net Core.

2. **Installation**:
    For now, the library is published on [myget.org](https://www.myget.org/feed/lite-message-bus/package/nuget/LiteMessageBus.Extensions.Pusher) only. 
    1. Follow this [tutorial](https://docs.myget.org/docs/walkthrough/getting-started-with-nuget) to add [https://www.myget.org/feed/Packages/lite-message-bus](https://www.myget.org/feed/Packages/lite-message-bus) as new feed.
    2. Intall package using nuget package manager.

3. **Interfaces & classes**:
    3.1: `ILiteMessageBusService`:

    - By default  `ILiteMessageBusService` is provided with some methods:
        - **void AddMessageChannel<T>(string channelName, string eventName)**: To add a message channel into message bus manager instance.
    
        - **IObservable<T> HookMessageChannel<T>(string channelName, string eventName)**: To catch to a message channel to listen to messages that are broadcasted through the channel with specific event names.
    
        - **void AddMessage<T>(string channelName, string eventName, T data)**: Broadcast a message into a channel - event pair.
    
        - **void DeleteMessage(string channelName, string eventName)**: Delete a message from a specific channel and event.
        
        - **void DeleteMessages()**: Delete messages from all channels and events.
    
    3.2: `LiteMessageBusPusherExtensions`:
    - The extension to helps developer to register an **In-memory message bus** service into **ASP.Net Core** system.
    
    - In **Startup.cs**, put `services.AddPusherLiteMessageBus(appKey, appId, appSecret, cluster, authorizer)` to register the service bus manager in the system.
