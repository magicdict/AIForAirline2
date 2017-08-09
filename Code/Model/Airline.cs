using System;
using System.Collections.Generic;
using System.Linq;

namespace AIForAirline
{
    //航班表
    [Serializable]
    public class Airline
    {
        //航班ID
        public string ID;
        //日期
        public string Date;
        //国际/国内(无意义项目)
        public string InterKbn;
        //航班号
        public string No;
        //起飞机场
        public string StartAirPort;
        //到达机场
        public string EndAirPort;
        //起飞时间
        public DateTime StartTime;
        //到达时间
        public DateTime EndTime;
        //飞机ID
        public string PlaneID;
        //修改后的飞机ID
        public string ModifiedPlaneID;

        //机型
        public string PlaneType;

        public string ModifiedPlaneType;

        public int GuestCnt;

        public int CombinedVoyageGuestCnt;

        public int SeatCnt
        {
            get
            {
                return Solution.PlaneTypeSeatCntDic[ModifiedPlaneType];
            }
        }

        //重要系数(空缺时默认是1.0)
        public double ImportFac;

        //故障类型
        public Problem Problem;

        //联程（如果非联程，则为空）
        public CombinedVoyage ComboAirline;
        //是否为联程的第一段
        public bool IsFirstCombinedVoyage;

        //执飞该航班的飞机，上一个执飞航班
        public Airline PreviousAirline;
        //执飞该航班的飞机，下一个执飞航班
        public Airline NextAirLine;

        public enmFixMethod FixMethod = enmFixMethod.UnFixed;
        //修改起飞时间
        public bool IsChangeStartTime
        {
            get
            {
                return StartTime != ModifiedStartTime;
            }
        }

        DateTime _ModifiedStartTime;
        DateTime _ModifiedEndTime;

        //起飞时间（修改后）
        public DateTime ModifiedStartTime
        {
            get
            {
                return _ModifiedStartTime;
            }
            set
            {
                //需要同时修改结束时间
                _ModifiedStartTime = value;
                _ModifiedEndTime = value.Add(FlightSpan);
            }
        }

        //本航班起飞前计划停留时间（过站时间）
        //原计划过站时间可能不满50分钟
        public int StayBeforeTakeOffTimeMinutes
        {
            get
            {
                if (PreviousAirline != null) return (int)StartTime.Subtract(PreviousAirline.EndTime).TotalMinutes;
                return 0;
            }
        }

        //降落时间（修改后）
        public DateTime ModifiedEndTime
        {
            get
            {
                return _ModifiedEndTime;
            }
        }


        //飞行时间
        public TimeSpan FlightSpan
        {
            get
            {
                return EndTime.Subtract(StartTime);
            }
        }


        //联程航班判别主键
        public string ComboAirLineKey
        {
            get
            {
                //日期和航班号相同的两个航班是联程航班
                return Date + No;
            }
        }


        public bool IsTyphoonLandFixable
        {
            get
            {
                //在台风无法降落的情况下，唯一的方法就是推迟起飞时间，但是有些航班推迟到最大极限
                //国内24小时，国际36小时，也无法降落的，这样的航班如果还是不能联程的，则无法修复
                if (CheckCondition.TyphoonAirport.Contains(EndAirPort))
                {
                    foreach (var item in CheckCondition.TyphoonList)
                    {
                        if (EndAirPort == item.AirPort && item.TroubleType == "降落")
                        {
                            if (item.IsInRange(ModifiedEndTime))
                            {
                                //无法降落
                                int MaxDelayTime = (InterKbn == "国内" ? Utility.DelayDemasticMaxMinute : Utility.DelayInternationalMaxMinute);
                                var LateLandTime = ModifiedStartTime.Add(FlightSpan).AddMinutes(MaxDelayTime);
                                if (LateLandTime < item.EndTime) return false;
                            }
                            break;
                        }
                    }
                }
                return true;
            }
        }


        public bool IsTyphoonTakeOffFixable
        {
            get
            {
                //在台风无法起飞的情况下，可以选择提前起飞或者延迟起飞
                if (CheckCondition.TyphoonAirport.Contains(StartAirPort))
                {
                    foreach (var item in CheckCondition.TyphoonList)
                    {
                        if (StartAirPort == item.AirPort && item.TroubleType == "起飞")
                        {
                            if (item.IsInRange(ModifiedStartTime))
                            {
                                //无法降落
                                int MaxDelayTime = (InterKbn == "国内" ? Utility.DelayDemasticMaxMinute : Utility.DelayInternationalMaxMinute);
                                var LateTakeOff = ModifiedStartTime.AddMinutes(MaxDelayTime);
                                var EarlyTakeOff = ModifiedStartTime.AddMinutes(-Utility.EarlyMaxMinute);
                                if (PreviousAirline != null)
                                {
                                    var StayEarly = PreviousAirline.ModifiedEndTime.AddMinutes(Utility.StayAtAirPortMinutes);
                                    if (StayEarly > EarlyTakeOff)
                                    {
                                        EarlyTakeOff = StayEarly;
                                    }
                                }
                                if (LateTakeOff < item.EndTime && EarlyTakeOff > item.StartTime) return false;
                            }
                            break;
                        }
                    }
                }
                return true;
            }
        }

        //恢复窗口限制
        public bool IsAllowAdjust
        {
            get
            {
                return StartTime >= Utility.RecoverStart && StartTime <= Utility.RecoverEnd;
            }
        }

        //空座位数
        public int EmptySeatCnt
        {
            get
            {
                //座位数 - 旅客数 - 联程旅客数 - 签转旅客数（转入） 
                return SeatCnt - GuestCnt - CombinedVoyageGuestCnt - ReceiveEndorseList.Sum(x => x.GuestCnt);
            }
        }

        //中转列表(转出)
        public List<EndorseInfo> SendEndorseList = new List<EndorseInfo>();
        //中转列表(转入)
        public List<EndorseInfo> ReceiveEndorseList = new List<EndorseInfo>();

        //如果取消该航班，同时发生签转，实际未安排人数
        public int CancelUnAssignedGuestCnt
        {
            get
            {
                return GuestCnt + CombinedVoyageGuestCnt - SendEndorseList.Sum(x => x.GuestCnt);
            }
        }

        //如果换机型，同时发生签转，实际未安排人数
        public int PlaneChangeUnAssignedGuestCnt
        {
            get
            {
                if (SeatCnt >= GuestCnt) return 0;
                return GuestCnt - SeatCnt - SendEndorseList.Sum(x => x.GuestCnt);
            }
        }

        //中转信息
        [Serializable]
        public struct EndorseInfo
        {
            //航班
            public string AirlineID;
            //人数
            public int GuestCnt;
            //ToString
            public override string ToString()
            {
                return AirlineID + ":" + GuestCnt;
            }
        }

        public Airline() { }
        //从文本初始化
        public Airline(string RawData)
        {
            var RawDataArray = RawData.Split(",".ToCharArray());
            ID = RawDataArray[0];
            Date = RawDataArray[1];
            InterKbn = RawDataArray[2];
            No = RawDataArray[3];
            StartAirPort = RawDataArray[4];
            EndAirPort = RawDataArray[5];
            StartTime = DateTime.Parse(RawDataArray[6]);
            EndTime = DateTime.Parse(RawDataArray[7]);
            PlaneID = RawDataArray[8];
            ModifiedPlaneID = PlaneID;
            PlaneType = RawDataArray[9];
            ModifiedPlaneType = PlaneType;
            GuestCnt = int.Parse(RawDataArray[10]);
            CombinedVoyageGuestCnt = int.Parse(RawDataArray[11]);
            //座位数根据机型获得
            if (!Solution.PlaneTypeSeatCntDic.ContainsKey(PlaneType))
                Solution.PlaneTypeSeatCntDic.Add(PlaneType, int.Parse(RawDataArray[12]));
            ImportFac = double.Parse(RawDataArray[13]);
            Problem = null;
            PreviousAirline = null;
            NextAirLine = null;
            ComboAirline = null;
            _ModifiedStartTime = StartTime;
            _ModifiedEndTime = EndTime;
        }

        //CSV输出用字符串
        public override string ToString()
        {
            string[] csv = new string[] {
                ID,Date, InterKbn, No, StartAirPort, EndAirPort,
                StartTime.ToString() , EndTime.ToString(),PlaneID,PlaneType, ImportFac.ToString(),
                StayBeforeTakeOffTimeMinutes.ToString()
            };
            return string.Join(",", csv);
        }
    }
}