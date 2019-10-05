using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;

namespace CASCToolHost
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
            services.AddResponseCompression();
            services.AddMvc().AddNewtonsoftJson();
            services.AddCors(options =>
            {
                options.AddPolicy("AllowSpecificOrigin", builder => builder.WithOrigins("https://wow.tools", "https://bnet.marlam.in", "http://localhost:27631"));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                NGDP.LoadAllIndexes();
            }

            KeyService.LoadKeys();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseCors("AllowSpecificOrigin");
            app.UseResponseCompression();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });
        }
    }
}
