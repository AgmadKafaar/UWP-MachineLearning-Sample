﻿using Microsoft.Data.DataView;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
using Mvvm;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Storage;

namespace XamlBrewer.Uwp.MachineLearningSample.Models
{
    internal class RegressionModel : ViewModelBase
    {
        private MLContext _mlContext = new MLContext(seed: null); // v0.6;

        private IDataView trainingData;

        private PredictionEngine<RegressionData, RegressionPrediction> predictionEngine;

        public ITransformer Model { get; private set; }

        public IEnumerable<RegressionData> Load(string trainingDataPath)
        {
            var readerOptions = new TextLoader.Options()
            {
                Separators = new[] { ',' },
                HasHeader = true,
                Columns = new[]
                    {
                        new TextLoader.Column("Label", DataKind.Single, 1),
                        new TextLoader.Column("NBA_DraftNumber", DataKind.Single, 3),
                        new TextLoader.Column("Age", DataKind.Single, 4),
                        new TextLoader.Column("Ws", DataKind.Single, 22),
                        new TextLoader.Column("Bmp", DataKind.Single, 26)
                    }
            };

            trainingData = _mlContext.Data.LoadFromTextFile(trainingDataPath, readerOptions);

            return _mlContext.Data.CreateEnumerable<RegressionData>(trainingData, reuseRowObject: false);
        }

        public void BuildAndTrain()
        {
            //var t = new FastTreeRegressionTrainer(_mlContext, "Label", "Features"); // PlatformNotSupportedException

            var pipeline = _mlContext.Transforms.ReplaceMissingValues("Age", "Age", MissingValueReplacingEstimator.ColumnOptions.ReplacementMode.Mean)
                .Append(_mlContext.Transforms.ReplaceMissingValues("Ws", "Ws", MissingValueReplacingEstimator.ColumnOptions.ReplacementMode.Mean))
                .Append(_mlContext.Transforms.ReplaceMissingValues("Bmp", "Bmp", MissingValueReplacingEstimator.ColumnOptions.ReplacementMode.Mean))
                .Append(_mlContext.Transforms.ReplaceMissingValues("NBA_DraftNumber", "NBA_DraftNumber", MissingValueReplacingEstimator.ColumnOptions.ReplacementMode.Mean))
                .Append(_mlContext.Transforms.Concatenate(
                    "Features",
                    new[] { "NBA_DraftNumber", "Age", "Ws", "Bmp" }))
                .Append(_mlContext.Regression.Trainers.GeneralizedAdditiveModels("Label", "Features"));

            Model = pipeline.Fit(trainingData);

            predictionEngine = Model.CreatePredictionEngine<RegressionData, RegressionPrediction>(_mlContext);
        }

        public void Save(string modelName)
        {
            var storageFolder = ApplicationData.Current.LocalFolder;
            using (var fs = new FileStream(
                    Path.Combine(storageFolder.Path, modelName),
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.Write))
            {
                Model.SaveTo(_mlContext, fs);
            }
        }

        public IEnumerable<RegressionPrediction> PredictTrainingData()
        {
            var res = Model.Transform(trainingData);
            return _mlContext.Data.CreateEnumerable<RegressionPrediction>(res, reuseRowObject: false);
        }

        public RegressionPrediction Predict(RegressionData data)
        {
            return predictionEngine.Predict(data);
        }
    }
}
