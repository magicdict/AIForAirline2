using System;
using System.Collections.Generic;
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
            Utility.IsEvalute = false;
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
            Solution.PlaneTypeSeatCntDic.Clear();
            Solution.TransTimeList.Clear();
            CheckCondition.AirPortProhibitList.Clear();
            CheckCondition.PlaneProhibitList.Clear();
            CheckCondition.TyphoonList.Clear();
            CheckCondition.TyphoonAirport.Clear();
            var timer = new Stopwatch();
            timer.Start();
            Utility.Log("Start Run AIForAirline...");
            //读取、整理数据
            Solution.ReadCSV();
            //分析数据
            if (false) Solution.Analyze();
            //恢复航班
            if (string.IsNullOrEmpty(PlaneID))
            {
                if (false)
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
                }

                //恢复航班
                Utility.IsUseTyphoonStayRoom = false;
                var UnFix = new List<string>();
                Parallel.ForEach(Solution.PlaneIdAirlineDic.Keys, PlaneIdAirlineKey =>
                {
                    var PlaneAirlineList = Solution.PlaneIdAirlineDic[PlaneIdAirlineKey];
                    if (!Solution.FixAirline(PlaneAirlineList).IsOK)
                    {
                        UnFix.Add(PlaneIdAirlineKey);
                        System.Console.WriteLine("无法修复的飞机号码：" + PlaneAirlineList[0].ModifiedPlaneID);
                    }
                });
                System.Console.WriteLine("启用停机库");
                Utility.IsUseTyphoonStayRoom = true;
                foreach (var PlaneIdAirlineKey in UnFix)
                {
                    var PlaneAirlineList = Solution.PlaneIdAirlineDic[PlaneIdAirlineKey];
                    if (!Solution.FixAirline(PlaneAirlineList).IsOK)
                    {
                        System.Console.WriteLine("无法修复的飞机号码：" + PlaneAirlineList[0].ModifiedPlaneID);
                    }
                    else
                    {
                        System.Console.WriteLine("修复飞机号码：" + PlaneAirlineList[0].ModifiedPlaneID);
                    }
                }

                //签转操作无法多线程
                foreach (var PlaneIdAirlineKey in Solution.PlaneIdAirlineDic.Keys)
                {
                    var PlaneAirlineList = Solution.PlaneIdAirlineDic[PlaneIdAirlineKey];
                    Solution.EndorseGuest(PlaneAirlineList);
                }
                Solution.EndorseTransferGuest();
            }
            else
            {
                //针对航班测试用
                Utility.IsEvalute = false;
                Utility.IsUseTyphoonStayRoom = false;
                var PlaneAirlineList = Solution.PlaneIdAirlineDic[PlaneID];
                if (Solution.FixAirline(PlaneAirlineList).IsOK)
                {
                    Solution.EndorseGuest(PlaneAirlineList);
                    Solution.EndorseTransferGuest();
                }
                else
                {
                    Utility.IsUseTyphoonStayRoom = true;
                    if (!Solution.FixAirline(PlaneAirlineList).IsOK)
                    {
                        System.Console.WriteLine("无法修复的飞机号码：" + PlaneID);
                    }
                }
            }
            if (false) ResultOptimize.Run();
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
