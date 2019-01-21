﻿using Microsoft.ML.Legacy;
using Microsoft.ML.Legacy.Models;
using Microsoft.ML.Legacy.Transforms;
using Microsoft.ML.Runtime.Data;
using Mvvm;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Storage;
using TextLoader = Microsoft.ML.Legacy.Data.TextLoader;

namespace XamlBrewer.Uwp.MachineLearningSample.Models
{
    internal class BinaryClassificationModel : ViewModelBase
    {
        private LocalEnvironment _mlContext = new LocalEnvironment(seed: null); // v0.6;

        public PredictionModel<BinaryClassificationData, BinaryClassificationPrediction> BuildAndTrain(string trainingDataPath, ILearningPipelineItem algorithm)
        {
            var pipeline = new LearningPipeline();
            pipeline.Add(new TextLoader(trainingDataPath).CreateFrom<BinaryClassificationData>(useHeader: true, separator: ';'));
            pipeline.Add(new MissingValueSubstitutor("FixedAcidity") { ReplacementKind = NAReplaceTransformReplacementKind.Mean });
            pipeline.Add(MakeNormalizer());
            pipeline.Add(new ColumnConcatenator("Features",
                                                 "FixedAcidity",
                                                 "VolatileAcidity",
                                                 "CitricAcid",
                                                 "ResidualSugar",
                                                 "Chlorides",
                                                 "FreeSulfurDioxide",
                                                 "TotalSulfurDioxide",
                                                 "Density",
                                                 "Ph",
                                                 "Sulphates",
                                                 "Alcohol"));
            pipeline.Add(algorithm);

            return pipeline.Train<BinaryClassificationData, BinaryClassificationPrediction>();
        }

        public void Save(PredictionModel model, string modelName)
        {
            var storageFolder = ApplicationData.Current.LocalFolder;
            using (var fs = new FileStream(Path.Combine(storageFolder.Path, modelName), FileMode.Create, FileAccess.Write, FileShare.Write))
                model.WriteAsync(fs);
        }

        public BinaryClassificationMetrics Evaluate(PredictionModel<BinaryClassificationData, BinaryClassificationPrediction> model, string testDataLocation)
        {
            var testData = new TextLoader(testDataLocation).CreateFrom<BinaryClassificationData>(useHeader: true, separator: ';');
            var metrics = new BinaryClassificationEvaluator().Evaluate(model, testData);
            return metrics;
        }

        public IEnumerable<BinaryClassificationPrediction> Predict(PredictionModel<BinaryClassificationData, BinaryClassificationPrediction> model, IEnumerable<BinaryClassificationData> data)
        {
            return model.Predict(data);
        }

        public IEnumerable<BinaryClassificationData> GetSample(string sampleDataPath)
        {
            return File.ReadAllLines(sampleDataPath)
               .Skip(1)
               .Select(x => x.Split(';'))
               .Select(x => new BinaryClassificationData
               {
                   FixedAcidity = float.Parse(x[0]),
                   VolatileAcidity = float.Parse(x[1]),
                   CitricAcid = float.Parse(x[2]),
                   ResidualSugar = float.Parse(x[3]),
                   Chlorides = float.Parse(x[4]),
                   FreeSulfurDioxide = float.Parse(x[5]),
                   TotalSulfurDioxide = float.Parse(x[6]),
                   Density = float.Parse(x[7]),
                   Ph = float.Parse(x[8]),
                   Sulphates = float.Parse(x[9]),
                   Alcohol = float.Parse(x[10]),
                   Label = float.Parse(x[11])
               });
        }

        private ILearningPipelineItem MakeNormalizer()
        {
            var normalizer = new BinNormalizer
            {
                NumBins = 2
            };
            normalizer.AddColumn("Label");

            return normalizer;
        }
    }
}
