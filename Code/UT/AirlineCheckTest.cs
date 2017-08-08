using System;
using System.Collections.Generic;

namespace AIForAirline
{
    //航班检查测试
    public static class AirlineCheckTest
    {
        public static void Init()
        {
            //机场关闭
            CheckCondition.AirPortProhibitList.Clear();
            CheckCondition.AirPortProhibitList.Add(
                new AirPortProhibit()
                {
                    StartDate = Utility.FormatDate("2017/3/1"),
                    EndDate = Utility.FormatDate("2017/4/1"),
                    AirPort = "SH",
                    CloseTime = Utility.FormatTime("18:00"),
                    OpenTime = Utility.FormatTime("21:00")
                }
            );
            CheckCondition.AirPortProhibitList.Add(
                new AirPortProhibit()
                {
                    StartDate = Utility.FormatDate("2017/3/1"),
                    EndDate = Utility.FormatDate("2017/4/1"),
                    AirPort = "BJ",
                    CloseTime = Utility.FormatTime("22:00"),
                    OpenTime = Utility.FormatTime("3:00")
                }
            );
            CheckCondition.PlaneProhibitList.Clear();
            CheckCondition.PlaneProhibitList.Add(
                new PlaneProhibit()
                {
                    StartAirPort = "SH",
                    EndAirPort = "BJ",
                    PlaneID = "1"
                }
            );
        }

        public static void CheckAirPortProhibit()
        {
            //航班一览
            //失败：起飞机场关闭
            Airline A0001 = new Airline()
            {
                ID = "A0001",
                StartAirPort = "SH",
                EndAirPort = "BJ",
                StartTime = new DateTime(2017, 3, 15, 20, 0, 0),
                EndTime = new DateTime(2017, 3, 15, 23, 0, 0)
            };
            var problem = CheckCondition.CheckAirLine(A0001);
            Utility.Log("StartTime:" + A0001.StartTime);
            Utility.Log("TakeOffEarlyTime:" + problem.TakeOffBeforeThisTime + " TakeoffLateTime:" + problem.TakeoffAfterThisTime);


            //失败：起飞机场关闭（跨日）
            Airline A0002 = new Airline()
            {
                ID = "A0002",
                StartAirPort = "BJ",
                EndAirPort = "SH",
                StartTime = new DateTime(2017, 3, 14, 23, 30, 0),
                EndTime = new DateTime(2017, 3, 15, 3, 0, 0)
            };
            problem = CheckCondition.CheckAirLine(A0002);
            Utility.Log("StartTime:" + A0002.StartTime);
            Utility.Log("LandEarlyTime:" + problem.TakeOffBeforeThisTime + " LandLateTime:" + problem.TakeoffAfterThisTime);

            Airline A0003 = new Airline()
            {
                ID = "A0003",
                StartAirPort = "BJ",
                EndAirPort = "SH",
                StartTime = new DateTime(2017, 3, 15, 1, 30, 0),
                EndTime = new DateTime(2017, 3, 15, 4, 0, 0)
            };
            problem = CheckCondition.CheckAirLine(A0003);
            Utility.Log("StartTime:" + A0003.StartTime);
            Utility.Log("TakeOffBeforeThisTime:" + problem.TakeOffBeforeThisTime + " TakeoffAfterThisTime:" + problem.TakeoffAfterThisTime);

            //失败：降落机场关闭
            Airline B0001 = new Airline()
            {
                ID = "B0001",
                StartAirPort = "BJ",
                EndAirPort = "SH",
                StartTime = new DateTime(2017, 3, 15, 15, 0, 0),
                EndTime = new DateTime(2017, 3, 15, 19, 0, 0)
            };
            problem = CheckCondition.CheckAirLine(B0001);
            Utility.Log("EndTime:" + B0001.EndTime);
            Utility.Log("TakeOffBeforeThisTime:" + problem.TakeOffBeforeThisTime + " TakeoffAfterThisTime:" + problem.TakeoffAfterThisTime);


            //失败：降落机场关闭（跨日，次日）
            Airline B0002 = new Airline()
            {
                ID = "B0002",
                StartAirPort = "SH",
                EndAirPort = "BJ",
                StartTime = new DateTime(2017, 3, 14, 22, 30, 0),
                EndTime = new DateTime(2017, 3, 15, 1, 0, 0)
            };
            problem = CheckCondition.CheckAirLine(B0002);
            Utility.Log("EndTime:" + B0002.EndTime);
            Utility.Log("TakeOffBeforeThisTime:" + problem.TakeOffBeforeThisTime + " TakeoffAfterThisTime:" + problem.TakeoffAfterThisTime);

            //失败：降落机场关闭（跨日，当日）
            Airline B0003 = new Airline()
            {
                ID = "B0003",
                StartAirPort = "SH",
                EndAirPort = "BJ",
                StartTime = new DateTime(2017, 3, 14, 21, 30, 0),
                EndTime = new DateTime(2017, 3, 14, 23, 0, 0)
            };
            problem = CheckCondition.CheckAirLine(B0003);
            Utility.Log("EndTime:" + B0003.EndTime);
            Utility.Log("TakeOffBeforeThisTime:" + problem.TakeOffBeforeThisTime + " TakeoffAfterThisTime:" + problem.TakeoffAfterThisTime);
        }

        public static void PlaneProhibit()
        {
            //飞机航线限制
            //失败
            Airline B0001 = new Airline()
            {
                ID = "B0001",
                StartAirPort = "SH",
                EndAirPort = "BJ",
                StartTime = new DateTime(2017, 3, 15, 8, 0, 0),
                EndTime = new DateTime(2017, 3, 15, 11, 0, 0),
                ModifiedPlaneID = "1"
            };
            CheckCondition.CheckAirLine(B0001);

            //正常
            Airline B0002 = new Airline()
            {
                ID = "B0002",
                StartAirPort = "SH",
                EndAirPort = "BJ",
                StartTime = new DateTime(2017, 3, 15, 8, 0, 0),
                EndTime = new DateTime(2017, 3, 15, 11, 0, 0),
                ModifiedPlaneID = "2"
            };
            CheckCondition.CheckAirLine(B0002);
        }

        public static void GetAirlineListByPlaneId()
        {
            Airline A0001 = new Airline()
            {
                ID = "A0001",
                ModifiedPlaneID = "1"
            };
            Airline A0002 = new Airline()
            {
                ID = "A0002",
                ModifiedPlaneID = "1"
            };
            Airline C0001 = new Airline()
            {
                ID = "C0001",
                ModifiedPlaneID = "3"
            };
            Airline B0001 = new Airline()
            {
                ID = "B0001",
                ModifiedPlaneID = "2"
            };
            Airline A0003 = new Airline()
            {
                ID = "A0003",
                ModifiedPlaneID = "1"
            };
            Airline B0002 = new Airline()
            {
                ID = "B0002",
                ModifiedPlaneID = "2"
            };
            List<Airline> AirlineList = new List<Airline>();
            AirlineList.Add(A0002);
            AirlineList.Add(A0001);
            AirlineList.Add(C0001);
            AirlineList.Add(B0002);
            AirlineList.Add(A0003);
            AirlineList.Add(B0001);

            foreach (var item in AirlineList)
            {
                Solution.AirlineDic.Add(item.ID, item);
            }
            Solution.GetAirlineDicByPlaneId();
            foreach (var item in AirlineList)
            {
                Utility.Log(item.ID + " Pre" + item.PreviousAirline?.ID + " Next" + item.NextAirLine?.ID);
            }

        }
    }
}