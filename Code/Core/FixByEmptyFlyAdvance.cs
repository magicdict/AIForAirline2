using System.Collections.Generic;
using System;

namespace AIForAirline
{
    /// <summary>
    /// 核心算法
    /// </summary>
    public static partial class CoreAlgorithm
    {
        public static int GetFixByEmptyFlyAdvanced(List<Airline> AirlineList)
        {
            int StartIndex, EndIndex;
            var range = GetTyphoonRange(AirlineList);
            StartIndex = range.StartIndex;
            EndIndex = range.EndIndex;
            //StartIndex 是受   台风影响的航班
            //EndIndex   是不受 台风影响的航班！
            if (EndIndex == -1 || StartIndex == -1) return -1;
            var MinScore = double.MaxValue;
            var MinEndIndex = -1;
            //空飞航班起点在 StartIndex，终点在 EndIndex之前是否也可以？
            for (int NewEndIndex = StartIndex + 1; NewEndIndex < EndIndex; NewEndIndex++)
            {
                var StartIndexClone = StartIndex;
                var EndIndexClone = NewEndIndex;
                if (IsCanEmptyFly(AirlineList, ref StartIndexClone, ref EndIndexClone))
                {
                    if (StartIndex != StartIndexClone || NewEndIndex != EndIndexClone) continue;
                    //可以直飞
                    //Console.WriteLine("Try EmptyFly" + AirlineList[StartIndex].ID + "," + AirlineList[NewEndIndex].ID);
                    //尝试状态
                    var AirlineListClone = Utility.DeepCopy(AirlineList);
                    var EmptyFly = GetEmptyFly(AirlineListClone, StartIndex, NewEndIndex);
                    for (int i = StartIndex; i < NewEndIndex; i++)
                    {
                        AirlineListClone[i].FixMethod = enmFixMethod.Cancel;
                    }
                    if (CoreAlgorithm.AdjustAirLineList(AirlineListClone))
                    {
                        var CurrentResult = Statistics.WriteResult(AirlineListClone) + Statistics.EmptyFlyParm;
                        if (MinScore > CurrentResult)
                        {
                            MinScore = CurrentResult;
                            MinEndIndex = NewEndIndex;
                        }
                    }
                }
            }
            return MinEndIndex;
        }


        public static double FixByEmptyFlyAdvanced(List<Airline> AirlineList, int BestEndIndex, bool IsTry)
        {
            int StartIndex, NewEndIndex;
            var range = GetTyphoonRange(AirlineList);
            StartIndex = range.StartIndex;
            NewEndIndex = BestEndIndex;
            var EmptyFly = GetEmptyFly(AirlineList, StartIndex, NewEndIndex);
            for (int i = StartIndex; i < NewEndIndex; i++)
            {
                AirlineList[i].FixMethod = enmFixMethod.Cancel;
            }
            CoreAlgorithm.AdjustAirLineList(AirlineList);
            if (!IsTry)
            {
                EmptyFlyList.Add(EmptyFly);
            }
            return Statistics.WriteResult(AirlineList) + (IsTry ? Statistics.EmptyFlyParm : 0);
        }


        public static (int StartCancelIndex, int EndCancelIndex) GetCancelSomeSectionIndex(List<Airline> AirlineList)
        {
            int StartIndex, EndIndex;
            var range = GetTyphoonRange(AirlineList);
            StartIndex = range.StartIndex;
            EndIndex = range.EndIndex;
            //StartIndex 是受   台风影响的航班
            //EndIndex   是不受 台风影响的航班！
            if (EndIndex == -1 || StartIndex == -1) return (-1, -1);
            var MinScore = double.MaxValue;
            var MinStartEndIndex = (-1, -1);
            //台风外的3个，台风内的5个，进行双循环匹配
            for (int StIdx = Math.Max(1, StartIndex - 5); StIdx < StartIndex; StIdx++)
            {
                for (int EdIdx = StartIndex + 1; EdIdx < Math.Min(AirlineList.Count, StartIndex + 8); EdIdx++)
                {
                    if (AirlineList[StIdx].FixMethod == enmFixMethod.Cancel ||
                        AirlineList[EdIdx].FixMethod == enmFixMethod.Cancel) continue;
                    if (AirlineList[StIdx].EndAirPort.Equals(AirlineList[EdIdx].StartAirPort))
                    {
                        if (!CheckCondition.TyphoonAirport.Contains(AirlineList[StIdx].EndAirPort))
                        {
                            var AirlineListClone = Utility.DeepCopy(AirlineList);
                            for (int CancelIdx = StIdx + 1; CancelIdx < EdIdx; CancelIdx++)
                            {
                                AirlineListClone[CancelIdx].FixMethod = enmFixMethod.Cancel;
                            }
                            if (CoreAlgorithm.AdjustAirLineList(AirlineListClone))
                            {
                                var CurrentResult = Statistics.WriteResult(AirlineListClone);
                                if (MinScore > CurrentResult)
                                {
                                    MinScore = CurrentResult;
                                    MinStartEndIndex = (StIdx, EdIdx);
                                }
                            }
                        }
                    }
                }
            }
            return MinStartEndIndex;
        }

        public static double FixByCancelSomeSection(List<Airline> AirlineList, (int StIdx, int EdIdx) Best)
        {
            for (int CancelIdx = Best.StIdx + 1; CancelIdx < Best.EdIdx; CancelIdx++)
            {
                AirlineList[CancelIdx].FixMethod = enmFixMethod.Cancel;
            }
            CoreAlgorithm.AdjustAirLineList(AirlineList);
            return Statistics.WriteResult(AirlineList);
        }

    }
}
