using System.Collections.Generic;
using System;

namespace AIForAirline
{
    /// <summary>
    /// 核心算法
    /// </summary>
    public static partial class CoreAlgorithm
    {
        public static List<Airline> EmptyFlyList = new List<Airline>();
        //关于台风的极端处理
        //1.调机开始点：如果一个航班将要飞往一个台风机场如果无法降落，则视为是开始点，
        //  虽然台风允许提早，则也不一定可以起飞
        //2.第一个没有台风影响的航班作为调机终止点
        //如果没有航班，则选择下一个
        //考虑抢先做调机工作
        public static bool FixByEmptyFly(List<Airline> AirlineList, bool IsTry)
        {
            int StartIndex, EndIndex;
            var range = GetTyphoonRange(AirlineList);
            StartIndex = range.StartIndex;
            EndIndex = range.EndIndex;
            //StartIndex 是受   台风影响的航班
            //EndIndex   是不受 台风影响的航班！
            if (EndIndex == -1 || StartIndex == -1) return false;
            //空飞航班起点在 StartIndex，终点在 EndIndex

            bool CanEmptyFly = IsCanEmptyFly(AirlineList, ref StartIndex, ref EndIndex);
            if (!CanEmptyFly) return false;
            //新增航班
            Airline EmptyFly = GetEmptyFly(AirlineList, StartIndex, EndIndex);
            if (!IsTry)
            {
                EmptyFlyList.Add(EmptyFly);
            }
            for (int i = StartIndex; i < EndIndex; i++)
            {
                AirlineList[i].FixMethod = enmFixMethod.Cancel;
            }
            return CoreAlgorithm.AdjustAirLineList(AirlineList);
        }

        private static Airline GetEmptyFly(List<Airline> AirlineList, int StartIndex, int EndIndex)
        {
            var EmptyFly = new Airline();
            //起飞时间取起飞前50分钟
            int EmptyFlySpan = Solution.FlyTimeDic[AirlineList[StartIndex].PlaneType + int.Parse(AirlineList[StartIndex].StartAirPort).ToString("D2") +
                                                                                       int.Parse(AirlineList[EndIndex].StartAirPort).ToString("D2")];
            EmptyFly.EndTime = AirlineList[EndIndex].StartTime.AddMinutes(-Utility.StayAtAirPortMinutes);
            EmptyFly.StartTime = EmptyFly.EndTime.AddMinutes(-EmptyFlySpan);
            EmptyFly.ModifiedStartTime = EmptyFly.StartTime;
            EmptyFly.ModifiedPlaneID = AirlineList[StartIndex].ModifiedPlaneID;
            EmptyFly.PlaneType = AirlineList[StartIndex].PlaneType;
            EmptyFly.FixMethod = enmFixMethod.EmptyFly;
            EmptyFly.StartAirPort = AirlineList[StartIndex].StartAirPort;
            EmptyFly.EndAirPort = AirlineList[EndIndex].StartAirPort;
            //调整调机后航班的PreviousAirLine的值
            AirlineList[EndIndex].PreviousAirline = EmptyFly;
            EmptyFly.NextAirLine = AirlineList[EndIndex];
            AirlineList[StartIndex - 1].NextAirLine = EmptyFly;
            EmptyFly.PreviousAirline = AirlineList[StartIndex - 1];
            //目标机场能否降落？
            var TyphoonProblem = CheckCondition.IsExistTyphoon(EmptyFly);

            switch (TyphoonProblem.DetailType)
            {
                //调整落地时间
                case ProblemType.TyphoonLand:
                    //获得降落机场停机的最后降落时间
                    foreach (var typhoon in CheckCondition.TyphoonList)
                    {
                        if (typhoon.AirPort == EmptyFly.EndAirPort)
                        {
                            EmptyFly.EndTime = typhoon.EndTime;
                            EmptyFly.StartTime = EmptyFly.EndTime.AddMinutes(-EmptyFlySpan);
                            EmptyFly.ModifiedStartTime = EmptyFly.StartTime;
                            //过站时间不足的处理
                            if (EmptyFly.NextAirLine.ModifiedStartTime.Subtract(EmptyFly.ModifiedEndTime).TotalMinutes < Utility.StayAtAirPortMinutes)
                            {
                                EmptyFly.NextAirLine.Problem = new Problem
                                {
                                    TakeOffBeforeThisTime = DateTime.MinValue,
                                    TakeoffAfterThisTime = EmptyFly.ModifiedEndTime.AddMinutes(Utility.StayAtAirPortMinutes)
                                };
                                FixAirportProblemByChangeTakeOffTime(AirlineList, EndIndex, EmptyFly.NextAirLine);
                            }
                            if (AirlineList[EndIndex].IsUseTyphoonRoom)
                            {
                                //如果降落的时候使用IsCanEmptyFly方法是用了机库，这里通过调整，使得不用到机库，退还机库
                                CheckCondition.TyphoonAirportRemain[EmptyFly.EndAirPort]++;
                            }
                            break;
                        }
                    }
                    break;
                case ProblemType.TyphoonTakeOff:
                    //获得起飞机场停机的最早起飞时间
                    foreach (var typhoon in CheckCondition.TyphoonList)
                    {
                        if (typhoon.AirPort == EmptyFly.StartAirPort && typhoon.TroubleType.Equals("起飞"))
                        {
                            EmptyFly.StartTime = typhoon.StartTime;
                            EmptyFly.EndTime = EmptyFly.StartTime.AddMinutes(EmptyFlySpan);
                            EmptyFly.ModifiedStartTime = EmptyFly.StartTime;
                            //最早时间起飞，再影响上一个航班也没有办法了
                            break;
                        }
                    }
                    break;
            }

            var AirportCloseLand = CheckCondition.IsAirPortAvalible(EmptyFly.EndAirPort, EmptyFly.ModifiedEndTime);
            var AirportCloseTakeoff = CheckCondition.IsAirPortAvalible(EmptyFly.StartAirPort, EmptyFly.ModifiedStartTime);
            if (!AirportCloseLand.IsAvalible)
            {
                var TempProblem = new Problem();
                CheckCondition.SetRecoverInfo(EmptyFly, TempProblem, AirportCloseLand, false);
                EmptyFly.ModifiedStartTime = TempProblem.TakeoffAfterThisTime;
            }

            if (!AirportCloseTakeoff.IsAvalible)
            {
                var TempProblem = new Problem();
                CheckCondition.SetRecoverInfo(EmptyFly, TempProblem, AirportCloseTakeoff, true);
                EmptyFly.ModifiedStartTime = TempProblem.TakeOffBeforeThisTime;
            }
            AdjustTakeOffTime(EmptyFly);
            return EmptyFly;
        }

        public static (int StartIndex, int EndIndex) GetTyphoonRange(List<Airline> AirlineList)
        {
            int StartIndex = -1;
            int EndIndex = -1;
            for (int i = 0; i < AirlineList.Count; i++)
            {
                var problem = GetProblem(AirlineList[i]);
                if (StartIndex == -1)
                {
                    //还没有设定台风调机开始点
                    switch (problem.DetailType)
                    {
                        case ProblemType.TyphoonLand:
                            StartIndex = i;
                            break;
                        case ProblemType.TyphoonTakeOff:
                            //虽然可以提前起飞，但是6个小时提前在台风停机的范围里面,或者机场关闭中
                            var AirlineClone = Utility.DeepCopy(AirlineList[i]);
                            AirlineClone.ModifiedStartTime = AirlineClone.ModifiedStartTime.AddMinutes(-Utility.EarlyMaxMinute);
                            if (GetProblem(AirlineClone).DetailType != ProblemType.None) StartIndex = i;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    //已经有台风开始点
                    switch (problem.DetailType)
                    {
                        case ProblemType.TyphoonLand:
                        case ProblemType.TyphoonTakeOff:
                            EndIndex = i + 1;
                            break;
                        default:
                            break;
                    }
                }
            }
            return (StartIndex, EndIndex);
        }


        //是否能改为空飞航班
        private static bool IsCanEmptyFly(List<Airline> AirlineList, ref int StartIndex, ref int EndIndex)
        {
            bool CanEmptyFly = false;
            //航班表检查
            for (int st = StartIndex; st > 0; st--)
            {
                var key = AirlineList[st].PlaneType + int.Parse(AirlineList[st].StartAirPort).ToString("D2");
                for (int ed = EndIndex; ed < AirlineList.Count; ed++)
                {
                    //是否拥有航线
                    if (Solution.FlyTimeDic.ContainsKey(key + int.Parse(AirlineList[ed].StartAirPort).ToString("D2")))
                    {
                        //是否为国内
                        if (Solution.DomaticAirport.Contains(AirlineList[ed].StartAirPort) &&
                            Solution.DomaticAirport.Contains(AirlineList[st].StartAirPort))
                        {
                            //是否航班机飞机限制
                            if (CheckCondition.IsAirlinePlaneAvalible(AirlineList[ed].StartAirPort, AirlineList[st].StartAirPort,
                                AirlineList[0].ModifiedPlaneID))
                            {
                                if (IsTyphoonOK(AirlineList, st, ed))
                                {
                                    EndIndex = ed;
                                    StartIndex = st;
                                    CanEmptyFly = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (CanEmptyFly) break;
            }
            return CanEmptyFly;
        }

        private static bool IsTyphoonOK(List<Airline> AirlineList, int startIndex, int endIndex)
        {
            //防止出现空飞的上一班结束和下一班开始都是台风限制的航班，并且中间夹着起飞停机时间
            //前者最早起飞也无法在后者结束之后才降落，则不选
            bool IsTyphoonOK = true;
            //起始机场是台风机场
            if (CheckCondition.TyphoonAirport.Contains(AirlineList[startIndex].StartAirPort))
            {
                if (Utility.IsUseTyphoonStayRoom)
                {
                    if (CheckCondition.TyphoonAirportRemain[AirlineList[startIndex].StartAirPort] != 0)
                    {
                        CheckCondition.TyphoonAirportRemain[AirlineList[startIndex].StartAirPort]--;
                        AirlineList[startIndex].IsUseTyphoonRoom = true;
                    }
                    else
                    {
                        IsTyphoonOK = false;
                    }
                }
                else
                {
                    IsTyphoonOK = false;
                }
            }
            //降落机场是台风机场
            if (CheckCondition.TyphoonAirport.Contains(AirlineList[endIndex].StartAirPort))
            {
                if (Utility.IsUseTyphoonStayRoom)
                {
                    if (CheckCondition.TyphoonAirportRemain[AirlineList[endIndex].StartAirPort] != 0)
                    {
                        CheckCondition.TyphoonAirportRemain[AirlineList[endIndex].StartAirPort]--;
                        AirlineList[endIndex].IsUseTyphoonRoom = true;
                    }
                    else
                    {
                        IsTyphoonOK = false;
                    }
                }
                else
                {
                    IsTyphoonOK = false;
                }
            }
            return IsTyphoonOK;
        }

        static void AdjustTakeOffTime(Airline emptyfly)
        {
            //以下四种策略进行可行性测试
            //如果起飞之前停在台风机场
            var StayCheck = IsStayAtTyphoonAirportBeforeTakeOff(emptyfly);
            if (StayCheck.isOverlap)
            {
                //修改起飞时间到最早起飞时间
                emptyfly.ModifiedStartTime = StayCheck.typhonn.StartTime;
            }

            //如果降落之后停在台风机场
            StayCheck = IsStayAtTyphoonAirportAfterLand(emptyfly);
            if (StayCheck.isOverlap)
            {
                //修改起飞时间到最晚起飞时间
                emptyfly.ModifiedStartTime = StayCheck.typhonn.EndTime.Subtract(emptyfly.FlightSpan);
            }
        }
        public static (bool isOverlap, Typhoon typhonn) IsStayAtTyphoonAirportBeforeTakeOff(Airline airline)
        {
            foreach (var typhoon in CheckCondition.TyphoonList)
            {
                if (typhoon.AirPort.Equals(airline.StartAirPort) && typhoon.TroubleType.Equals("停机"))
                {
                    if (airline.PreviousAirline != null)
                    {
                        //前一班降落时间是否在台风停机时段之后，如果之后，则返回否，不然为真
                        return (airline.PreviousAirline.ModifiedEndTime < typhoon.EndTime, typhoon);
                    }
                }
                else
                {
                    continue;
                }
            }
            return (false, new Typhoon());
        }

        public static (bool isOverlap, Typhoon typhonn) IsStayAtTyphoonAirportAfterLand(Airline airline)
        {
            foreach (var typhoon in CheckCondition.TyphoonList)
            {
                if (typhoon.AirPort.Equals(airline.EndAirPort) && typhoon.TroubleType.Equals("停机"))
                {
                    if (airline.NextAirLine != null)
                    {
                        //降落在台风后面
                        bool LandTimeInRange = typhoon.IsInRange(airline.ModifiedEndTime);
                        bool TakeOffTimeInRange = typhoon.IsInRange(airline.NextAirLine.ModifiedStartTime);
                        return (LandTimeInRange || TakeOffTimeInRange, typhoon);
                    }
                }
                else
                {
                    continue;
                }
            }
            return (false, new Typhoon());
        }
    }
}