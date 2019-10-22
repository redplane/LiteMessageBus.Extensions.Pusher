namespace LiteMessageBus.Extensions.Pusher.Models
{
    public class PusherMessageContainer
    {
        #region Properties

        public string Channel { get; }
        
        public string Event { get; }
        
        public string Data { get; }
        
        #endregion
        
        #region Constructor

        public PusherMessageContainer(string channel, string @event, string data)
        {
            Channel = channel;
            Event = @event;
            Data = data;
        }

        #endregion
    }
}