using System;

namespace AIForAirline
{
    //飞机限制表
    public struct TransTime
    {
        //起飞机场
        public string StartAirPort;
        //到达机场
        public string EndAirPort;
        //中转时间
        public int TransferTime;

        public int GuestCnt;

        public TransTime(string RawData)
        {
            var RawDataArray = RawData.Split(",".ToCharArray());
            StartAirPort = RawDataArray[0];
            EndAirPort = RawDataArray[1];
            TransferTime = int.Parse(RawDataArray[2]);
            GuestCnt =int.Parse(RawDataArray[3]);
        }
    }
}