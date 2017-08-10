using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;
using System.Diagnostics;

namespace AIForAirline
{
    /// <summary>
    /// 优化结果
    /// </summary>
    public static partial class ResultOptimize
    {
        //单位时间起降限制
        private static void UnitCapLimit()
        {
            foreach (var typhoon in CheckCondition.TyphoonAirport)
            {
                var airlines = AirportIdAirlineDic[typhoon];
                //台风前修复
                //5分钟一个航班
                FixBeforeTyphoon(airlines);
                FixAfterTyphoon(airlines);
            }
        }

        private static void FixAfterTyphoon(List<AirportInfo> airlines)
        {
            var TimeCntDic = new Dictionary<DateTime, int>();
            for (DateTime CheckPoint = Utility.UnitTimeLimitAfterTyphoonStart;
                          CheckPoint <= Utility.UnitTimeLimitAfterTyphoonEnd; CheckPoint = CheckPoint.AddMinutes(5))
            {
                var Cnt = airlines.Count(x =>
                {
                    return x.EventTime == CheckPoint &&
                           x.EventAirline.FixMethod != enmFixMethod.Cancel &&
                           x.EventAirline.FixMethod != enmFixMethod.CancelByDirect;
                });
                TimeCntDic.Add(CheckPoint, Cnt);
            }
        }

        private static void FixBeforeTyphoon(List<AirportInfo> airlines)
        {
            var TimeCntDic = new Dictionary<DateTime, int>();
            for (DateTime CheckPoint = Utility.UnitTimeLimitBeforeTyphoonStart;
                          CheckPoint <= Utility.UnitTimeLimitBeforeTyphoonEnd; CheckPoint = CheckPoint.AddMinutes(5))
            {
                var Cnt = airlines.Count(x =>
                {
                    return x.EventTime == CheckPoint &&
                           x.EventAirline.FixMethod != enmFixMethod.Cancel &&
                           x.EventAirline.FixMethod != enmFixMethod.CancelByDirect;
                });
                TimeCntDic.Add(CheckPoint, Cnt);
            }
            //将主键保存在List中
            var TimeList = TimeCntDic.Keys.ToList();
            TimeList.Sort();
            //在保证足够过站时间的基础上，进行转化,从后向前尝试
            for (int i = TimeList.Count - 1; i >= 0; i--)
            {
                var CheckPoint = TimeList[i];
                var Cnt = TimeCntDic[CheckPoint];
                if (Cnt > 2)
                {
                    //台风前，集中在16点起飞的航班
                    var ReAssignList = airlines.Where(x =>
                    {
                        return x.EventTime == CheckPoint &&
                            x.EventAirline.FixMethod != enmFixMethod.Cancel &&
                            x.EventAirline.FixMethod != enmFixMethod.CancelByDirect;
                    }).ToList();
                    //排序，按照过站时间，最不充裕的最前面,这里都是起飞问题，这个时间段无法降落的
                    ReAssignList.Sort((x, y) =>
                    {
                        return x.EventAirline.ModifiedStartTime.Subtract(x.EventAirline.PreviousAirline.ModifiedEndTime).CompareTo
                              (y.EventAirline.ModifiedStartTime.Subtract(y.EventAirline.PreviousAirline.ModifiedEndTime));
                    });
                    for (int j = 0; j < ReAssignList.Count; j++)
                    {
                        for (int k = TimeList.Count - 1; k >= 0; k--)
                        {
                            if (TimeCntDic[TimeList[k]] < 2)
                            {
                                var CheckEventInfo = ReAssignList[j];
                                var TakeOffTime = CheckEventInfo.EventAirline.ModifiedStartTime;
                                var ModifiedTakeOffTime = TimeList[k];
                                if (ModifiedTakeOffTime.Subtract(CheckEventInfo.EventAirline.PreviousAirline.ModifiedEndTime).TotalMinutes >=
                                                                 Utility.StayAtAirPortMinutes)
                                {
                                    //提前起飞
                                    CheckEventInfo.EventAirline.ModifiedStartTime = ModifiedTakeOffTime;
                                    //修改结束时间
                                    CheckEventInfo.EventAirline.ModifiedEndTime.Subtract(TakeOffTime.Subtract(ModifiedTakeOffTime));
                                    TimeCntDic[TimeList[k]]++;
                                    Console.WriteLine(CheckEventInfo.EventAirline.ID + "调整起飞时间：" + TakeOffTime + "->" + ModifiedTakeOffTime);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}