using System;

namespace AIForAirline
{
    //飞机限制表
    public struct TransTime
    {
        //进港航班ID
        public string LandAirlineID;
        //出港航班ID
        public string TakeOffAirlineID;
        //中转时间
        public int TransferTime;

        public int GuestCnt;

        public TransTime(string RawData)
        {
            var RawDataArray = RawData.Split(",".ToCharArray());
            LandAirlineID = RawDataArray[0];
            TakeOffAirlineID = RawDataArray[1];
            TransferTime = int.Parse(RawDataArray[2]);
            GuestCnt =int.Parse(RawDataArray[3]);
        }
    }
}