using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing; 
using CsvHelper;
using System.IO;
using System.Globalization;


namespace Swarm
{
    class Program
    {

        public class Sample {
            public double Value {get; set;}
        }

        public class FScore {
            public double Patch {get; set;}
            public double NotPatch {get; set;}
        }

            
        static void Main(string[] args)
        {
            void writeIntoCSV(ref List<Sample> data, string path){
                using (var writer = new StreamWriter(path)){
                    using (var csv = new CsvWriter(writer,CultureInfo.InvariantCulture))
                    {
                        csv.WriteRecords(data);
                        writer.Flush();
                    }
                }
            }

            double calculateError(double test, double value){
                return (Math.Abs(test-value))/value * 100;
            }

            double tickDataError(Field tick){
                var beeError = new List<double>();
                var errorList = new List<double>();
                foreach (var bee in tick.BeesList.ToList()){
                    if (bee.DataCollection.Count == 0){
                        beeError.Add(0);
                    }
                    else{
                        foreach(var data in bee.DataCollection){
                            //take into account the empty list, or data with 0 nectar count
                            var patch = tick.PatchList.FirstOrDefault(patch => patch.Location.Equals(data.Location));
                            if (!(patch is null)){
                                beeError.Add(calculateError(data.NectarCount, patch.NectarCount));
                            }
                            else {
                                if (data.NectarCount == 0){
                                    beeError.Add(0); //empty flower remains true
                                }
                                else {
                                    beeError.Add(100); //when flowerdata with nectar does not exist anymore
                                }
                            }
                        }
                    }
                    errorList.Add(beeError.Average());
                }
                return errorList.Average(); //total average of all bees error rate
            }

            void beeErrorAnalysis(ref List<List <Field>> data){
                var errorList = new List<List<Double>>();
                var AverageList = new List<Sample>();

                foreach(var run in data.ToList()){
                    var tickError = new List<Double>();
                    foreach (var tick in run.ToList()){
                        tickError.Add(tickDataError(tick));
                    }
                    errorList.Add(tickError);
                }

                for (var i= 0; i < Constant.TotalTicks; i++){
                    var avg = new Sample();
                    avg.Value = errorList.Average(innerList => innerList[i]);
                    AverageList.Add(avg);
                }
                writeIntoCSV(ref AverageList, "./test/errorPercentage.csv");
            }
            
            void honeyAnalysis(ref List<List <Field>> data){
                    var honeyAvgList  = new List<Sample>();
                for (var j = 0; j < Constant.TotalTicks; j++){
                    var avg = new Sample();
                    avg.Value = data.Average(innerList => innerList[j].Hive.HoneyCount);
                    honeyAvgList.Add(avg);
                }
            
                writeIntoCSV(ref honeyAvgList, "./test/honey-big.csv");

            }

            void exchangeCountAnalysis(ref List<List <Field>> data){
                var tickCounts = new List<int>();
                var averageData = new List<Sample>();

                for (var i = 0; i < Constant.TotalTicks; i++){
                    for (var j = 0; j < Constant.SimulationRuns; j++){
                        tickCounts.Add(data[j][i].BeesList.Where(bee => bee.Duty == Bee.State.exchange).Count());
                    }
                    averageData.Add(new Sample{ Value = tickCounts.Average()});
                    tickCounts = new List<int>();

                }
                writeIntoCSV(ref averageData, "./test/exchange.csv");
            }
            
            //calculates bees accuracy to detect patch in field
            Double getAccuracy(Field area, Bee bee){
                double accuracy = 0;
                var correctCount = 0;
                for (var i = 0; i <= Constant.SizeX; i++){
                    for (var j = 0; j <= Constant.SizeY; j++){
                        if (new Point(i,j) != area.Hive.Location){
                             var fieldPatch = area.PatchList.FirstOrDefault(patch => patch.Location.Equals(new Point(i,j)));
                             var dataPatch = bee.DataCollection.FirstOrDefault(data => data.Location.Equals(new Point(i,j)));
                            
                            if (fieldPatch is null && dataPatch is null)
                            {
                                correctCount++;
                            }
                            else if (fieldPatch is null && !(dataPatch is null) && dataPatch.NectarCount <= 0){
                                correctCount++;
                            }
                            else if (!(fieldPatch is null) && !(dataPatch is null) && dataPatch.NectarCount <= 0 && fieldPatch.NectarCount <= 0){
                                correctCount++;
                            }
                            else if (!(fieldPatch is null) && !(dataPatch is null) && fieldPatch.NectarCount > 0 && dataPatch.NectarCount > 0){
                                correctCount++;
                            }
                        }
                    }
                }
                accuracy = (double)correctCount/ (double)((Math.Pow((Constant.SizeX + 1),2)-1));
                return accuracy;
            }
            
            void accuracyAnalysis(ref List<List <Field>> data){
                var result = new List<Sample>();
                var accuracyList = new List<double>();
                var simulationValues = new List<double>();
                for (var i = 0; i < Constant.TotalTicks; i++){
                    for (var j = 0; j < Constant.SimulationRuns; j++){
                        foreach (var bee in data[j][i].BeesList.ToList()){
                            accuracyList.Add(getAccuracy(data[j][i], bee));
                        }
                        simulationValues.Add(accuracyList.Average());
                        accuracyList = new List<double>();
                    }
                    result.Add(new Sample{ Value = simulationValues.Average()});
                    simulationValues = new List<double>();
                }

                writeIntoCSV(ref result, "./test/accuracy-5.csv");
            }

            List<double> getFScores(Field area, List<FlowerData> dataCollection){
                /* Confusion matrices
                                Prediction
             T            | Patch  |  Not Patch
             R             -----------------
             U  Patch     |  TP   |   FN
             T  Not patch |  FP   |   TN          
             H  */
                
                /* Returns Nested List will be structured as 
                                precision   recall  f-score   
                    Patch            a          b       c      
                    Not Patch        e          f       g     
                */

                /* if all data is empty is classifed not patches then then
                 T        |  Not Patch
             R             -----------------
             U  Patch     |   FN
             T  Not patch |   TN          
             H  */
            
            double truePos = 0;
            double trueNeg = 0;
            double falsePos  = 0;
            double falseNeg = 0;


             for (var i = 0; i <= Constant.SizeX; i++){
                    for (var j = 0; j <= Constant.SizeY; j++){
                        if (new Point(i,j) != area.Hive.Location){
                             var fieldPatch = area.PatchList.FirstOrDefault(patch => patch.Location.Equals(new Point(i,j)));
                             var dataPatch = dataCollection.FirstOrDefault(data => data.Location.Equals(new Point(i,j)));
                             if (fieldPatch is null && dataPatch is null){
                                 trueNeg++;
                             }
                             else if (fieldPatch is not null && dataPatch is not null){
                                 if (fieldPatch.NectarCount > 0 && dataPatch.NectarCount > 0){
                                     truePos++;
                                 }
                                 else if (fieldPatch.NectarCount <= 0 && dataPatch.NectarCount <= 0){
                                     trueNeg++;
                                 }
                                 else if (fieldPatch.NectarCount <= 0 && dataPatch.NectarCount > 0){
                                     falsePos++;
                                 }
                                 else if (fieldPatch.NectarCount > 0 && dataPatch.NectarCount <= 0) {
                                     falseNeg++;
                                 }
                             }
                             else if (fieldPatch is not null && dataPatch is null){
                                 if (fieldPatch.NectarCount > 0){
                                     falseNeg++;
                                 }
                                 else {
                                     trueNeg++;
                                 }
                             }
                             else if (fieldPatch is null && dataPatch is not null){
                                 if (dataPatch.NectarCount > 0){
                                     falsePos++;
                                 }
                                 else {
                                     truePos++;
                                 }
                             }
                           
                        }
                    }
                }


            double patchFScore = truePos /(truePos+ 0.5*(falsePos + falseNeg));
            double notPatchFScore = trueNeg /(trueNeg + 0.5*(falseNeg+falsePos));

                return new List<double>{ patchFScore, notPatchFScore};
                //return patchFScore;
            }

            List<double> beeFscoreAvg(Field area){
                var avg = new List<double>();
                var items = new List<List <double>>();
                foreach (var bee in area.BeesList.ToList()){
                    var result = getFScores(area, bee.DataCollection);
                    items.Add(result);
                }
                avg.Add(items.Average(inner => inner[0]));
                avg.Add(items.Average(inner => inner[1]));
                return avg;
            }

            void FscoreAnalysis(ref List<List <Field>> data){
                var simulationData = new List<List <double>>();
                var tickAvg = new List<FScore>();
                for (var i = 0; i < Constant.TotalTicks; i++){
                    for (var j = 0; j < Constant.SimulationRuns; j++){
                        simulationData.Add(beeFscoreAvg(data[j][i]));
                    }
                    tickAvg.Add(new FScore{ Patch = simulationData.Average(inner => inner[0]), NotPatch = simulationData.Average(inner => inner[1]) });
                    simulationData = new List<List<double>>();
                }

                using (var writer = new StreamWriter("./test/fscore.csv")){
                    using (var csv = new CsvWriter(writer,CultureInfo.InvariantCulture))
                    {
                        csv.WriteRecords(tickAvg);
                        writer.Flush();
                    }
                }
            }
           

            void fieldNectarCount(ref List<List <Field>> data){
                var tickData = new List<int>();
                var averageData = new List<Sample>();

                for (var i = 0; i < Constant.TotalTicks; i++){
                    for (var j = 0; j < Constant.SimulationRuns; j++){
                        tickData.Add(data[j][i].PatchList.Sum(patch => patch.NectarCount));
                    }
                    averageData.Add(new Sample{ Value = tickData.Average()});
                    tickData = new List<int>();

                }
                writeIntoCSV(ref averageData, "./test/field-nectar-5.csv");
            }

            void beeDataCollectionComparison(ref List<List <Field>> data){
                var beeData = new List<double>();
                var tickData = new List<double>();
                var averageData = new List<Sample>();

                for (var i = 0; i < Constant.TotalTicks; i++){
                    for (var j = 0; j < Constant.SimulationRuns; j++){
                        foreach (var bee in data[j][i].BeesList.ToList()){
                            var knowledgePercentage = ((double)bee.DataCollection.Count / (double)data[j][i].PatchList.Count) * 100;
                            beeData.Add(knowledgePercentage);
                        }
                        tickData.Add(beeData.Average());
                        beeData = new List<double>();
                    }
                    averageData.Add(new Sample{ Value = tickData.Average()});
                    tickData = new List<double>();

                }
                writeIntoCSV(ref averageData, "./test/data-collection.csv");
            }
            
            void beeSimulation(Field area, ref List<Field> result){

                for (var tick = 0; tick < Constant.TotalTicks; tick++){  
                    result.Add(area.DeepCopy());
                    //Console.WriteLine("bees idle in #"+ tick + ": " + area.BeesList.Count(bee => bee.Duty.Equals(Bee.State.idle)));
                    //Console.WriteLine("bees exhange in #"+ tick + ": " + area.BeesList.Count(bee => bee.Duty.Equals(Bee.State.exchange)));
                    foreach(var bee in area.BeesList.ToList()){  
                        bee.StateMachine(area, tick);                     
                    }
                    area.UpdateField();
                }
                Console.WriteLine(area.Hive.HoneyCount);

            }

            void multipleRuns(ref List<List <Field>> data){
                var result = new List<Field>();
                var area = new Field();

                for (var i = 0; i < Constant.SimulationRuns; i++){

                    area = new Field();
                    area.InitialiseField();
                    result = new List<Field>();

                    beeSimulation(area, ref result);
                    data.Add(result);

                }
            }
            
            

            var data = new List<List <Field>>();
            multipleRuns(ref data);

            //accuracyAnalysis(ref data);
            //FscoreAnalysis(ref data);
            //fieldNectarCount(ref data);
            //honeyAnalysis(ref data);
            //exchangeCountAnalysis(ref data);
         
        }
    }
}