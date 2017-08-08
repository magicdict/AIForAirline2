using System;

namespace AIForAirline
{
    //联程
    [Serializable]
    public class CombinedVoyage
    {
        //日期
        public string Date;
        //航班号
        public string No;
        //起飞机场
        public string StartAirPort;
        //中转
        public string TransformAirPort;
        //到达机场
        public string EndAirPort;

        //重要系数
        public double ImportFac
        {
            get
            {
                //统计联程拉直时，将两个重要度相加
                return First.ImportFac + Second.ImportFac;
            }
        }
        //起飞时间(第一段)
        public DateTime StartTimeFirst;
        //到达时间(第一段)
        public DateTime EndTimeFirst;
        //起飞时间(第二段)
        public DateTime StartTimeSecond;
        //到达时间(第二段)
        public DateTime EndTimeSecond;
        //飞机ID
        public string PlaneID;
        //机型
        public string PlaneType;


        //直飞后降落时间
        public DateTime EndTimeDirect
        {
            get
            {
                //计算两段时间的合计
                var key = PlaneType + int.Parse(StartAirPort).ToString("D2") + int.Parse(EndAirPort).ToString("D2");
                if (Solution.FlyTimeDic.ContainsKey(key))
                {
                    var TotalTimeUsage = Solution.FlyTimeDic[key];
                    return StartTimeFirst.AddMinutes(TotalTimeUsage);
                }
                else
                {
                    if (Utility.PrintInfo) Utility.Log("直航时间未知：机型[" + PlaneType + "]起飞机场:[" + StartAirPort + "]降落机场:[" + EndAirPort + "]");
                    //去除中间停留时间即可
                    var StopTimeUsage = StartTimeSecond.Subtract(EndTimeFirst);
                    return EndTimeSecond.Subtract(StopTimeUsage);
                }
            }
        }

        public bool CanChangeDirect
        {
            get
            {
                //仅两段都是国内的联程航班可以拉直。
                //当且仅当中间机场受影响时可拉直航班。
                if (First.InterKbn == "国内" && Second.InterKbn == "国内")
                {
                    if (First.Problem != null && (First.Problem.DetailType == ProblemType.TyphoonLand)) return true;
                    if (Second.Problem != null && (Second.Problem.DetailType == ProblemType.TyphoonTakeOff)) return true;
                }
                return false;
            }
        }

        //联程转直航
        public Airline DirectAirLine;

        public Airline First;

        public Airline Second;

        //两个航班生成联程
        public CombinedVoyage(Airline first, Airline second)
        {
            //ComboAirLineKey Check
            if (!first.ComboAirLineKey.Equals(second.ComboAirLineKey))
            {
                if (Utility.IsDebugMode)
                {
                    Utility.Log("ComboAirLineKey Not Match:[" + first.No + "] VS [" + second.No + "]");
                }
                else
                {
                    throw new Exception("ComboAirLineKey Not Match!");
                }
            }
            First = first;
            Second = second;
            Date = first.Date;
            No = first.No;
            StartAirPort = first.StartAirPort;
            TransformAirPort = first.EndAirPort;
            EndAirPort = second.EndAirPort;
            StartTimeFirst = first.StartTime;
            EndTimeFirst = first.EndTime;
            StartTimeSecond = second.StartTime;
            EndTimeSecond = second.EndTime;
            PlaneType = first.PlaneType;
            PlaneID = first.ModifiedPlaneID;
            //仅两段都是国内的联程航班可以拉直。
            //中间机场台风问题可以拉直
            DirectAirLine = new Airline
            {
                StartAirPort = StartAirPort,
                EndAirPort = EndAirPort,
                StartTime = StartTimeFirst,
                //EndTime已经是经过计算的
                EndTime = EndTimeDirect,
                ModifiedStartTime = StartTimeFirst,
                PlaneID = PlaneID,
                ModifiedPlaneID = PlaneID,
                PlaneType = PlaneType
            };
        }
    }
}