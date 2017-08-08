using System;

namespace AIForAirline
{
    //单体测试
    public static class UnitTest
    {

        //基础函数的测试
        public static void CommonFunc_FormatTime()
        {
            Utility.Log("====FormatTime Unit Test====");
            //无需变更
            string RawValue_first = "23:00";
            string Predict_first = "23:00";
            UTTransFunc(RawValue_first, Predict_first, Utility.FormatTime);
            //需要变更
            string RawValue_second = "6:00";
            string Predict_second = "06:00";
            UTTransFunc(RawValue_second, Predict_second, Utility.FormatTime);
            Utility.Log("============================");
        }

        public static void CommonFunc_FormatDate()
        {
            Utility.Log("====FormatDate Unit Test====");
            //无需变更
            string RawValue_first = "2016-6-15";
            string Predict_first = "2016-06-15";
            UTTransFunc(RawValue_first, Predict_first, Utility.FormatDate);
            //需要变更
            string RawValue_second = "2016-6-5";
            string Predict_second = "2016-06-05";
            UTTransFunc(RawValue_second, Predict_second, Utility.FormatDate);
            Utility.Log("============================");
        }


        public static void UTTransFunc(string RawValue, string PredictValue, Func<string, string> TransformFunc)
        {
            string Actual = TransformFunc(RawValue);
            Utility.Log("RawValue:[" + RawValue + "]");
            Utility.Log("Actual  :[" + Actual + "]");
            Utility.Log("Predict :[" + PredictValue + "]");
            if (!PredictValue.Equals(Actual)) throw new Exception("UT Test Exception!");
        }

        //机场关闭测试
        public static void AirPortProhibitTest()
        {
            AirPortProhibit BJAir = new AirPortProhibit()
            {
                AirPort = "Beijing",
                CloseTime = Utility.FormatTime("23:00"),
                OpenTime = Utility.FormatTime("6:00"),
                StartDate = Utility.FormatDate("2016/6/15"),
                EndDate = Utility.FormatDate("2016/8/15")
            };
            AirPortProhibit SHAir = new AirPortProhibit()
            {
                AirPort = "Shanghai",
                CloseTime = Utility.FormatTime("2:00"),
                OpenTime = Utility.FormatTime("10:00"),
                StartDate = Utility.FormatDate("2016/8/12"),
                EndDate = Utility.FormatDate("2016/8/15")
            };
            Utility.Log("Airport:" + BJAir.AirPort + " CloseTime:" + BJAir.CloseTime + " OpenTime:" + BJAir.OpenTime +
            " StartDate:" + BJAir.StartDate + " EndDate:" + BJAir.EndDate);
            Utility.Log("Airport:" + SHAir.AirPort + " CloseTime:" + SHAir.CloseTime + " OpenTime:" + SHAir.OpenTime +
            " StartDate:" + SHAir.StartDate + " EndDate:" + SHAir.EndDate);

            //上海机场，无法检查北京的：上海禁飞，北京不禁飞
            UTAirport(SHAir, new DateTime(2016, 8, 13, 4, 0, 0), "Shanghai", false);
            UTAirport(SHAir, new DateTime(2016, 8, 13, 4, 0, 0), "Beijing", true);

            //跨日期测试
            //2016-8-12 2016-8-14
            //CloseTime:[23:00] OpenTime:[06:00]
            Utility.Log("Airport:" + BJAir.AirPort + " CloseTime:" + BJAir.CloseTime + " OpenTime:" +
            BJAir.OpenTime + " StartDate:" + BJAir.StartDate + " EndDate:" + BJAir.EndDate);
            //1.第一分测试 6-15 23:00            
            UTAirport(BJAir, new DateTime(2016, 6, 15, 22, 59, 59), "Beijing", true);
            UTAirport(BJAir, new DateTime(2016, 6, 15, 23, 0, 0), "Beijing", false);

            //2.最后一分钟测试 2016-08-14 23：59
            UTAirport(BJAir, new DateTime(2016, 8, 14, 23, 59, 59), "Beijing", false);
            UTAirport(BJAir, new DateTime(2016, 8, 15, 0, 0, 0), "Beijing", true);

            //3.当日测试
            UTAirport(BJAir, new DateTime(2016, 8, 1, 23, 30, 0), "Beijing", false);
            UTAirport(BJAir, new DateTime(2016, 8, 1, 22, 30, 0), "Beijing", true);

            //4.跨日测试
            UTAirport(BJAir, new DateTime(2016, 8, 2, 3, 30, 0), "Beijing", false);
            UTAirport(BJAir, new DateTime(2016, 8, 2, 6, 30, 0), "Beijing", true);

            //5.临界点测试
            UTAirport(BJAir, new DateTime(2016, 8, 1, 23, 0, 0), "Beijing", false);
            UTAirport(BJAir, new DateTime(2016, 8, 1, 22, 59, 59), "Beijing", true);
            UTAirport(BJAir, new DateTime(2016, 8, 2, 5, 59, 59), "Beijing", false);
            UTAirport(BJAir, new DateTime(2016, 8, 2, 6, 0, 0), "Beijing", true);

            //6.零点测试
            UTAirport(BJAir, new DateTime(2016, 8, 1, 0, 0, 0), "Beijing", false);

            //7.启用前测试
            UTAirport(BJAir, new DateTime(2016, 6, 15, 3, 0, 0), "Beijing", true);
            UTAirport(BJAir, new DateTime(2016, 6, 14, 3, 0, 0), "Beijing", true);

            //8.失效后测试
            UTAirport(BJAir, new DateTime(2016, 8, 15, 3, 0, 0), "Beijing", true);

            //同日期测试
            Utility.Log("Airport:" + SHAir.AirPort + " CloseTime:" + SHAir.CloseTime + " OpenTime:" +
            SHAir.OpenTime + " StartDate:" + SHAir.StartDate + " EndDate:" + SHAir.EndDate);
            //2016-8-12 2016-8-15
            //CloseTime:[02:00] OpenTime:[10:00]
            //1.第一分钟测试
            UTAirport(SHAir, new DateTime(2016, 8, 12, 1, 59, 59), "Shanghai", true);
            UTAirport(SHAir, new DateTime(2016, 8, 12, 2, 0, 0), "Shanghai", false);

            //2.最后一分钟测试
            UTAirport(SHAir, new DateTime(2016, 8, 14, 10, 0, 0), "Shanghai", true);
            UTAirport(SHAir, new DateTime(2016, 8, 14, 9, 59, 59), "Shanghai", false);

            //3.启用前测试   
            UTAirport(SHAir, new DateTime(2016, 8, 11, 8, 0, 0), "Shanghai", true);

            //4.失效后测试  
            UTAirport(SHAir, new DateTime(2016, 8, 15, 8, 0, 0), "Shanghai", true);

        }

        //机场关闭测试方法
        public static void UTAirport(AirPortProhibit TestAirPort, DateTime RawValue, string AriPort, bool Predict)
        {
            Utility.Log(RawValue.ToString("yyyy-MM-dd HH:mm") + " " + AriPort + " " + Predict.ToString());
            bool isAvalible = TestAirPort.AirportAvalible(RawValue, AriPort);
            Utility.Log("IsAvalible:[" + isAvalible + "]");
            if (!isAvalible.Equals(Predict)) throw new Exception("UT Test Exception!");
        }
    }
}