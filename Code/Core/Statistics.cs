using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace AIForAirline
{
    public partial class Statistics
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
        public const int PlaneTypeCnt = 4;
        public const int CancelPerGuestParm = 4;
        //旅客取消人数
        public int CancelGuestCnt;

        public static double GetDelayPerGuestParm(double TotalHours)
        {
            if (TotalHours <= 2) return 1;
            if (TotalHours <= 4) return 1.5;
            if (TotalHours <= 8) return 2;
            if (TotalHours <= 36) return 3;
            return CancelPerGuestParm;
        }

        public static double GetTransDelayPerGuestParm(double TotalHours)
        {
            if (TotalHours < 6) return 0;
            if (TotalHours < 24) return 0.5;
            if (TotalHours <= 48) return 1;
            return CancelPerGuestParm;
        }
        //旅客延误惩罚（包括签转）
        public double DelayGuestTotal = 0;

        //调机航班数(包含重要度因素)
        public double EmptyFlyAirlineCnt;
        //取消航班数(包含重要度因素)
        public double CancelAirlineCnt;
        //机型发生变化的航班数(包含重要度因素)
        public double PlaneTypeChangeAirlineCnt;

        public const int BeforeTyphoonChangeParm = 15;
        public const int AfterTyphoonChangeParm = 5;
        //台风前换机数
        public int BeforeTyphoonChangeCnt;
        //台风后换机数
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
                        result.CancelGuestCnt += airline.CancelUnAssignedGuestCnt;   //已经去除签转的人
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
                    default:
                        break;
                }
                //签转
                if (airline.SendTransList.Count != 0)
                {
                    outputline[9] = "1"; //是否拉直
                    outputline[10] = string.Join("&", airline.SendTransList);
                }
                //如果有签转（入），计算延误值
                if (airline.ReceiveTransList.Count != 0)
                {
                    foreach (var transinfo in airline.ReceiveTransList)
                    {
                        var orgStartTime = Solution.AirlineDic[transinfo.AirlineID].StartTime;
                        if (orgStartTime == airline.ModifiedStartTime) continue;                  //签转和原航班时间一致，小概率事件
                        //签转一定是延期的
                        result.DelayGuestTotal += GetTransDelayPerGuestParm(airline.ModifiedStartTime.Subtract(orgStartTime).TotalHours) * transinfo.GuestCnt;
                    }
                }

                if (airline.IsChangeStartTime && (airline.FixMethod == enmFixMethod.UnFixed || airline.FixMethod == enmFixMethod.Direct))
                {
                    //联程拉直后的航班，统计拉直的时候是两者系数之和；统计提前，延误，换机按照第一个航班来算
                    var Diff = airline.ModifiedStartTime.Subtract(airline.StartTime).TotalHours;
                    if (Diff > 0)
                    {
                        //延迟
                        result.TotalDelayHours += (Diff * airline.ImportFac);
                        //旅客延迟（非签转）
                        result.DelayGuestTotal += GetDelayPerGuestParm(Diff) * (airline.GuestCnt + airline.CombinedVoyageGuestCnt);
                    }
                    else
                    {
                        //提早(Diff是负数，这里用减法)
                        result.TotalEarlyHours -= (Diff * airline.ImportFac);
                    }
                }
                if (airline.ModifiedPlaneID != airline.PlaneID)
                {
                    //换机惩罚
                    if (airline.StartTime <= Utility.TyphoonStartTime)
                    {
                        result.BeforeTyphoonChangeCnt++;
                    }
                    else
                    {
                        result.AfterTyphoonChangeCnt++;
                    }
                    if (Solution.PlaneTypeSearchDic[airline.ModifiedPlaneID] !=
                        Solution.PlaneTypeSearchDic[airline.PlaneID])
                    {
                        //机型变换惩罚
                        //TODO:算法确认,纵横关系
                        int index = (int.Parse(airline.PlaneID) - 1) * PlaneTypeCnt + int.Parse(airline.ModifiedPlaneID) - 1;
                        result.PlaneTypeChangeAirlineCnt += PlaneTypeChangeParmTable[index];
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

            //目标函数值 = p1*调机空飞航班数 + 
            //            p2*取消航班数 + 
            //            p3*机型发生变化的航班数 + 
            //            p4*换飞机数量 + 
            //            p5*联程拉直航班的个数 + 
            //            p6*航班总延误时间（小时） + 
            //            p7*航班总提前时间（小时）+ 
            //            p8*取消旅客人数 +
            //            p9*延迟旅客人数 +
            //            p10*签转延误旅客人数。
            Double Score = 0;
            //航班计算
            Score += EmptyFlyAirlineCnt * EmptyFlyParm;  //p1*调机空飞航班数
            Score += CancelAirlineCnt * CancelAirlineParm;  //p2*取消航班数
            Score += PlaneTypeChangeAirlineCnt * PlaneTypeChangeAirlineParm; //p3*机型发生变化的航班数 
            Score += BeforeTyphoonChangeCnt * BeforeTyphoonChangeParm;       //p4*换飞机数量 
            Score += AfterTyphoonChangeCnt * AfterTyphoonChangeParm;         //p4*换飞机数量
            Score += ChangeToDirectArilineCnt * ChangeToDirectArilineParm;   //p5*联程拉直航班的个数
            Score += TotalDelayHours * TotalDelayParm;           //p6*航班总延误时间（小时）
            Score += TotalEarlyHours * TotalEarlyParm;           //p7*航班总提前时间（小时）   
            //旅客别计算
            Score += CancelGuestCnt * CancelAirlineParm;  //p8*取消旅客人数
            Score += DelayGuestTotal;                     //p9*延迟旅客人数 p10*签转延误旅客人数
            return Score;
        }
    }
}