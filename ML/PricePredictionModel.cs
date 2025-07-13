using Microsoft.ML.Data;

namespace KiteConnectApi.ML
{
    public class PriceData
    {
        [LoadColumn(0)]
        public float HistoricalPrice { get; set; }

        [LoadColumn(1)]
        public float Feature1 { get; set; }

        [LoadColumn(2)]
        public float Feature2 { get; set; }

        [LoadColumn(3)]
        [ColumnName("Label")]
        public float OptimalPrice { get; set; }
    }

    public class PricePrediction
    {
        [ColumnName("PredictedLabel")]
        public float PredictedOptimalPrice { get; set; }
    }
}