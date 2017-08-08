using System;
using System.Collections.Generic;

namespace AIForAirline
{
    /// <summary>
    /// 核心算法
    /// </summary>
    public static partial class CoreAlgorithm
    {
        /// <summary>
        /// 修复台风引发的无法起飞
        /// </summary>
        /// <returns>是否修复成功</returns>
        /// <remarks>
        /// 可以提前起飞，但是必须能够满足过站条件
        /// </remarks>
        public static bool FixTyphoonTakeOffByChangeTakeOffTime(Airline airline)
        {
            //尝试提早起飞
            bool CanEarly = (airline.InterKbn == "国内");
            var TakeOffTime = airline.Problem.TakeOffBeforeThisTime;


            if (CanEarly)
            {
                if (airline.PreviousAirline != null)
                {
                    //是否影响上一班的过站时间
                    CanEarly = TakeOffTime.Subtract(airline.PreviousAirline.ModifiedEndTime).TotalMinutes >= Utility.StayAtAirPortMinutes;
                }
                if (CanEarly)
                {
                    //提早：过站50分钟，最大时间6小时，国内航班
                    CanEarly = airline.StartTime.Subtract(TakeOffTime).TotalMinutes <= Utility.EarlyMaxMinute;
                }
            }
            //尝试延迟起飞
            bool CanLate = true;
            TakeOffTime = airline.Problem.IsNotEnoughStayAirportTime ? airline.Problem.GetTakeOffTime() : airline.Problem.TakeoffAfterThisTime;
            if (TakeOffTime == DateTime.MaxValue)
            {
                CanLate = false;
            }
            else
            {
                var LandTime = TakeOffTime.Add(airline.FlightSpan);
                //是否影响下一班的过站时间
                CanLate = airline.NextAirLine.ModifiedStartTime.Subtract(LandTime).TotalMinutes >= Utility.StayAtAirPortMinutes;
                if (CanLate)
                {
                    //延迟：国内24小时，国际36小时
                    if (airline.InterKbn == "国内")
                    {
                        CanLate = TakeOffTime.Subtract(airline.StartTime).TotalMinutes <= Utility.DelayDemasticMaxMinute;
                    }
                    else
                    {
                        CanLate = TakeOffTime.Subtract(airline.StartTime).TotalMinutes <= Utility.DelayInternationalMaxMinute;
                    }
                }
            }


            //既不能提前，也不能延迟
            if (!CanEarly && !CanLate) return false;

            if (CanEarly && !CanLate)
            {
                //提前
                airline.ModifiedStartTime = airline.Problem.TakeOffBeforeThisTime;
            }
            if (!CanEarly && CanLate)
            {
                //延迟
                airline.ModifiedStartTime = TakeOffTime;
            }
            if (CanEarly && CanLate)
            {
                double EarlyMinutePunish = airline.StartTime.Subtract(airline.Problem.TakeOffBeforeThisTime).TotalMinutes * Statistics.TotalEarlyParm;
                double DelayMinutePunish = TakeOffTime.Subtract(airline.StartTime).TotalMinutes * Statistics.TotalDelayParm;
                if (EarlyMinutePunish > DelayMinutePunish)
                {
                    airline.ModifiedStartTime = TakeOffTime;
                }
                else
                {
                    airline.ModifiedStartTime = airline.Problem.TakeOffBeforeThisTime;
                }
            }
            Utility.Log("[起飞（机场限制）]调整完毕航班：航班ID：" + airline.ID + " 修改起飞时间：" + airline.StartTime + "->" + airline.ModifiedStartTime);
            return true;
        }
    }
}