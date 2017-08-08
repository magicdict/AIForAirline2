using System;

namespace AIForAirline
{
    class ComboAirLineTest
    {

        public static void RunTest()
        {

            Airline A0001 = new Airline()
            {
                ID = "A0001",
                Date = "2016-6-15",
                No = "1",
                StartAirPort = "北京",
                EndAirPort = "武汉"
            };

            Airline A0002 = new Airline()
            {
                ID = "A0002",
                Date = "2016-6-19",
                No = "2",
                StartAirPort = "武汉",
                EndAirPort = "北京"
            };


            //联合运输
            Airline A0003 = new Airline()
            {
                ID = "A0003",
                Date = "2016-6-20",
                No = "3",
                StartAirPort = "上海",
                EndAirPort = "巴黎"
            };

            Airline A0004 = new Airline()
            {
                ID = "A0004",
                Date = "2016-6-20",
                No = "3",
                StartAirPort = "巴黎",
                EndAirPort = "上海"
            };
            CombinedVoyage C0001 = new CombinedVoyage(A0001, A0002);
            CombinedVoyage C0002 = new CombinedVoyage(A0003, A0004);
        }

    }
}