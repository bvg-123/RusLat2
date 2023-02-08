using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Vision;
using RusLat2.ML;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HWND = System.UInt32;

namespace RusLat2
{
  class Program
  {
    private static void Main (string[] args)
    {
      string mlDataDir = "ML.Data";
      if (Directory.Exists(mlDataDir)) Train(mlDataDir, "MLModel.zip");
      var input = new ModelInput();
      input.ImageSource = @"dump.png";
      int i = 0;
      while (i < 1)
      {
        Bitmap bitmap = GetLangBarImage();
        if (bitmap != null)
        {
          using (bitmap)
          {
            bitmap.Save("dump.png");
          }
          ModelOutput result = Model.Predict(input);
          string lang = null;
          if (result.Score[0] > 90) lang = "ENG";
            else if (result.Score[1] > 90) lang = "RUS";
          string s = $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} {lang}\r\n";
          File.AppendAllText("log.txt", s);
        }
        Thread.Sleep(1000);
        i++;
      }
    } // Main


    /// <summary>
    /// Возвращает dataset с классифицированными картинками для обучения. 
    /// </summary>
    /// <param name="mlDataDir">Корневой каталог с подкаталогами-метками, содержащими картинки для обучения.</param>
    private static IEnumerable<ModelInput> ReadTrainData (string mlDataDir)
    {
      List<ModelInput> list = new List<ModelInput>();
      var directories = Directory.EnumerateDirectories(mlDataDir);
      foreach (string dir in directories)
      {
        string label = dir.Split('\\').Last();
        foreach (string file in Directory.GetFiles(dir))
        {
          list.Add(new ModelInput()
          {
            ImageSource = file,
            Label = label
          });
        }
      }
      return list;
    } // ReadTrainData


    /// <summary>
    /// Обучает модель на данных из каталога mlDataDir при его наличии.
    /// </summary>
    private static double Train (string mlDataDir, string mlModelFile)
    {
      IEnumerable<ModelInput> trainingData = ReadTrainData(mlDataDir);
      MLContext mlContext = new MLContext(seed: 1);
      IEstimator<ITransformer> trainingPipeline = BuildTrainingPipeline(mlContext);
      IDataView trainingDataView = mlContext.Data.LoadFromEnumerable<ModelInput>(trainingData);
      ITransformer mlModel = trainingPipeline.Fit(trainingDataView);
      IDataView predictions = mlModel.Transform(trainingDataView);
      List<ModelOutput> imagePredictionData = mlContext.Data.CreateEnumerable<ModelOutput>(predictions, true).ToList();
      MulticlassClassificationMetrics metrics = mlContext.MulticlassClassification.Evaluate(predictions, labelColumnName: "Label", predictedLabelColumnName: "PredictedLabel");
      mlContext.Model.Save(mlModel, trainingDataView.Schema, mlModelFile);
      return metrics.LogLoss;
    } // Train


    private static IEstimator<ITransformer> BuildTrainingPipeline (MLContext mlContext)
    {
      var dataProcessPipeline = mlContext.Transforms.Conversion.MapValueToKey("Label", "Label")
                                .Append(mlContext.Transforms.LoadRawImageBytes("ImageSource_featurized", null, "ImageSource"))
                                .Append(mlContext.Transforms.CopyColumns("Features", "ImageSource_featurized"));
      var trainer = mlContext.MulticlassClassification.Trainers.ImageClassification(new ImageClassificationTrainer.Options() { LabelColumnName = "Label", FeatureColumnName = "Features" })
                                .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel", "PredictedLabel"));
      var trainingPipeline = dataProcessPipeline.Append(trainer);
      return trainingPipeline;
      /*
            IEstimator<ITransformer> pipeline = mlContext.Transforms.LoadImages(outputColumnName: "input", imageFolder: "", inputColumnName: nameof(ImageData.ImagePath))
                     .Append(mlContext.Transforms.ResizeImages(outputColumnName: "input", imageWidth: InceptionSettings.ImageWidth, imageHeight: InceptionSettings.ImageHeight, inputColumnName: "input"))
                     .Append(mlContext.Transforms.ExtractPixels(outputColumnName: "input", interleavePixelColors: InceptionSettings.ChannelsLast, offsetImage: InceptionSettings.Mean))
                     .Append(mlContext.Model.LoadTensorFlowModel(_inceptionTensorFlowModel).
                         ScoreTensorFlowModel(outputColumnNames: new[] { "softmax2_pre_activation" }, inputColumnNames: new[] { "input" }, addBatchDimensionInput: true))
                     .Append(mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "LabelKey", inputColumnName: "Label"))
                     .Append(mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy(labelColumnName: "LabelKey", featureColumnName: "softmax2_pre_activation"))
                     .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabelValue", "PredictedLabel"))
                     .AppendCacheCheckpoint(mlContext);
      */
    } // BuildTrainingPipeline




    /// <summary>
    /// Возвращает изображение текущего языка из Language Bar-а.
    /// Если Language Bar отсутствует, то возвращается null.
    /// </summary>
    private static Bitmap GetLangBarImage ()
    {
      Bitmap result = null;
      HWND langBarHwnd = 0;
      Native.User32.EnumerateWindows((parentHwnd, hWnd, className, caption) =>
      {
        bool @continue = true;
        if ((className == "CiceroUIWndFrame") && (caption == "TF_FloatingLangBar_WndTitle"))          // className == "TscShellContainerClass" - RDP-окно
        {
          langBarHwnd = hWnd;
          @continue = false;
        }
        return @continue;
      });
      if (langBarHwnd != 0)
      {
        result = Native.User32.PrintWindow(langBarHwnd);
      }
      return result;
    } // GetLangBarImage

  } // class Program

} // namespace RusLat2
