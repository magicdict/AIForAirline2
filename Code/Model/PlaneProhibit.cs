using System;

namespace AIForAirline
{
    //飞机限制表
    public struct PlaneProhibit
    {
        //起飞机场
        public string StartAirPort;
        //到达机场
        public string EndAirPort;
        //飞机ID
        public string PlaneID;
        public PlaneProhibit(string RawData)
        {
            var RawDataArray = RawData.Split(",".ToCharArray());
            StartAirPort = RawDataArray[0];
            EndAirPort = RawDataArray[1];
            PlaneID = RawDataArray[2];
        }
    }
}