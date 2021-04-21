using System;
using System.Threading;
using System.Threading.Tasks;
using elastic_sample.domain;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nest;

namespace elastic_ingest
{

    public class StockWorkerReIndex : BackgroundService
    {
        private readonly ILogger<StockWorkerReIndex> _logger;
        private readonly IElasticClient  _elasticClient;
        //   private readonly StockDataReader _stockReader;
        private readonly IHostApplicationLifetime _hostApplication;

        public StockWorkerReIndex(ILogger<StockWorkerReIndex> logger, IElasticClient elasticClient, IHostApplicationLifetime hostApplication)
        {
            _logger = logger;
            _elasticClient = elasticClient;
            _hostApplication = hostApplication; 
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
          //for indexing, we check if the index already exist...
          var checkExist = await _elasticClient.Indices.ExistsAsync("stock-data-reindex",ct: stoppingToken);
          if(checkExist.Exists){
              await _elasticClient.Indices.DeleteAsync("stock-data-reindex", ct: stoppingToken);
          }

          //create and map new index to our class
          var newIndexResponse = await _elasticClient.Indices.CreateAsync("stock-data-reindex",
          c => c.Map( m => m.AutoMap<StockData>().
          Properties<StockData>(p => p.Keyword(k => k.Name(f => f.FoodName)))) //key allows only exact word match (case sensitive)
           ,ct: stoppingToken);

           if(newIndexResponse.IsValid){
               _logger.LogInformation("new index created");

               //reindex on the server by creating a new index from an exsiting index
               //reindexing is moving data from one index to another while also probably creating a new document schema
               var reindex = await _elasticClient.ReindexOnServerAsync(r => r.Source(s => s.Index("stock-data"))
               .Destination(d => d.Index("stock-data-reindex")).WaitForCompletion(false));

              //get taskid from reindxing operation
              var taskId = reindex.Task;
              //check response of the task
              var checkTask = await _elasticClient.Tasks.GetTaskAsync(taskId, ct: stoppingToken);

              //check if the task is completed and do somthing while at it
              while(!checkTask.Completed){
                  _logger.LogInformation("task still in progress, wait for 5 mins");
                  await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken: stoppingToken);
                  checkTask = await _elasticClient.Tasks.GetTaskAsync(taskId, ct: stoppingToken);
               }

              _logger.LogInformation("reindex complete");

              //atempt to also modify aliases by removing from any index and replacing with a new alias in another indexx
              await _elasticClient.Indices.BulkAliasAsync(a => a.Remove(alias => alias.Alias("stock-demo").
              Index("*")).Add(a => a.Alias("stock-demo").Index("stock-data-reindex")));

            _logger.LogInformation("alias complete");

           }

           _hostApplication.StopApplication();

        }
    }
}
