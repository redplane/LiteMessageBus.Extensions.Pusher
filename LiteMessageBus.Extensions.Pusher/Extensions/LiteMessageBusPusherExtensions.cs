using LiteMessageBus.Extensions.Pusher.Services;
using Microsoft.Extensions.DependencyInjection;
using PusherClient;

namespace LiteMessageBus.Extensions.Pusher.Extensions
{
    public static class LiteMessageBusPusherExtensions
    {
        #region Methods

        public static void AddPusherLiteMessageBus(this IServiceCollection services, string appKey,
            string appId,
            string appSecret,
            string cluster, IAuthorizer authorizer = null)
        {
            services.AddSingleton(new PusherLiteMessageBusService(appKey, appId,
                appSecret, cluster, authorizer));
        }

        #endregion
    }
}