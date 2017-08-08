using System;

namespace AIForAirline
{
    /// <summary>
    /// 台风故障表
    /// </summary>
    public struct Typhoon
    {
        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime;
        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime EndTime;
        /// <summary>
        /// 故障类型(飞行/降落)
        /// </summary>
        public String TroubleType;
        /// <summary>
        /// 机场
        /// </summary>
        public string AirPort;
        //停机数
        public int StayCnt;
        /// <summary>
        /// 是否在范围内
        /// </summary>
        /// <param name="TimePoint"></param>
        /// <returns></returns>
        public bool IsInRange(DateTime TimePoint)
        {
            return (TimePoint > StartTime) && (TimePoint < EndTime);
        }

        /// <summary>
        /// 从文本初始化
        /// </summary>
        /// <param name="RawData"></param>
        public Typhoon(string RawData)
        {
            var RawDataArray = RawData.Split(",".ToCharArray());
            StartTime = DateTime.Parse(RawDataArray[0]);
            EndTime = DateTime.Parse(RawDataArray[1]);
            TroubleType = RawDataArray[2];
            AirPort = RawDataArray[3];
            //第二赛季没有飞机和航班限制
            StayCnt = string.IsNullOrEmpty(RawDataArray[6]) ? 0 : int.Parse(RawDataArray[6]);
        }

        /// <summary>
        /// 转字符
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "\tStartTime:[" + StartTime.ToString() + "] \tEndTime:[" + EndTime.ToString() +
                   "] \tTroubleType:[" + TroubleType + "] \tAirPort:[" + AirPort + "]";
        }

    }
}