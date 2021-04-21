using elastic_sample.domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nest;

namespace elastic_ingest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    //services.AddHostedService<StockWorker>();
                    services.AddHostedService<StockWorkerReIndex>();
                    //services.AddHostedService<IElasticIngestWorker>(); //register your worker service
                    //register elastic search
                    services.AddSingleton<IElasticClient>(e =>
                    {
                        var config = e.GetRequiredService<IConfiguration>(); //get app configuration
                        string cloudId = config["UserSecrets:CloudId"];

                        var settings = new ConnectionSettings(cloudId: cloudId, new Elasticsearch.Net.BasicAuthenticationCredentials(
                            "elastic", config["UserSecrets:Code"]
                            )).DefaultIndex("example-index").DefaultMappingFor<StockData>(i => i.IndexName("stock-demo-v1"));

                        return new ElasticClient(settings);

                    });
                });

    }
}
