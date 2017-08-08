using System;

namespace AIForAirline
{
    //机场关闭限制表
    public struct AirPortProhibit
    {
        //机场
        public string AirPort;
        //关闭时间
        public string CloseTime;
        //开放时间
        public string OpenTime;
        //生效日期
        public string StartDate;
        //失效日期
        public string EndDate;

        //从文本初始化
        public AirPortProhibit(string RawData)
        {
            var RawDataArray = RawData.Split(",".ToCharArray());
            AirPort = RawDataArray[0];
            CloseTime = Utility.FormatTime(RawDataArray[1]);
            OpenTime = Utility.FormatTime(RawDataArray[2]);
            StartDate = Utility.FormatDate(RawDataArray[3]);
            EndDate = Utility.FormatDate(RawDataArray[4]);
        }

        //初始化
        public bool AirportAvalible(DateTime time, String airport)
        {
            //如果机场关闭规则和这条规则适用机场不符合，则退出
            if (!airport.Equals(AirPort)) return true;
            //获得时间字符串日期部分
            string Date = time.ToString(Utility.DateFormat);
            string Time = time.ToString(Utility.TimeFormat);
            //如果该待检查日期 小于开始日期，或者大于等于结束日期，则开始进一步检查。
            //注意，EndDate是失效日期，该日期的零点，规则失效。
            //2:00 10:00 2016-8-12 2016-8-15
            //2016-8-12 -> 2016-8-14 之间的日期需要检查    
            if (Date.CompareTo(StartDate) < 0 || Date.CompareTo(EndDate) >= 0)
            {
                return true;
            }
            if (CloseTime.CompareTo(OpenTime) < 0)
            {
                //如果CloseTime < OpenTime,同一天的关闭
                //2:00 10:00 2016-8-12 2016-8-15
                //CloseTime:[02:00]
                // OpenTime:[10:00]
                if (Time.CompareTo(CloseTime) > 0 && Time.CompareTo(OpenTime) < 0)
                {
                    //时刻大于关闭，小于开启，则不可用
                    return false;
                }
            }
            else
            {
                //如果CloseTime > OpenTime,表示跨日期关闭
                //2016-8-12 2016-8-14
                //CloseTime:[23:00]
                // OpenTime:[06:00]
                //时刻大于关闭，或者小于开启（开始日期当天除外），则不可用
                if (Time.CompareTo(CloseTime) > 0 ||
                    (Time.CompareTo(OpenTime) < 0 && !Date.Equals(StartDate)))
                {
                    //时刻大于关闭，小于开启，则不可用
                    return false;
                }
            }

            return true;
        }

    }
}