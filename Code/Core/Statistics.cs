using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace AIForAirline
{
    public class Statistics
    {

        //参数1=5000，参数2=1000，参数3=1000，参数4=750，参数5=100，参数6=150.
        public const int EmptyFlyParm = 5000;
        public const int CancelAirlineParm = 1000;
        public const int PlaneTypeChangeAirlineParm = 500;
        public const int ChangeToDirectArilineParm = 750;
        public const int TotalDelayParm = 100;
        public const int TotalEarlyParm = 150;
        //调机表
        public static double[] PlaneTypeChangeParmTable = new double[] { 0, 0, 2, 4,
                                                                         0.5, 0, 2, 4,
                                                                         1.5, 1.5, 0, 2,
                                                                         1.5, 1.5, 2, 0 };
        //5月6日16点
        public static DateTime TyphoonStartTime = new DateTime(2017, 5, 6, 16, 0, 0);
        public const int CancelPerGuestParm = 4;

        public double GetDelayPerGuestParm(TimeSpan DelayTime)
        {
            if (DelayTime.TotalHours <= 2) return 1;
            if (DelayTime.TotalHours <= 4) return 1.5;
            if (DelayTime.TotalHours <= 8) return 2;
            if (DelayTime.TotalHours <= 36) return 3;
            return CancelPerGuestParm;
        }

        public double GetTransDelayPerGuestParm(TimeSpan DelayTime)
        {
            if (DelayTime.TotalHours < 6) return 0;
            if (DelayTime.TotalHours < 24) return 0.5;
            if (DelayTime.TotalHours <= 48) return 1;
            return CancelPerGuestParm;
        }

        //调机航班数(包含重要度因素)
        public double EmptyFlyAirlineCnt;
        //取消航班数(包含重要度因素)
        public double CancelAirlineCnt;
        //机型发生变化的航班数(包含重要度因素)
        public double PlaneTypeChangeAirlineCnt;

        public const int BeforeTyphoonChangeParm = 15;

        public const int AfterTyphoonChangeParm = 5;

        public int BeforeTyphoonChangeCnt;

        public int AfterTyphoonChangeCnt;

        //联程拉直航班对的个数(包含重要度因素)
        public double ChangeToDirectArilineCnt;
        //航班总延误时间（小时）(包含重要度因素)
        public double TotalDelayHours;
        //航班总提前时间（小时）(包含重要度因素)      
        public double TotalEarlyHours;

        public static double WriteResult(List<Airline> airlineList, bool isOutput = false, int TimeUsageMinutes = 0)
        {
            var writer = isOutput ? new StreamWriter(Utility.ResultPath + "Temp.csv") : null;
            var result = new Statistics();
            //统计所有航班的处理情况
            //if (isOutput) { airlineList.Sort((x, y) => { return x.ID.CompareTo(y.ID); }); }
            var outputline = new string[11];
            foreach (var airline in airlineList)
            {
                if (!string.IsNullOrEmpty(Utility.RunAirline))
                {
                    if (airline.Problem == null && airline.FixMethod == enmFixMethod.UnFixed) continue;
                }
                var problem = airline.Problem;
                outputline[0] = airline.ID;
                outputline[1] = airline.StartAirPort;
                outputline[2] = airline.EndAirPort;
                outputline[3] = airline.ModifiedStartTime.ToString(Utility.FullDateFormat);
                outputline[4] = airline.ModifiedEndTime.ToString(Utility.FullDateFormat);
                outputline[5] = airline.ModifiedPlaneID;
                outputline[6] = "0"; //是否取消
                outputline[7] = "0"; //是否拉直
                outputline[8] = "0"; //是否调机
                outputline[9] = "0"; //是否签转
                switch (airline.FixMethod)
                {
                    case enmFixMethod.Cancel:
                        //直接取消
                        result.CancelAirlineCnt += airline.ImportFac;
                        outputline[6] = "1"; //是否取消
                        break;
                    case enmFixMethod.Direct:
                        var combinedAirline = airline.ComboAirline;
                        //第一段：
                        outputline[1] = combinedAirline.StartAirPort;
                        outputline[2] = combinedAirline.EndAirPort;
                        //Fix阶段，联程拉直之后，使用DirectAirLine进行再处理的，所以，这里必须使用DirectAirLine
                        outputline[3] = combinedAirline.DirectAirLine.ModifiedStartTime.ToString(Utility.FullDateFormat);
                        outputline[4] = combinedAirline.DirectAirLine.ModifiedEndTime.ToString(Utility.FullDateFormat);
                        outputline[5] = combinedAirline.DirectAirLine.ModifiedPlaneID;
                        outputline[6] = "0"; //是否取消
                        outputline[7] = "1"; //是否拉直
                        //联程拉直取消的第二段航班不算惩罚的
                        result.ChangeToDirectArilineCnt += airline.ComboAirline.ImportFac;
                        break;
                    case enmFixMethod.CancelByDirect:
                        //使用原数据填充
                        outputline[6] = "1"; //是否取消
                        outputline[7] = "1"; //是否拉直
                        break;
                    case enmFixMethod.Transfer:
                        outputline[9] = "0"; //是否签转
                        outputline[10] = string.Empty;
                        break;
                    default:
                        break;
                }
                if (airline.IsChangeStartTime && (airline.FixMethod == enmFixMethod.UnFixed || airline.FixMethod == enmFixMethod.Direct))
                {
                    //联程拉直后的航班，统计拉直的时候是两者系数之和；统计提前，延误，换机按照第一个航班来算
                    var Diff = airline.ModifiedStartTime.Subtract(airline.StartTime).TotalHours;
                    if (Diff > 0)
                    {
                        //延迟
                        result.TotalDelayHours += (Diff * airline.ImportFac);
                    }
                    else
                    {
                        //提早(Diff是负数，这里用减法)
                        result.TotalEarlyHours -= (Diff * airline.ImportFac);
                    }
                }
                //机型变换对应
                if (Solution.PlaneTypeSearchDic[airline.ModifiedPlaneID] !=
                    Solution.PlaneTypeSearchDic[airline.PlaneID])
                {
                    int index = (int.Parse(airline.PlaneID) - 1) * 4 + int.Parse(airline.ModifiedPlaneID) - 1;
                    result.PlaneTypeChangeAirlineCnt += PlaneTypeChangeParmTable[index];
                    if (airline.StartTime <= TyphoonStartTime)
                    {
                        result.BeforeTyphoonChangeCnt++;
                    }
                    else
                    {
                        result.AfterTyphoonChangeCnt++;
                    }
                }
                if (isOutput) writer.WriteLine(string.Join(",", outputline));
            }
            if (isOutput)
            {
                int EmptyFlyCnt = 9001;
                foreach (var airline in CoreAlgorithm.EmptyFlyList)
                {
                    airline.ID = EmptyFlyCnt.ToString();
                    EmptyFlyCnt++;
                    outputline[0] = airline.ID;
                    outputline[1] = airline.StartAirPort;
                    outputline[2] = airline.EndAirPort;
                    outputline[3] = airline.ModifiedStartTime.ToString(Utility.FullDateFormat);
                    outputline[4] = airline.ModifiedEndTime.ToString(Utility.FullDateFormat);
                    outputline[5] = airline.ModifiedPlaneID;
                    outputline[6] = "0"; //是否取消
                    outputline[7] = "0"; //是否拉直
                    outputline[8] = "1"; //是否调机
                    writer.WriteLine(string.Join(",", outputline));
                    result.EmptyFlyAirlineCnt++;
                }
                writer.Close();
            }
            var score = result.GetTarget();
            if (isOutput)
            {
                string filename = Utility.ResultPath + Utility.UserId + "_" + Math.Round(score, 3) + "_" + TimeUsageMinutes + ".csv";
                if (File.Exists(filename)) File.Delete(filename);
                File.Move(Utility.WorkSpaceRoot + "Result" + Path.DirectorySeparatorChar + "Temp.csv", filename);
                Utility.Log("分数：" + Math.Round(score, 3));
                Utility.Log("保存文件：" + filename);
                if (File.Exists(Utility.XMAEvaluationFilename) && File.Exists(Utility.DataSetXLSFilename) && Utility.IsEvalute)
                {
                    System.Diagnostics.Process clientProcess = new Process();
                    clientProcess.StartInfo.FileName = "java";
                    clientProcess.StartInfo.Arguments = @"-jar " + Utility.XMAEvaluationFilename + " " + Utility.DataSetXLSFilename + " " + filename;
                    clientProcess.Start();
                    clientProcess.WaitForExit();
                }
                if (File.Exists(Utility.XMAEvaluationDatasetFilename)) File.Delete(Utility.XMAEvaluationDatasetFilename);
                File.Copy(filename, Utility.XMAEvaluationDatasetFilename);
            }
            return score;
        }
        //目标函数        
        public Double GetTarget()
        {
            //目标函数值 = 参数1*调机航班数 + 
            //            参数2*取消航班数 + 
            //            参数3*机型发生变化的航班数 +
            //            参数4*联程拉直航班对的个数 +
            //            参数5*航班总延误时间（小时） +
            //            参数6*航班总提前时间（小时）
            Double Score = 0;
            Score += EmptyFlyAirlineCnt * EmptyFlyParm;
            Score += CancelAirlineCnt * CancelAirlineParm;
            Score += PlaneTypeChangeAirlineCnt * PlaneTypeChangeAirlineParm;
            Score += BeforeTyphoonChangeCnt * BeforeTyphoonChangeParm;
            Score += AfterTyphoonChangeCnt * AfterTyphoonChangeParm;
            Score += ChangeToDirectArilineCnt * ChangeToDirectArilineParm;
            Score += TotalDelayHours * TotalDelayParm;
            Score += TotalEarlyHours * TotalEarlyParm;
            return Score;
        }

        internal static void WriteDebugInfo(List<Airline> airlineList)
        {
            airlineList.AddRange(CoreAlgorithm.EmptyFlyList);
            airlineList.Sort((x, y) =>
            {
                if (int.Parse(x.ModifiedPlaneID).ToString("D3").Equals(int.Parse(y.ModifiedPlaneID).ToString("D3")))
                {
                    return x.StartTime.CompareTo(y.StartTime);
                }
                else
                {
                    return int.Parse(x.ModifiedPlaneID).ToString("D3").CompareTo(int.Parse(y.ModifiedPlaneID).ToString("D3"));
                }
            });
            string filename = Utility.WorkSpaceRoot + "Result" + Path.DirectorySeparatorChar + "Debug.csv";
            var writer = new StreamWriter(filename);
            var outputline = new string[19];
            outputline[0] = "AirLineID";
            outputline[1] = "StartAirPort";
            outputline[2] = "EndAirPort";
            outputline[3] = "StartTime";
            outputline[4] = "EndTime";
            outputline[5] = "ModifiedStartTime";
            outputline[6] = "ModifiedEndTime";
            outputline[7] = "ModifiedPlaneID";
            outputline[8] = "IsCancel";
            outputline[9] = "IsDirect";
            outputline[10] = "IsEmptyFly";
            outputline[11] = "PlanStayTime";
            outputline[12] = "ActureStayTime";
            outputline[13] = "StartTimeDiff";
            outputline[14] = "AirlineNo";
            outputline[15] = "InterKbn";
            outputline[16] = "Important";
            outputline[17] = "PlaneID";
            outputline[18] = "DiffScore";
            writer.WriteLine(string.Join(",", outputline));
            foreach (var airline in airlineList)
            {
                outputline[0] = airline.ID;
                outputline[1] = airline.StartAirPort;
                outputline[2] = airline.EndAirPort;
                outputline[3] = airline.StartTime.ToString();
                outputline[4] = airline.EndTime.ToString();
                outputline[5] = airline.ModifiedStartTime.ToString();
                outputline[6] = airline.ModifiedEndTime.ToString();
                outputline[7] = airline.ModifiedPlaneID;
                outputline[8] = "0"; //是否取消
                outputline[9] = "0"; //是否拉直
                outputline[10] = "0"; //是否调机
                outputline[14] = "";  //联程
                outputline[15] = airline.InterKbn;  //国际国内
                outputline[16] = airline.ImportFac.ToString();
                outputline[17] = airline.PlaneID;
                var Diff = airline.ModifiedStartTime.Subtract(airline.StartTime).TotalHours;
                if (Diff > 0)
                {
                    //延迟
                    outputline[18] = Math.Round((Diff * airline.ImportFac * TotalDelayParm), 0).ToString();
                }
                else
                {
                    //提早(Diff是负数，这里用减法)
                    outputline[18] = Math.Round((-Diff * airline.ImportFac * TotalEarlyParm), 0).ToString();
                }

                switch (airline.FixMethod)
                {
                    case enmFixMethod.Cancel:
                        outputline[8] = "1"; //是否取消
                        break;
                    case enmFixMethod.Direct:
                        outputline[8] = "0"; //是否取消
                        outputline[9] = "1"; //是否拉直
                        break;
                    case enmFixMethod.CancelByDirect:
                        //使用原数据填充
                        outputline[8] = "1"; //是否取消
                        outputline[9] = "1"; //是否拉直
                        break;
                    case enmFixMethod.EmptyFly:
                        outputline[10] = "1"; //是否调机
                        break;
                }
                if (airline.PreviousAirline != null)
                {
                    outputline[11] = airline.StartTime.Subtract(airline.PreviousAirline.EndTime).TotalMinutes.ToString();
                    outputline[12] = airline.ModifiedStartTime.Subtract(airline.PreviousAirline.ModifiedEndTime).TotalMinutes.ToString();
                }
                else
                {
                    outputline[11] = "-";
                    outputline[12] = "-";
                }
                outputline[13] = Math.Abs(airline.StartTime.Subtract(airline.ModifiedStartTime).TotalMinutes).ToString();
                if (airline.ComboAirline != null)
                {
                    outputline[14] = airline.ComboAirline.No;
                }
                writer.WriteLine(string.Join(",", outputline));
            }
            writer.Close();
        }
    }
}