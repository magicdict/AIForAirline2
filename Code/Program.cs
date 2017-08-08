using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AIForAirline
{
    class Program
    {
        static void Main(string[] args)
        {
            Utility.IsDebugMode = false;
            Utility.WriteLogToFile = false;
            Utility.PrintInfo = false;
            Utility.IsMac = false;
            if (Utility.IsMac)
            {
                Utility.Init("/Users/hu/AIForAirline2/SecondSeason/");
                Utility.XMAEvaluationDatasetFilename = "/Users/hu/Downloads/gitlab-u14641/data/baseline_result.csv";
            }
            else
            {
                Utility.Init("E:\\WorkSpace2017\\AIForAirline2\\SecondSeason\\");
                Utility.XMAEvaluationDatasetFilename = "E:\\WorkSpace2017\\AIForAirline\\External\\XMAEvaluation\\data\\baseline_result.csv";
            }
            Utility.RunAirline = "";
            Run(Utility.RunAirline);
            if (Utility.WriteLogToFile)
            {
                StreamWriter writer = new StreamWriter(Utility.LogPath + DateTime.Now.ToString("yyyyMMddHHmmss") + ".log");
                writer.Write(Utility.LogStringBuilder);
                writer.Close();
            }
        }

        public static void Run(string PlaneID = "")
        {
            Solution.AirlineDic.Clear();
            Solution.PlaneIdAirlineDic.Clear();
            Solution.CombinedVoyageList.Clear();
            Solution.FlyTimeDic.Clear();
            CheckCondition.AirPortProhibitList.Clear();
            CheckCondition.PlaneProhibitList.Clear();
            CheckCondition.TyphoonList.Clear();
            CheckCondition.TyphoonAirport.Clear();
            CheckCondition.TransTimeList.Clear();
            var timer = new Stopwatch();
            timer.Start();
            Utility.Log("Start Run AIForAirline...");
            //读取、整理数据
            Solution.ReadCSV();
            //分析数据
            Solution.Analyze();
            //恢复航班
            if (string.IsNullOrEmpty(PlaneID))
            {
                foreach (var planeid in Solution.PlaneIdAirlineDic.Keys)
                {
                    CoreAlgorithm.scoreDic.Add(planeid, Solution.FixAirline(Solution.PlaneIdAirlineDic[planeid], true).Score);
                }
                Console.WriteLine("当前温度：" + CoreAlgorithm._currentTemperature);
                do
                {
                    CoreAlgorithm.CheckExchangableAirline();
                    Solution.GetAirlineDicByPlaneId();
                    Console.WriteLine("当前温度：" + CoreAlgorithm._currentTemperature);
                } while (CoreAlgorithm._currentTemperature > 0);
                //TODO：签转顺序问题，不同顺序可能造成满座，结果不同
                Parallel.ForEach(Solution.PlaneIdAirlineDic.Keys, PlaneIdAirlineKey =>
                {
                    var PlaneAirlineList = Solution.PlaneIdAirlineDic[PlaneIdAirlineKey];
                    if (Solution.FixAirline(PlaneAirlineList).IsOK)
                    {
                        Solution.TransferGuest(PlaneAirlineList);
                    }
                    else
                    {
                        System.Console.WriteLine("无法修复的飞机号码：" + PlaneID);
                    }
                });
            }
            else
            {
                //针对航班测试用
                Utility.IsEvalute = false;
                var PlaneAirlineList = Solution.PlaneIdAirlineDic[PlaneID];
                if (Solution.FixAirline(PlaneAirlineList).IsOK)
                {
                    Solution.TransferGuest(PlaneAirlineList);
                }
                else
                {
                    System.Console.WriteLine("无法修复的飞机号码：" + PlaneID);
                }

            }
            Utility.Log("Finish!!");
            timer.Stop();
            //输出答案
            var score = Statistics.WriteResult(Solution.AirlineDic.Values.ToList(), true, (int)Math.Round(timer.Elapsed.TotalMinutes, 0));
            Utility.Log("Time Escape:" + timer.Elapsed.ToString());
            //输出Debug页面
            Statistics.WriteDebugInfo(Solution.AirlineDic.Values.ToList());
        }
    }
}
