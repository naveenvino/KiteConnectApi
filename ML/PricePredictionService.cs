using Microsoft.ML;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace KiteConnectApi.ML
{
    public class PricePredictionService
    {
        private readonly MLContext _mlContext;
        private ITransformer? _trainedModel;
        private readonly ILogger<PricePredictionService> _logger;

        public PricePredictionService(ILogger<PricePredictionService> logger)
        {
            _mlContext = new MLContext();
            _logger = logger;
        }

        public void TrainModel(IEnumerable<PriceData> trainingData)
        {
            _logger.LogInformation("Training ML.NET model...");

            IDataView dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

            var pipeline = _mlContext.Transforms.Concatenate("Features", "HistoricalPrice", "Feature1", "Feature2")
                .Append(_mlContext.Regression.Trainers.Sdca(labelColumnName: "OptimalPrice", featureColumnName: "Features"));

            _trainedModel = pipeline.Fit(dataView);

            _logger.LogInformation("ML.NET model training complete.");
        }

        public float PredictOptimalPrice(PriceData input)
        {
            if (_trainedModel == null)
            {
                _logger.LogWarning("ML.NET model not trained. Returning default value.");
                return 0.0f;
            }

            var predictionEngine = _mlContext.Model.CreatePredictionEngine<PriceData, PricePrediction>(_trainedModel);
            var prediction = predictionEngine.Predict(input);

            return prediction.PredictedOptimalPrice;
        }
    }
}