using elastic_sample.domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nest;

namespace elastic_sample
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
            services.AddControllers();
                services.AddSingleton<IElasticClient>(e =>
                    {
                        var config = e.GetRequiredService<IConfiguration>(); //get app configuration
                        string cloudId = config["UserSecrets:CloudId"];

                        var settings = new ConnectionSettings(cloudId: cloudId, new Elasticsearch.Net.BasicAuthenticationCredentials(
                            "elastic", config["UserSecrets:Code"]
                            )).DefaultIndex("example-index").DefaultMappingFor<StockData>(i => i.IndexName("stock-data-reindex"));

                        return new ElasticClient(settings);

                    });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
