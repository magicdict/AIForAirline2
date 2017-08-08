using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using System.Diagnostics;

namespace AIForAirline
{
    /// <summary>
    /// 优化结果
    /// </summary>
    public static partial class ResultOptimize
    {
        //按照机场号整理的航班表
        public static Dictionary<string, List<AirportInfo>> AirportIdAirlineDic = new Dictionary<string, List<AirportInfo>>();

        public static void Run()
        {
            foreach (var airline in Solution.AirlineDic.Values)
            {
                if (!AirportIdAirlineDic.ContainsKey(airline.StartAirPort)) AirportIdAirlineDic.Add(airline.StartAirPort, new List<AirportInfo>());
                if (!AirportIdAirlineDic.ContainsKey(airline.EndAirPort)) AirportIdAirlineDic.Add(airline.EndAirPort, new List<AirportInfo>());
                AirportIdAirlineDic[airline.StartAirPort].Add(new AirportInfo()
                {
                    IsTakeOff = true,
                    EventTime = airline.ModifiedStartTime,
                    EventAirline = airline
                });
                AirportIdAirlineDic[airline.EndAirPort].Add(new AirportInfo()
                {
                    IsTakeOff = false,
                    EventTime = airline.ModifiedEndTime,
                    EventAirline = airline
                });
            }

            foreach (var airline in CoreAlgorithm.EmptyFlyList)
            {
                if (!AirportIdAirlineDic.ContainsKey(airline.StartAirPort)) AirportIdAirlineDic.Add(airline.StartAirPort, new List<AirportInfo>());
                if (!AirportIdAirlineDic.ContainsKey(airline.EndAirPort)) AirportIdAirlineDic.Add(airline.EndAirPort, new List<AirportInfo>());
                AirportIdAirlineDic[airline.StartAirPort].Add(new AirportInfo()
                {
                    IsTakeOff = true,
                    EventTime = airline.ModifiedStartTime,
                    EventAirline = airline
                });
                AirportIdAirlineDic[airline.EndAirPort].Add(new AirportInfo()
                {
                    IsTakeOff = false,
                    EventTime = airline.ModifiedEndTime,
                    EventAirline = airline
                });
            }

            //修改执飞航班
            ResultOptimize.TryChangePlane();

            CancelPairList.Clear();
            GetCancelPair();
            //按照机场和重要度进行排序
            CancelPairList.Sort((x, y) =>
            {
                if (x.StartAirPort == y.StartAirPort)
                {
                    return y.ImportFac.CompareTo(x.ImportFac);
                }
                else
                {
                    return x.StartAirPort.CompareTo(y.StartAirPort);
                }
            });

            GapList.Clear();
            PrintAirportInfo("49");
            foreach (var airportId in AirportIdAirlineDic.Keys)
            {
                PutCancelToBigGap(airportId);
            }
            //按照机场和时间进行排序
            GapList.Sort(
                (x, y) =>
                {
                    if (x.AirportID == y.AirportID)
                    {
                        return x.LandTime.CompareTo(y.LandTime);
                    }
                    else
                    {
                        return x.AirportID.CompareTo(y.AirportID);
                    }
                }
            );


            //进行匹配
            for (int i = 0; i < CancelPairList.Count; i++)
            {
                double MaxScore = 0;
                int MaxIndex = -1;
                var CancelItem = CancelPairList[i];
                for (int j = 0; j < GapList.Count; j++)
                {
                    var GapItem = GapList[j];
                    if (GapItem.IsFilled) continue;

                    //Gap的机场和取消组的机场一致
                    if (CancelItem.StartAirPort != GapItem.AirportID) continue;
                    //不是同一架飞机,机型相同
                    if (CancelItem.PlaneID == GapItem.PlaneID || CancelItem.PlaneType != GapItem.PlaneType) continue;
                    //中间是台风，暂时不考虑
                    var ArrivalTime = GapItem.LandTime.AddMinutes(Utility.StayAtAirPortMinutes) + CancelItem.First.FlightSpan;
                    if (CheckCondition.TyphoonAirport.Contains(CancelItem.MidAirport))
                    {
                        DateTime TyphoonTime = DateTime.MinValue;
                        foreach (var item in CheckCondition.TyphoonList)
                        {
                            if (item.AirPort == CancelItem.MidAirport && item.TroubleType == "停机")
                            {
                                TyphoonTime = item.EndTime;
                                break;
                            }
                        }
                        if (ArrivalTime < TyphoonTime) continue;
                    }
                    //联航不能分割
                    if (CancelItem.First.ComboAirline != null && !CancelItem.First.IsFirstCombinedVoyage) continue;
                    if (CancelItem.Second.ComboAirline != null && CancelItem.Second.IsFirstCombinedVoyage) continue;
                    var TakeOffEarlyTime = GapItem.LandTime.AddMinutes(Utility.StayAtAirPortMinutes);
                    if (TakeOffEarlyTime <= CancelItem.First.StartTime)
                    {
                        //取消组的开始时间暂时要求在50分钟之内
                        if (CancelItem.Second.EndTime.AddMinutes(Utility.StayAtAirPortMinutes) < GapItem.TakeOffTime)
                        {
                            //结束飞行后有充分的过站时间
                            Console.WriteLine("Cancel: " + CancelItem.PlaneID + " StartAirPort:" + CancelItem.StartAirPort + " MidAirPort：" + CancelItem.MidAirport + " " +
                            CancelItem.First.StartTime + " " + CancelItem.Second.EndTime);
                            Console.WriteLine("Gap: " + GapItem.PlaneID + " StartAirPort:" + GapItem.AirportID + " " + GapItem.LandTime + " " + GapItem.TakeOffTime);
                        }
                    }
                    else
                    {
                        //允许延期
                        var CloneAirline = Utility.DeepCopy(Solution.PlaneIdAirlineDic[GapItem.PlaneID]);
                        //取消恢复后的可以挽回的分数：
                        var FixScore = CancelItem.ImportFac * Statistics.CancelAirlineParm;
                        //修改前分数
                        var GapBefore = Statistics.WriteResult(CloneAirline);
                        //添加取消项目到原列表中
                        var C1 = Utility.DeepCopy(CancelItem.First);
                        var C2 = Utility.DeepCopy(CancelItem.Second);
                        var TimeDiff = TakeOffEarlyTime.Subtract(CancelItem.First.StartTime);
                        C1.ModifiedStartTime = TakeOffEarlyTime;
                        C2.ModifiedStartTime = C1.ModifiedEndTime.AddMinutes(Utility.StayAtAirPortMinutes);
                        C1.FixMethod = enmFixMethod.UnFixed;
                        C2.FixMethod = enmFixMethod.UnFixed;
                        C1.ModifiedPlaneID = GapItem.PlaneID;
                        C2.ModifiedPlaneID = GapItem.PlaneID;
                        if (C2.ModifiedEndTime.AddMinutes(Utility.StayAtAirPortMinutes) > GapItem.TakeOffTime) continue;
                        CloneAirline.Add(C1);
                        CloneAirline.Add(C2);
                        CloneAirline.Sort((x, y) => { return x.ModifiedStartTime.CompareTo(y.ModifiedStartTime); });
                        if (!CoreAlgorithm.AdjustAirLineList(CloneAirline)) continue;
                        var GapAfter = Statistics.WriteResult(CloneAirline);
                        var Decream = FixScore - (GapAfter - GapBefore);
                        if (Decream > MaxScore)
                        {
                            MaxScore = Decream;
                            MaxIndex = j;
                        }
                    }
                }

                if (MaxIndex != -1)
                {
                    var GapItem = GapList[MaxIndex];
                    Console.WriteLine("Gap PlaneID:" + GapItem.PlaneID + " Max Score:" + MaxScore);
                    Console.WriteLine("Gap Land:" + GapItem.LandAirline.ToString());
                    Console.WriteLine("Gap Takeoff:" + GapItem.TakeoffAirLine.ToString());
                    var TakeOffEarlyTime = GapItem.LandTime.AddMinutes(Utility.StayAtAirPortMinutes);
                    CancelItem.First.ModifiedStartTime = TakeOffEarlyTime;
                    CancelItem.Second.ModifiedStartTime = CancelItem.First.ModifiedEndTime.AddMinutes(Utility.StayAtAirPortMinutes);
                    CancelItem.First.ModifiedPlaneID = GapItem.PlaneID;
                    CancelItem.Second.ModifiedPlaneID = GapItem.PlaneID;
                    CancelItem.First.FixMethod = enmFixMethod.UnFixed;
                    CancelItem.Second.FixMethod = enmFixMethod.UnFixed;
                    Solution.PlaneIdAirlineDic[GapItem.PlaneID].Add(CancelItem.First);
                    Solution.PlaneIdAirlineDic[GapItem.PlaneID].Add(CancelItem.Second);
                    Solution.PlaneIdAirlineDic[GapItem.PlaneID].Sort((x, y) => { return x.ModifiedStartTime.CompareTo(y.ModifiedStartTime); });
                    CoreAlgorithm.AdjustAirLineList(Solution.PlaneIdAirlineDic[GapItem.PlaneID]);
                    Solution.GetAirlineDicByPlaneId();
                    GapItem.IsFilled = true;
                }
            }
        }

        static List<Gap> GapList = new List<Gap>();

        class Gap
        {
            public string AirportID;
            public DateTime LandTime;
            public DateTime TakeOffTime;

            public Airline LandAirline;

            public Airline TakeoffAirLine;
            public string PlaneID;
            public string PlaneType;
            public int GapTime
            {
                get
                {
                    return (int)TakeOffTime.Subtract(LandTime).TotalMinutes;
                }
            }
            public bool IsFilled = false;
        }

        public static void PutCancelToBigGap(string AirportID)
        {

            //寻找足够大的空余时间
            var airlines = AirportIdAirlineDic[AirportID];
            //排序
            airlines.Sort((x, y) =>
            {
                if (x.EventAirline.ModifiedPlaneID == y.EventAirline.ModifiedPlaneID)
                {
                    return x.EventAirline.ModifiedStartTime.CompareTo(y.EventAirline.ModifiedStartTime);
                }
                else
                {
                    return x.EventAirline.ModifiedPlaneID.CompareTo(y.EventAirline.ModifiedPlaneID);
                }
            });
            //去除所有已经取消的航班
            airlines = airlines.Where(x => x.EventAirline.FixMethod != enmFixMethod.Cancel).ToList();
            //按照飞机进行整理，这里不用DeepCopy的话，直接修改飞机，可能会造成CancelByEmptyFly变为ChangeTime的问题
            //运行顺序会影响结果
            var grp = airlines.GroupBy(x => x.EventAirline.ModifiedPlaneID);
            foreach (var planeAir in grp)
            {
                var planeAirList = planeAir.ToList();
                for (int i = 0; i < planeAirList.Count; i++)
                {
                    if (!planeAirList[i].IsTakeOff && i != (planeAirList.Count - 1))
                    {
                        //降落，并且不是最后一个
                        var Land = planeAirList[i];
                        var TakeOff = planeAirList[i + 1];
                        var GapTime = TakeOff.EventTime.Subtract(Land.EventTime).TotalMinutes;
                        GapList.Add(new Gap()
                        {
                            AirportID = AirportID,
                            LandTime = Land.EventTime,
                            TakeOffTime = TakeOff.EventTime,
                            LandAirline = Land.EventAirline,
                            TakeoffAirLine = TakeOff.EventAirline,
                            PlaneID = Land.EventAirline.ModifiedPlaneID,
                            PlaneType = Land.EventAirline.PlaneType
                        });
                        //Console.WriteLine(AirportID + "," + Land.EventTime + "," + TakeOff.EventTime + "," +
                        //                  GapTime + "," + Land.EventAirline.PlaneID + "," + Land.EventAirline.PlaneType);
                    }
                }
            }
        }

        static List<CancelPair> CancelPairList = new List<CancelPair>();
        class CancelPair
        {
            public Airline First;

            public Airline Second;

            public string StartAirPort
            {
                get
                {
                    return First.StartAirPort;
                }
            }
            public string MidAirport
            {
                get
                {
                    return First.EndAirPort;
                }
            }

            public double ImportFac
            {
                get
                {
                    return First.ImportFac + Second.ImportFac;
                }
            }
            public string PlaneID
            {
                get
                {
                    return First.ModifiedPlaneID;
                }
            }
            public string PlaneType
            {
                get
                {
                    return First.PlaneType;
                }
            }
        }

        public static void GetCancelPair()
        {
            foreach (var PlaneID in Solution.PlaneIdAirlineDic.Keys)
            {
                var planeAirList = Solution.PlaneIdAirlineDic[PlaneID];
                for (int i = 0; i < planeAirList.Count; i++)
                {
                    if (i != (planeAirList.Count - 1) &&
                       planeAirList[i].FixMethod == enmFixMethod.Cancel &&      //连续取消
                       planeAirList[i + 1].FixMethod == enmFixMethod.Cancel &&
                       planeAirList[i].StartAirPort == planeAirList[i + 1].EndAirPort) //循环检查
                    {
                        CancelPairList.Add(new CancelPair()
                        {
                            First = planeAirList[i],
                            Second = planeAirList[i + 1]
                        });
                        var TotalImportFunc = planeAirList[i].ImportFac + planeAirList[i + 1].ImportFac;
                        //其实，过站时间还是可以压缩的，过站时间超过50分钟的，压缩到50分钟
                        /*Console.WriteLine(planeAirList[i].StartAirPort + "," + planeAirList[i].EndAirPort + "," + TotalImportFunc + "," +
                                          planeAirList[i].ModifiedPlaneID + "," + planeAirList[i].ID + "," +
                                          planeAirList[i + 1].ID + "," +
                                          planeAirList[i].StartTime + "," + planeAirList[i].EndTime + "," +
                                          planeAirList[i + 1].StartTime + "," + planeAirList[i + 1].EndTime + "," +
                                          planeAirList[i + 1].EndTime.Subtract(planeAirList[i].StartTime).TotalMinutes.ToString());
                        */
                    }
                }
            }
        }

        public static void PrintAirportInfo(string AirportID)
        {
            var writer = new StreamWriter(Utility.WorkSpaceRoot + "Dataset" + Path.DirectorySeparatorChar + AirportID + ".csv");
            writer.WriteLine("航班号,飞机,事件,时间,相关机场,机型,修复方法");
            var airlines = AirportIdAirlineDic[AirportID];
            airlines.Sort((x, y) =>
            {
                if (x.EventAirline.ModifiedPlaneID == y.EventAirline.ModifiedPlaneID)
                {
                    return x.EventAirline.ModifiedStartTime.CompareTo(y.EventAirline.ModifiedStartTime);
                }
                else
                {
                    return x.EventAirline.ModifiedPlaneID.CompareTo(y.EventAirline.ModifiedPlaneID);
                }
            });
            for (int i = 0; i < airlines.Count; i++)
            {
                writer.WriteLine(airlines[i].ToString());
            }
            writer.Close();
        }
    }
}