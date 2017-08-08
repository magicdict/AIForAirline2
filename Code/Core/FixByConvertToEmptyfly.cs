using System.Collections.Generic;
using System;

namespace AIForAirline
{
    /// <summary>
    /// 核心算法
    /// </summary>
    public static partial class CoreAlgorithm
    {
        //是否能将某个航班改为调机航班，减少多余的取消航班
        public static bool FixByConvertToEmptyFly(List<Airline> AirlineList, bool IsTry)
        {
            int StartIndex, EndIndex;
            var range = GetTyphoonRange(AirlineList);
            StartIndex = range.StartIndex;
            EndIndex = StartIndex + 1;
            //StartIndex 是受   台风影响的航班
            //EndIndex   是不受 台风影响的航班！
            if (EndIndex == -1 || StartIndex == -1) return false;
            if (CheckCondition.TyphoonAirport.Contains(AirlineList[StartIndex].StartAirPort))
            {
                //转为空飞
                int StartIndexClone = StartIndex;
                int EndIndexClone = EndIndex;
                if (IsCanEmptyFly(AirlineList, ref StartIndex, ref EndIndex))
                {
                    if (StartIndexClone != StartIndex || EndIndexClone != EndIndex) return false;
                    var FirstAirline = AirlineList[StartIndex];
                    var EmptyFly = Utility.DeepCopy(FirstAirline);
                    //修复空飞
                    EmptyFly.FixMethod = enmFixMethod.EmptyFly;
                    EmptyFly.Problem = GetProblem(FirstAirline);
                    EmptyFly.StartTime = EmptyFly.Problem.TakeOffBeforeThisTime;
                    var key = EmptyFly.PlaneType + int.Parse(EmptyFly.StartAirPort).ToString("D2") +
                                                   int.Parse(EmptyFly.EndAirPort).ToString("D2");
                    EmptyFly.EndTime = EmptyFly.StartTime.AddMinutes(Solution.FlyTimeDic[key]);
                    EmptyFly.ModifiedStartTime = EmptyFly.Problem.TakeOffBeforeThisTime;
                    if (!IsTry)
                    {
                        EmptyFlyList.Add(EmptyFly);
                    }
                    AirlineList[StartIndex].FixMethod = enmFixMethod.Cancel;
                    return AdjustAirLineList(AirlineList);
                }
            }
            return false;
        }
    }
}