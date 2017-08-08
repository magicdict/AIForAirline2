
using System.Collections.Generic;
using System;

namespace AIForAirline
{
    //航班环
    public class AirlineCircle
    {
        //起始和终止机场
        public string Airport;
        //起始航班在航班串种的索引
        public int StartIndex;
        //终止航班在航班串种的索引
        public int EndIndex;
        //原始航班
        public List<Airline> AirlineList = null;

        public DateTime StartTime
        {
            get
            {
                return AirlineList[StartIndex].StartTime;
            }
        }

        public DateTime EndTime
        {
            get
            {
                return AirlineList[EndIndex].EndTime;
            }
        }

        public int AirlineCount
        {
            get
            {
                return EndIndex - StartIndex;
            }
        }

        //从航班串中获得所有可能的航班环
        public static List<AirlineCircle> GetCircle(List<Airline> PlaneAirlineList)
        {
            var Circles = new List<AirlineCircle>();
            //从第一个航班开始寻找所有可能的航班环
            for (int i = 0; i < PlaneAirlineList.Count; i++)
            {
                //i为大循环开始位置
                for (int j = i; j < PlaneAirlineList.Count; j++)
                {
                    if (PlaneAirlineList[i].StartAirPort == PlaneAirlineList[j].EndAirPort)
                    {
                        Circles.Add(new AirlineCircle()
                        {
                            Airport = PlaneAirlineList[i].StartAirPort,
                            StartIndex = i,
                            EndIndex = j,
                            AirlineList = PlaneAirlineList
                        });
                    }
                }
            }
            return Circles;
        }
    }
}