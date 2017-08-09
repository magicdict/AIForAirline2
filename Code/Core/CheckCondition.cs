using System;
using System.Collections.Generic;

namespace AIForAirline
{
    public static class CheckCondition
    {
        /// <summary>
        /// 航班可用性检查
        /// </summary>
        /// <param name="airline"></param>
        /// <returns></returns>
        public static Problem CheckAirLine(Airline airline)
        {
            //问题列表
            List<Problem> ProblemList = new List<Problem>();

            //机场关闭限制的检查
            //起飞，降落机场的确认
            var checkResult = IsAirPortAvalible(airline.StartAirPort, airline.ModifiedStartTime);
            if (!checkResult.IsAvalible)
            {
                var problem = new Problem();
                Utility.Log("[机场关闭限制：起飞]航班ID：[" + airline.ID + "] 机场：[" + airline.StartAirPort + "] 时间：[" + airline.ModifiedStartTime + "]");
                problem.DetailType = ProblemType.AirportProhibitTakeOff;
                SetRecoverInfo(airline, problem, checkResult, true);
                Utility.Log("StartTime:" + airline.ModifiedStartTime);
                Utility.Log("TakeOffBeforeThisTime:" + problem.TakeOffBeforeThisTime + " TakeoffAfterThisTime:" + problem.TakeoffAfterThisTime);
                ProblemList.Add(problem);
            }

            checkResult = IsAirPortAvalible(airline.EndAirPort, airline.ModifiedEndTime);
            if (!checkResult.IsAvalible)
            {
                var problem = new Problem();
                Utility.Log("[机场关闭限制：降落]航班ID：[" + airline.ID + "] 机场：[" + airline.EndAirPort + "] 时间：[" + airline.ModifiedEndTime + "]");
                problem.DetailType = ProblemType.AirportProhibitLand;
                SetRecoverInfo(airline, problem, checkResult, false);
                Utility.Log("EndTime:" + airline.ModifiedEndTime);
                Utility.Log("TakeOffBeforeThisTime:" + problem.TakeOffBeforeThisTime + " TakeoffAfterThisTime:" + problem.TakeoffAfterThisTime);
                ProblemList.Add(problem);

            }

            //航班-飞机限制的检查
            if (!IsAirlinePlaneAvalible(airline.StartAirPort, airline.EndAirPort, airline.ModifiedPlaneID))
            {
                var problem = new Problem();
                Utility.Log("[飞行限制          ]航班ID：[" + airline.ID + "] 起飞机场：[" +
                airline.StartAirPort + "] 降落机场：[" + airline.EndAirPort + "] 飞机编号：[" + airline.ModifiedPlaneID + "]");
                problem.DetailType = ProblemType.AirLinePlaneProhibit;
                ProblemList.Add(problem);
            }
            if (ProblemList.Count > 1)
            {
                if (ProblemList[0].TakeoffAfterThisTime > ProblemList[1].TakeoffAfterThisTime)
                {
                    return ProblemList[0];
                }
                else
                {
                    return ProblemList[1];
                }
            };

            if (ProblemList.Count == 0)
            {
                ProblemList.Add(new Problem());
                if (Utility.PrintInfo) Utility.Log("[正常              ]航班ID：[" + airline.ID + "]");
            }
            return ProblemList[0];
        }

        /// <summary>
        /// 设定最早（晚）能够起飞（降落）的时间
        /// </summary>
        /// <param name="airline"></param>
        /// <param name="problem"></param>
        /// <param name="checkResult"></param>
        /// <param name="IsTakeOff"></param>
        public static void SetRecoverInfo(Airline airline, Problem problem,
                                         (bool IsAvalible, string CloseTime, string OpenTime) checkResult, bool IsTakeOff)
        {
            //在关闭之前起飞或者在关闭之后起飞
            //跨日期、不跨日期分开处理
            int CloseHour = int.Parse(checkResult.CloseTime.Substring(0, 2));
            int CloseMinute = int.Parse(checkResult.CloseTime.Substring(3, 2));
            int OpenHour = int.Parse(checkResult.OpenTime.Substring(0, 2));
            int OpenMinute = int.Parse(checkResult.OpenTime.Substring(3, 2));
            var EarlyTime = DateTime.MinValue;
            var LateTime = DateTime.MaxValue;
            var StandDay = IsTakeOff ? airline.ModifiedStartTime : airline.ModifiedEndTime;
            if (checkResult.CloseTime.CompareTo(checkResult.OpenTime) < 0)
            {
                //关闭和开启在同一天,则要么在当日的关闭之前起飞，要么在当日关闭之后起飞
                EarlyTime = new DateTime(StandDay.Year, StandDay.Month, StandDay.Day, CloseHour, CloseMinute, 0);
                LateTime = new DateTime(StandDay.Year, StandDay.Month, StandDay.Day, OpenHour, OpenMinute, 0);
            }
            else
            {
                //跨日期的情况
                //这里需要区分前半段还是后半段
                if (StandDay.ToString(Utility.TimeFormat).CompareTo(checkResult.OpenTime) < 0)
                {
                    //标准时刻小于OpenTime的，则属于后半夜
                    var previoustoday = StandDay.AddDays(-1);
                    EarlyTime = new DateTime(previoustoday.Year, previoustoday.Month, previoustoday.Day, CloseHour, CloseMinute, 0);
                    LateTime = new DateTime(StandDay.Year, StandDay.Month, StandDay.Day, OpenHour, OpenMinute, 0);
                }
                else
                {
                    //标准时刻大于CloseTime的，则属于前半夜
                    if (StandDay.ToString(Utility.TimeFormat).CompareTo(checkResult.CloseTime) > 0)
                    {
                        var nextstartday = StandDay.AddDays(1);
                        EarlyTime = new DateTime(StandDay.Year, StandDay.Month, StandDay.Day, CloseHour, CloseMinute, 0);
                        LateTime = new DateTime(nextstartday.Year, nextstartday.Month, nextstartday.Day, OpenHour, OpenMinute, 0);
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
            }
            if (IsTakeOff)
            {
                //注意：起飞时间不能超过6小时，这个在后面进行调整！
                problem.TakeOffBeforeThisTime = EarlyTime;
                problem.TakeoffAfterThisTime = LateTime;
            }
            else
            {
                problem.TakeOffBeforeThisTime = EarlyTime.Subtract(airline.FlightSpan);
                problem.TakeoffAfterThisTime = LateTime.Subtract(airline.FlightSpan);
            }
        }


        //机场关闭列表
        public static List<AirPortProhibit> AirPortProhibitList = new List<AirPortProhibit>();
        //某个时刻机场可用性检查
        public static (bool IsAvalible, string CloseTime, string OpenTime) IsAirPortAvalible(string airPort, DateTime checkPoint)
        {
            foreach (var item in AirPortProhibitList)
            {
                if (!item.AirportAvalible(checkPoint, airPort))
                {
                    //TODO:如果这个机场禁止规则是当日的，则直接返回当天时间的OpenClose即可
                    //如果是跨日的，则需要判断一下日期和时间怎么设定！
                    return (false, item.CloseTime, item.OpenTime);
                }
            }
            return (true, string.Empty, string.Empty);
        }


        //飞行限制列表
        public static List<PlaneProhibit> PlaneProhibitList = new List<PlaneProhibit>();
        //飞行限制检查
        public static bool IsAirlinePlaneAvalible(string fromAirport, string toAirport, string planeId)
        {
            foreach (var Prohibit in PlaneProhibitList)
            {
                if (Prohibit.StartAirPort.Equals(fromAirport) &&
                    Prohibit.EndAirPort.Equals(toAirport) &&
                    Prohibit.PlaneID.Equals(planeId)) return false;
            }
            return true;
        }

        //故障表
        public static List<Typhoon> TyphoonList = new List<Typhoon>();
        //台风机场列表
        public static List<string> TyphoonAirport = new List<string>();

        /// <summary>
        /// 是否存在台风故障
        /// </summary>
        /// <param name="airline"></param>
        /// <returns></returns>
        public static Problem IsExistTyphoon(Airline airline)
        {
            var problem = new Problem();
            foreach (var typhoon in TyphoonList)
            {
                bool TakeOffTimeInRange = typhoon.IsInRange(airline.ModifiedStartTime) && airline.StartAirPort.Equals(typhoon.AirPort);
                bool LandTimeInRange = typhoon.IsInRange(airline.ModifiedEndTime) && airline.EndAirPort.Equals(typhoon.AirPort);
                switch (typhoon.TroubleType)
                {
                    case "停机":
                        if (airline.PreviousAirline != null)
                        {
                            if (airline.PreviousAirline.EndAirPort == typhoon.AirPort)
                            {
                                var t1 = airline.PreviousAirline.ModifiedEndTime;
                                var t2 = airline.ModifiedStartTime;
                                var t3 = typhoon.StartTime;
                                var t4 = typhoon.EndTime;
                                if (t3 < t2 && t4 > t1) problem.DetailType = ProblemType.TyphoonStay;
                            }
                        }
                        //台风：停机(暂时不认为会出现起飞和降落同时遇到停机的情况)
                        if (TakeOffTimeInRange || LandTimeInRange)
                        {
                            Utility.Log("[台风：停机]航班ID：[" + airline.ID + "]");
                            Utility.Log(typhoon.ToString());
                            if (TakeOffTimeInRange) problem.DetailType = ProblemType.TyphoonTakeOff;
                            if (LandTimeInRange) problem.DetailType = ProblemType.TyphoonLand;
                            return problem;
                        }
                        break;
                    case "降落":
                        //台风：降落
                        if (LandTimeInRange && airline.EndAirPort.Equals(typhoon.AirPort))
                        {
                            Utility.Log("[台风：降落]航班ID：[" + airline.ID + "]");
                            Utility.Log(typhoon.ToString());
                            problem.DetailType = ProblemType.TyphoonLand;
                            problem.TakeOffBeforeThisTime = typhoon.StartTime.Subtract(airline.FlightSpan);
                            problem.TakeoffAfterThisTime = typhoon.EndTime.Subtract(airline.FlightSpan);
                            return problem;
                        }
                        break;
                    case "起飞":
                        //台风：起飞
                        if (TakeOffTimeInRange && airline.StartAirPort.Equals(typhoon.AirPort))
                        {
                            Utility.Log("[台风：起飞]航班ID：[" + airline.ID + "]");
                            Utility.Log(typhoon.ToString());
                            problem.DetailType = ProblemType.TyphoonTakeOff;
                            problem.TakeOffBeforeThisTime = typhoon.StartTime;
                            problem.TakeoffAfterThisTime = typhoon.EndTime;
                            return problem;
                        }
                        break;
                    default:
                        continue;
                }
            }
            return problem;
        }
    }
}