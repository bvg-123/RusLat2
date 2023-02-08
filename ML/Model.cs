// This file was auto-generated by ML.NET Model Builder. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.ML;

namespace RusLat2.ML
{
  public class Model
  {
    private static Lazy<PredictionEngine<ModelInput, ModelOutput>> PredictionEngine = new Lazy<PredictionEngine<ModelInput, ModelOutput>>(CreatePredictionEngine);

    public static ModelOutput Predict (ModelInput input)
    {
      ModelOutput result = PredictionEngine.Value.Predict(input);
      return result;
    } // Predict


    private static PredictionEngine<ModelInput, ModelOutput> CreatePredictionEngine ()
    {
      MLContext mlContext = new MLContext();
      ITransformer mlModel = mlContext.Model.Load("MLModel.zip", out var modelInputSchema);
      var predEngine = mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(mlModel);
      return predEngine;
    } // CreatePredictionEngine

  } // class Model

} // namespace RusLat2.ML