using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MessengerAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Hangfire;
using Hangfire.MemoryStorage;
using Swashbuckle.AspNetCore.Swagger;

namespace MessengerAPI
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
            
            services.AddMvc();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "My API", Version = "v1" });
                var filePath = System.IO.Path.Combine(Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationBasePath, "api.xml");
                c.IncludeXmlComments(filePath);
            });
            // services.AddTransient<ISingleMessengerService, SingleMessengerService>();
            services.AddSingleton<IMessengerServices, MessengerServices>();
            services.AddHangfire((c) => c.UseMemoryStorage());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseHangfireServer();
            app.UseMvc();
            app.UseSwagger();
            app.UseSwaggerUI(c => 
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });
            RecurringJob.AddOrUpdate<ReocurringCleaner>((c) => c.CleanMessengers(), Cron.MinuteInterval(3));
        }

        public class ReocurringCleaner
        {
            private IMessengerServices _messengerService;
            public ReocurringCleaner(IMessengerServices messengerService)
            {
                _messengerService = messengerService;
            }
            public void CleanMessengers(){
                _messengerService.Cleanup();
            }
        }
        
    }
}
