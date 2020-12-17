using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NWebDav.Server;
using NWebDav.Server.AspNetCore;
using WebRaid.UI.Adapters;
using WebRaid.Node.Lokal;

namespace WebRaid.UI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IRequestHandlerFactory, RequestHandlerFactory>();
            services.AddSingleton<IWebDavDispatcher, WebDavDispatcher>();

            services.AddWebRaidLokalNodes(Configuration);
        }

        // ReSharper disable once UnusedMember.Global, This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebDavDispatcher webDavDispatcher, ILoggerFactory loggerFactory)
        {
            NWebDav.Server.Logging.LoggerFactory.Factory = new NWebDavLogging(loggerFactory);
            app.Run(async context =>
            {
                // Create the proper HTTP context
                var httpContext = new AspNetCoreContext(context);

                // Dispatch request
                await webDavDispatcher.DispatchRequestAsync(httpContext)
                    .ConfigureAwait(false);
            });
        }
    }
}
