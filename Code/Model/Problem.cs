using System;
using System.Collections.Generic;

namespace AIForAirline
{
    [Serializable]
    public class Problem
    {
        //最早起飞时间
        public DateTime TakeOffBeforeThisTime = DateTime.MinValue;
        //最晚起飞时间
        public DateTime TakeoffAfterThisTime = DateTime.MaxValue;

        public ProblemType DetailType = ProblemType.None;

        //是否没有足够的过站时间（可能和台风禁飞冲突）
        public bool IsNotEnoughStayAirportTime = false;

        public DateTime TakeoffAfterThisTimeFixStayTime = DateTime.MaxValue;


        public DateTime GetTakeOffTime()
        {
            if (TakeoffAfterThisTime.Subtract(TakeoffAfterThisTimeFixStayTime).TotalMinutes > 0)
            {
                return TakeoffAfterThisTime;
            }
            else
            {
                return TakeoffAfterThisTimeFixStayTime;
            }
        }
    }

}