using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using CsvHelper;
using elastic_sample.domain;

namespace elastic_ingest
{
    public class DataReaderHelper
    {
        public static IEnumerable<StockData> ReadDataSet(CancellationToken token)
        {
            var filePath = @"./dataset/generic-food.csv";
            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var stockDatas = csv.GetRecords<StockData>().ToList();
                return stockDatas;
            }
        }
    }
}
