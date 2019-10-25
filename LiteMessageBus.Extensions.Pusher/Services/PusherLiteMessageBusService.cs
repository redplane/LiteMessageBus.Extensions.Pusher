using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using LiteMessageBus.Extensions.Pusher.Models;
using LiteMessageBus.Models;
using LiteMessageBus.Services.Interfaces;
using Newtonsoft.Json.Linq;
using PusherClient;
using PusherServer;
using PusherOptions = PusherServer.PusherOptions;

namespace LiteMessageBus.Extensions.Pusher.Services
{
    public class PusherLiteMessageBusService : ILiteMessageBusService
    {
        #region Properties

        /// <summary>
        /// Instance for broadcasting messages.
        /// </summary>
        private readonly PusherServer.Pusher _broadcaster;

        /// <summary>
        /// Instance for receiving messages.
        /// </summary>
        private readonly PusherClient.Pusher _recipient;

        /// <summary>
        /// Whether recipient has connected or not.
        /// </summary>
        private bool _hasRecipientConnected;

        /// <summary>
        /// Chanel event manager.
        /// </summary>
        private readonly ConcurrentDictionary<MessageChannel, PusherMessageChannelOption>
            _channelManager;

        /// <summary>
        /// Channel initialization manager.
        /// </summary>
        private readonly ConcurrentDictionary<MessageChannel, ReplaySubject<AddedChannelEvent>>
            _channelInitializationManager;

        #endregion

        #region Constructor

        public PusherLiteMessageBusService(string appKey, string appId,
            string appSecret,
            string cluster, IAuthorizer authorizer = null)
        {
            _channelManager = new ConcurrentDictionary<MessageChannel, PusherMessageChannelOption>();
            _channelInitializationManager =
                new ConcurrentDictionary<MessageChannel, ReplaySubject<AddedChannelEvent>>();

            _broadcaster = new PusherServer.Pusher(appId, appKey,
                appSecret,
                new PusherOptions
                {
                    Cluster = cluster,
                    Encrypted = true
                });

            _recipient = new PusherClient.Pusher(appKey,
                new PusherClient.PusherOptions
                {
                    Cluster = cluster,
                    Authorizer = authorizer
                });

            _recipient.Connected += OnRecipientConnected;
            _recipient.Disconnected += OnRecipientDisconnected;
        }

        #endregion

        #region Methods

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="eventName"></param>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="NotImplementedException"></exception>
        public virtual void AddMessageChannel<T>(string channelName, string eventName)
        {
            // In hosting, message channel will be created.
            // Nothing to do here.
            LoadMessageChannel(channelName, eventName, true);
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="eventName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public virtual IObservable<T> HookMessageChannel<T>(string channelName, string eventName)
        {
            // Recipient does not connect.
            if (!_hasRecipientConnected)
                _recipient.ConnectAsync().Wait();

            return HookChannelInitialization(channelName, eventName)
                .Select(x =>
                {
                    return LoadMessageChannel(channelName, eventName, false)
                        ?.InternalBroadcaster
                        .Where(messageContainer => messageContainer != null && messageContainer.Available)
                        .Select(messageContainer =>
                        {
                            var root = messageContainer.Data;

                            // Root is json object.
                            if (root is JObject jObject)
                                return jObject.ToObject<T>();

                            // Root is already in type of T.
                            if (root is T data)
                                return data;

                            return default(T);
                        });
                })
                .Switch();
        }

        /// <summary>
        /// Add message to pusher message bus.
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="eventName"></param>
        /// <param name="data"></param>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="NotImplementedException"></exception>
        public virtual void AddMessage<T>(string channelName, string eventName, T data)
        {
            var channelMessageEmitter = LoadMessageChannel(channelName, eventName, true);
            channelMessageEmitter?.SendExternalMessage<T>(data);
        }

        /// <summary>
        /// Delete message from a pusher
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="eventName"></param>
        /// <exception cref="NotImplementedException"></exception>
        public virtual void DeleteMessage(string channelName, string eventName)
        {
            var channelMessageEmitter = LoadMessageChannel(channelName, eventName, false);
            channelMessageEmitter?.DeleteMessage();
        }

        /// <summary>
        /// Delete message from every channel in pusher server.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public virtual void DeleteMessages()
        {
            var keys = _channelManager.Keys;
            var broadCastedEvents = new LinkedList<Event>();
            var channelMessageEmitters = new LinkedList<PusherMessageChannelOption>();

            foreach (var key in keys)
            {
                if (!_channelManager.TryGetValue(key, out var channelMessageEmitter))
                    continue;

                // Empty message container.
                var messageContainer = new MessageContainer<object>(null, false);

                // Build event that needs passing to pusher server.
                var broadCastedEvent = new Event();
                broadCastedEvent.Channel = key.Name;
                broadCastedEvent.EventName = key.EventName;
                broadCastedEvent.Data = messageContainer;
                broadCastedEvents.AddLast(broadCastedEvent);

                channelMessageEmitters.AddLast(channelMessageEmitter);
            }

            // Trigger to every channel.
            _broadcaster.TriggerAsync(broadCastedEvents.ToArray());

            // Clear the local channel.
            foreach (var channelMessageEmitter in channelMessageEmitters)
                channelMessageEmitter?.DeleteMessage();
        }

        #endregion

        #region Event handlers & inner methods

        /// <summary>
        /// Hook to channel initialization.
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="eventName"></param>
        /// <returns></returns>
        protected virtual IObservable<AddedChannelEvent> HookChannelInitialization(string channelName, string eventName)
        {
            var channelInitializationEventEmitter = _channelInitializationManager
                .GetOrAdd(new MessageChannel(channelName, eventName), new ReplaySubject<AddedChannelEvent>());
            return channelInitializationEventEmitter;
        }

        /// <summary>
        /// Load message channel using channel name and event name.
        /// Specifying auto create will trigger channel creation if it is not available.
        /// Auto create option can cause concurrent issue, such as parent channel can be replaced by child component.
        /// Therefore, it should be used wisely.
        /// </summary>
        protected virtual PusherMessageChannelOption LoadMessageChannel(string channelName, string eventName,
            bool autoCreate = false)
        {
            // Initialize a message channel key.
            var messageChannel = new MessageChannel(channelName, eventName);

            // Message channel has been added before.
            if (_channelManager.TryGetValue(messageChannel, out var messageChannelOption))
                return messageChannelOption;

            // Raise an event about message channel creation if it has been newly added.
            if (!_channelInitializationManager.TryGetValue(new MessageChannel(channelName, eventName),
                out var channelInitializationEventEmitter))
            {
                channelInitializationEventEmitter = new ReplaySubject<AddedChannelEvent>(1);
                _channelInitializationManager.TryAdd(new MessageChannel(channelName, eventName),
                    channelInitializationEventEmitter);
            }

            // Whether channel should be created automatically.
            if (!autoCreate)
                return null;

            // Initialize pusher channel subscription.
            var recipientChannel = _recipient.SubscribeAsync(channelName)
                .Result;

            // Initialize message channel option.
            messageChannelOption =
                new PusherMessageChannelOption(eventName, recipientChannel, _broadcaster);

            // Fail to add new message channel.
            if (!_channelManager.TryAdd(messageChannel, messageChannelOption))
                return null;


            channelInitializationEventEmitter.OnNext(new AddedChannelEvent(channelName, eventName));
            return messageChannelOption;
        }

        /// <summary>
        /// Called when recipient connected.
        /// </summary>
        /// <param name="sender"></param>
        protected virtual void OnRecipientConnected(object sender)
        {
            _hasRecipientConnected = true;
        }

        /// <summary>
        /// Called when recipient disconnected.
        /// </summary>
        /// <param name="sender"></param>
        protected virtual void OnRecipientDisconnected(object sender)
        {
            _hasRecipientConnected = false;
        }

        #endregion
    }
}