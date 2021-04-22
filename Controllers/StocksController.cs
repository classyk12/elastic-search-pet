using System.Linq;
using System.Text;
using System.Threading.Tasks;
using elastic_sample.domain;
using Microsoft.AspNetCore.Mvc;
using Nest;

namespace elastic_sample.Controllers
{
    [ApiController]
    [Route("[Controller]")]
    public class StocksController : ControllerBase
    {
        private readonly IElasticClient _client;

        public StocksController(IElasticClient client) => _client = client;
        // GET: /<controller>/

        [HttpGet("FoodNames")]
        public async Task<IActionResult> Get()
        {
             ISearchResponse<StockData> response = null;
            //using Fluent query
            response = await _client.SearchAsync<StockData>
            (s => s.Aggregations(a => a.Terms("foodnames", t => t.Field( f => f.FoodName)
            .Size(500))));


            //using Object initializer query
            //  var objectrequest= new SearchRequest<StockData>
            //  {
            //      Aggregations = new TermsAggregation("foodnames")
            //      {
            //          Field = Infer.Field<StockData>(f => f.FoodName),
            //          Size = 500
            //      }
            // };

            // response = await _client.SearchAsync<StockData>(objectrequest);


            if(!response.IsValid){
                return NotFound();
            } 

            //try to group by a key
            var fooods = response.Aggregations.Terms("foodnames").Buckets.Select(b => b.Key).ToList();

            //format response to display evrey new data on the next line
            if(!fooods.Any()){
                return NotFound();
            }

           return Content(string.Join(",\r\n", fooods));   
        }

         [HttpGet("FoodNames/{food}")]
        public async Task<IActionResult> Get(string food)
        {   
            var response = await _client.SearchAsync<StockData>
            (s => s.Query(q => q.ConstantScore(c => c.Filter(f => f.Term(t => t.Field(f => f.FoodName).Value(food)))))
            .Size(40).Sort(s => s.Descending(d => d.FoodName)));

            //Sort(s => s.Descending(d => d.Group))
            if(!response.IsValid){
                return NotFound();
            }

            //format document content
            var sb = new StringBuilder();
            foreach(var doc in response.Documents){
                sb.AppendLine($"{doc.FoodName} - {doc.Group} - {doc.ScientificName} - {doc.SubGroup}");
            }
            
            return Content(sb.ToString());



            
        }
    }
}
