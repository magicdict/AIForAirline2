using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AIForAirline
{
    public static partial class CoreAlgorithm
    {
        static ExchangeRecord ExchangeEvaluateAdvance(string firstPlaneId, string secondPlandId)
        {
            ExchangeRecord Rtn = new ExchangeRecord();
            Rtn.DiffScore = 0;

            List<Airline> first = Solution.PlaneIdAirlineDic[firstPlaneId];
            List<Airline> second = Solution.PlaneIdAirlineDic[secondPlandId];
            //测试一下当前分数：
            double FirstScore = scoreDic[firstPlaneId];
            double SecondScore = scoreDic[secondPlandId];
            var TotalScore = FirstScore + SecondScore;
            var FirstTyphoonRange = CoreAlgorithm.GetTyphoonRange(first);
            var SecondTyphonnRange = CoreAlgorithm.GetTyphoonRange(second);
            for (int firstIdx = Math.Max(FirstTyphoonRange.StartIndex - 2, 2); firstIdx < FirstTyphoonRange.EndIndex; firstIdx++)
            {
                //首航班不调整
                if (firstIdx == 0) continue;
                //是否存在无法降落的航班
                var firstAirline = first[firstIdx];

                //相同起飞机场的检查
                var FocusAirport = firstAirline.StartAirPort;
                for (int secondIdx = Math.Max(SecondTyphonnRange.StartIndex - 2, 2); secondIdx < SecondTyphonnRange.EndIndex; secondIdx++)
                {
                    //首航班不调整
                    if (secondIdx == 0) continue;
                    var secondAirline = second[secondIdx];
                    if (secondAirline.StartAirPort != FocusAirport) continue;
                    //联程不能换飞机(但是可以同时换两个)
                    if ((first[firstIdx].ComboAirline != null && !first[firstIdx].IsFirstCombinedVoyage) ||
                         second[secondIdx].ComboAirline != null && !second[secondIdx].IsFirstCombinedVoyage) continue;

                    var FirstAirlineListClone = Utility.DeepCopy(first);
                    var SecondAirlineListClone = Utility.DeepCopy(second);
                    //时间测试(起飞关系不能乱,等于也不可以，防止前后航班关系错乱)
                    var TakeoffAfterThisTime = SecondAirlineListClone[secondIdx - 1].ModifiedEndTime.AddMinutes(Utility.StayAtAirPortMinutes);
                    var AddTime = TakeoffAfterThisTime.Subtract(FirstAirlineListClone[firstIdx].ModifiedStartTime);
                    if (AddTime.TotalMinutes > 0)
                    {
                        //尝试是否能够将first整体后移
                        for (int i = firstIdx; i < first.Count; i++)
                        {
                            FirstAirlineListClone[i].ModifiedStartTime += AddTime;
                        }
                    }

                    TakeoffAfterThisTime = FirstAirlineListClone[firstIdx - 1].ModifiedEndTime.AddMinutes(Utility.StayAtAirPortMinutes);
                    AddTime = TakeoffAfterThisTime.Subtract(SecondAirlineListClone[secondIdx].ModifiedStartTime);
                    if (AddTime.TotalMinutes > 0)
                    {
                        //尝试是否能够将first整体后移
                        for (int i = secondIdx; i < second.Count; i++)
                        {
                            SecondAirlineListClone[i].ModifiedStartTime += AddTime;
                        }
                    }
                    if (!Exchange(FirstAirlineListClone, firstIdx, SecondAirlineListClone, secondIdx)) continue;
                    var FirstForTest = new List<Airline>();
                    var SecondForTest = new List<Airline>();
                    foreach (var air in FirstAirlineListClone)
                    {
                        if (air.ModifiedPlaneID == firstPlaneId)
                        {
                            FirstForTest.Add(air);
                        }
                        else
                        {
                            SecondForTest.Add(air);
                        }
                    }
                    foreach (var air in SecondAirlineListClone)
                    {
                        if (air.ModifiedPlaneID == firstPlaneId)
                        {
                            FirstForTest.Add(air);
                        }
                        else
                        {
                            SecondForTest.Add(air);
                        }
                    }
                    //测试一下交换后分数
                    FirstForTest.Sort((x, y) => { return x.ModifiedStartTime.CompareTo(y.ModifiedStartTime); });
                    SecondForTest.Sort((x, y) => { return x.ModifiedStartTime.CompareTo(y.ModifiedStartTime); });
                    var FirstScoreAfter = Solution.FixAirline(FirstForTest, true).Score;
                    if (FirstScoreAfter == double.MaxValue) continue;
                    var SecondScoreAfter = Solution.FixAirline(SecondForTest, true).Score;
                    if (SecondScoreAfter == double.MaxValue) continue;
                    var TotalScoreAfter = FirstScoreAfter + SecondScoreAfter;
                    var Minius = Math.Round(TotalScoreAfter, 0) - Math.Round(TotalScore, 0);
                    Rtn = new ExchangeRecord()
                    {
                        firstIndex = firstIdx,
                        secondIndex = secondIdx,
                        firstPlaneId = firstPlaneId,
                        secondPlaneId = secondPlandId,
                        firstScore = FirstScoreAfter,
                        secondScore = SecondScoreAfter,
                        DiffScore = Minius,
                    };
                    return Rtn;
                }
            }
            return Rtn;
        }
    }
}