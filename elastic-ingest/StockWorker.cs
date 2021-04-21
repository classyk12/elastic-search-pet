using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nest;

namespace elastic_ingest
{

    public class StockWorker : BackgroundService
    {
        private readonly ILogger<StockWorker> _logger;
        private readonly IElasticClient  _elasticClient;
        //   private readonly StockDataReader _stockReader;
        private readonly IHostApplicationLifetime _hostApplication;

        public StockWorker(ILogger<StockWorker> logger, IElasticClient elasticClient, IHostApplicationLifetime hostApplication)
        {
            _logger = logger;
            _elasticClient = elasticClient;
            _hostApplication = hostApplication; 
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
          var bulkSet = _elasticClient.BulkAll(DataReaderHelper.ReadDataSet(stoppingToken), 
          b => b.Index("stock-data").MaxDegreeOfParallelism(4)
          .Size(1000).BackOffRetries(2)
          .BackOffTime("30s"));

          var waitHandle = new CountdownEvent(1);
          ExceptionDispatchInfo exceptionInfo = null;
          var subscription = bulkSet.Subscribe(new BulkAllObserver(
            onNext: b => _logger.LogInformation("Data indexed"),
            onError: e => {
                exceptionInfo = ExceptionDispatchInfo.Capture(e);
                waitHandle.Signal();
            },
            onCompleted:() => waitHandle.Signal()
          ));

          waitHandle.Wait(TimeSpan.FromMinutes(30), stoppingToken);
          if(exceptionInfo != null && !(exceptionInfo.SourceException is OperationCanceledException)){
            exceptionInfo?.Throw();
          }

            //use aliases if you need to change the shape of the data or the way data is being indexed later
          await _elasticClient.Indices.PutAliasAsync("stock-demo-v1", "stock-demo", ct:stoppingToken);


          
        }
    }
}
