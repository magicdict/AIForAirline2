using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AIForAirline
{
    public static partial class Solution
    {
        //签转处理
        public static void TransferGuest(List<Airline> PlaneAirlineList)
        {
            //取消和换机人员的签转
            for (int i = 0; i < PlaneAirlineList.Count; i++)
            {
                //寻找取消的航班
                if (PlaneAirlineList[i].FixMethod == enmFixMethod.Cancel ||
                    PlaneAirlineList[i].FixMethod == enmFixMethod.ChangePlane)
                {
                    //分析该机场的其他航班是否可以签转
                    //TODO：签转是否能够提前
                    var airline = PlaneAirlineList[i];
                    int NeedForTrans = PlaneAirlineList[i].FixMethod == enmFixMethod.Cancel ?
                                       airline.CancelUnAssignedGuestCnt : airline.PlaneChangeUnAssignedGuestCnt;
                    if (NeedForTrans == 0) continue;
                    //寻找同一机场起飞时间在实际起飞时间之后的航班
                    TryTransFer(airline, NeedForTrans);
                }
            }
            //中转失败的签转
            for (int i = 0; i < TransTimeList.Count; i++)
            {
                var transTime = TransTimeList[i];
                var LandAirline = AirlineDic[transTime.LandAirlineID];
                var TakeOffAirline = AirlineDic[transTime.TakeOffAirlineID];
                if (LandAirline.FixMethod == enmFixMethod.Cancel ||
                    TakeOffAirline.FixMethod == enmFixMethod.Cancel) continue;
                var StayTime = TakeOffAirline.ModifiedStartTime.Subtract(LandAirline.ModifiedEndTime);
                if (StayTime.TotalMinutes <= transTime.TransferTime) continue;
                TryTransFer(TakeOffAirline, transTime.GuestCnt);
            }
        }
        //进行签转
        private static void TryTransFer(Airline airline, int NeedForTrans)
        {
            foreach (var targetAirline in Solution.AirlineDic.Values)
            {
                if (targetAirline.StartAirPort != airline.StartAirPort ||
                    targetAirline.EndAirPort != airline.EndAirPort) continue;         //相同起始机场
                if (targetAirline.ID == airline.ID) continue;                         //去除自己
                if (targetAirline.FixMethod != enmFixMethod.UnFixed &&
                    targetAirline.FixMethod != enmFixMethod.ChangePlane) continue;    //没有调整或者单纯换机型
                if (targetAirline.EmptySeatCnt == 0) continue;                        //是否有空座位
                if (targetAirline.ModifiedStartTime < airline.StartTime) continue;    //签转到后续航班
                if (targetAirline.ModifiedStartTime.Subtract(airline.StartTime).TotalMinutes > Utility.DelayInternationalMaxMinute) continue;
                if (targetAirline.EmptySeatCnt >= NeedForTrans)
                {
                    targetAirline.ReceiveTransList.Add(new Airline.TransInfo()
                    {
                        AirlineID = airline.ID,
                        GuestCnt = NeedForTrans
                    });
                    airline.SendTransList.Add(new Airline.TransInfo()
                    {
                        AirlineID = targetAirline.ID,
                        GuestCnt = NeedForTrans
                    });
                    break;
                }
                else
                {
                    //EmptySeatCnt是计算属性，所以这里必须使用变量
                    var transCnt = targetAirline.EmptySeatCnt;
                    targetAirline.ReceiveTransList.Add(new Airline.TransInfo()
                    {
                        AirlineID = airline.ID,
                        GuestCnt = transCnt
                    });
                    airline.SendTransList.Add(new Airline.TransInfo()
                    {
                        AirlineID = targetAirline.ID,
                        GuestCnt = transCnt
                    });
                }
            }
        }
    }
}