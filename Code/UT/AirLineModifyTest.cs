using System;

namespace AIForAirline
{
    //单体测试
    public static class AirLineModifyTest
    {
        public static void ModifyTest()
        {
            Airline A0001 = new Airline()
            {
                ID = "A0001",
                Date = "2016-6-15",
                No = "1",
                StartAirPort = "北京",
                EndAirPort = "武汉",
                StartTime = DateTime.Parse("2016/6/15 14:47"),
                EndTime = DateTime.Parse("2016/6/15 16:33")
            };

            A0001.ModifiedStartTime = DateTime.Parse("2016/6/15 13:47");
            Utility.Log("A0001:StartTime:" + A0001.StartTime);
            Utility.Log("A0001:EndTime  :" + A0001.EndTime);
            Utility.Log("A0001:ModifiedStartTime:" + A0001.ModifiedStartTime);
            Utility.Log("A0001:ModifiedEndTime  :" + A0001.ModifiedEndTime);
        }
    }
}