using System;
using System.Reactive.Subjects;
using LiteMessageBus.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LiteMessageBus.Extensions.Pusher.Models
{
    public class PusherMessageChannelOption
    {
        #region Properties

        /// <summary>
        /// Instance for broadcasting message inside application.
        /// </summary>
        public ReplaySubject<MessageContainer<object>> InternalBroadcaster { get; }

        /// <summary>
        /// Instance represent recipient channel.
        /// </summary>
        private readonly PusherClient.Channel _recipient;

        /// <summary>
        /// Instance for broadcasting messages to pusher server.
        /// </summary>
        private readonly PusherServer.Pusher _externalBroadcaster;

        /// <summary>
        /// Event which message container manages.
        /// </summary>
        private readonly string _eventName;
        
        #endregion

        #region Constructor

        public PusherMessageChannelOption(string eventName, PusherClient.Channel recipientChannel, PusherServer.Pusher externalBroadcaster)
        {
            if (string.IsNullOrEmpty(eventName))
                throw new ArgumentException($"{nameof(eventName)} is required.");

            _eventName = eventName;
            
            InternalBroadcaster = new ReplaySubject<MessageContainer<object>>();
            _externalBroadcaster = externalBroadcaster;
            _recipient = recipientChannel;
            
            recipientChannel.Bind(eventName, OnEventMessageReceived);
        }

        #endregion
        
        #region Methods

        /// <summary>
        /// Delete the broad casted message.
        /// </summary>
        public virtual void DeleteMessage()
        {
            InternalBroadcaster?.OnNext(new MessageContainer<object>(null, false));
        }

        /// <summary>
        /// Send message to external message bus service.
        /// </summary>
        /// <param name="data"></param>
        /// <typeparam name="T"></typeparam>
        public virtual void SendExternalMessage<T>(T data)
        {
            var messageContainer = new MessageContainer<T>(data, true);
            _externalBroadcaster.TriggerAsync(_recipient.Name, _eventName, messageContainer)
                .Wait();
        }
        
        #endregion
        
        #region Event handlers

        /// <summary>
        /// Called when a message container was broad casted from pusher message bus.
        /// </summary>
        /// <param name="root"></param>
        protected virtual void OnEventMessageReceived(dynamic root)
        {
            MessageContainer<object> messageContainer;
            if (root is JObject jObject)
                messageContainer = jObject.ToObject<MessageContainer<object>>();
            else
                messageContainer = JsonConvert.DeserializeObject<MessageContainer<object>>(root);
            
            InternalBroadcaster.OnNext(messageContainer);
        }
        
        #endregion
    }
}