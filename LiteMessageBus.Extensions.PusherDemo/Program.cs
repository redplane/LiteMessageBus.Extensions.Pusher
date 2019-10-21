using System;
using LiteMessageBus.Extensions.Pusher.Services;
using LiteMessageBus.Extensions.PusherDemo.Constants;
using LiteMessageBus.Extensions.PusherDemo.Models;
using PusherClient;

namespace LiteMessageBus.Extensions.PusherDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            var appKey = "7924946bbbd2fc014135";
            var appId = "875585";
            var appSecret = "a6c2c1f9ae29637d995a";
            var cluster = "ap1";
            
            var pusherServerOptions = new PusherServer.PusherOptions();
            pusherServerOptions.Cluster = cluster;
            pusherServerOptions.Encrypted = true;
            
            var pusherClientOptions = new PusherClient.PusherOptions();
            pusherClientOptions.Cluster = cluster;
            pusherClientOptions.Encrypted = true;

            var broadcaster = new PusherServer.Pusher(appId,
                appKey, appSecret, pusherServerOptions);

            var recipient = new PusherClient.Pusher(appKey, pusherClientOptions);

            // Pusher message bus initialization.
            var pusherMessageBus = new PusherLiteMessageBusService(broadcaster, recipient);
            pusherMessageBus
                .HookMessageChannel<Item>(MessageChannelConstants.Ui, MessageEventConstants.SendMessage)
                .Subscribe(message =>
                {
                    Console.WriteLine(
                        $"[PUSHER] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - Message received: {message}");
                });

            recipient.ConnectAsync().Wait();
            
            var m4a1 = new Item(Guid.NewGuid(), "M4A1 Carbine");
            pusherMessageBus.AddMessage(MessageChannelConstants.Ui, MessageEventConstants.SendMessage, m4a1);
            Console.WriteLine($"Sent message");
            Console.ReadLine();
        }
    }
}