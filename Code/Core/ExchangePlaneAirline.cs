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
        public static Dictionary<string, double> scoreDic = new Dictionary<string, double>();

        public static double _currentTemperature = 1000000d;

        public static void CheckExchangableAirline()
        {
            double InCrease = 0;
            double Decrease = 0;
            Parallel.ForEach(Solution.PlaneTypeDic.Keys, planetype =>
            {
                var AirlineIDList = Solution.PlaneTypeDic[planetype];
                Parallel.For(0, AirlineIDList.Count, i =>
                {
                    for (int j = 0; j < AirlineIDList.Count; j++)
                    {
                        if (i == j) continue;
                        var firstPlaneId = AirlineIDList[i];
                        var secondPlandId = AirlineIDList[j];
                        var rtn = ExchangeEvaluateAdvance(firstPlaneId, secondPlandId);
                        //好的结果肯定会通过，坏的结果，则一定几率通过，通过几率随着迭代次数越来越下降
                        if (rtn.DiffScore == 0) continue;
                        if (rtn.DiffScore < 0)
                        {
                            //接受更新
                            var firstairline = Solution.PlaneIdAirlineDic[rtn.firstPlaneId];
                            var secondairline = Solution.PlaneIdAirlineDic[rtn.secondPlaneId];
                            PreExchangeForAdvance(rtn, firstairline, secondairline);
                            Exchange(firstairline, rtn.firstIndex, secondairline, rtn.secondIndex);
                            scoreDic[rtn.firstPlaneId] = rtn.firstScore;
                            scoreDic[rtn.secondPlaneId] = rtn.secondScore;
                            _currentTemperature += rtn.DiffScore;
                            Decrease -= rtn.DiffScore;
                        }
                        else
                        {
                            //退火更新
                            Random _rdm = new Random((int)DateTime.Now.Ticks);
                            double probability = Math.Exp(-(rtn.DiffScore / _currentTemperature));
                            //随机指定的正数lambda
                            double lambda = ((double)(_rdm.Next() % 10000)) / 10000d;
                            if (probability > lambda)
                            {
                                //接受更新
                                var firstairline = Solution.PlaneIdAirlineDic[rtn.firstPlaneId];
                                var secondairline = Solution.PlaneIdAirlineDic[rtn.secondPlaneId];
                                PreExchangeForAdvance(rtn, firstairline, secondairline);
                                Exchange(firstairline, rtn.firstIndex, secondairline, rtn.secondIndex);
                                scoreDic[rtn.firstPlaneId] = rtn.firstScore;
                                scoreDic[rtn.secondPlaneId] = rtn.secondScore;
                                _currentTemperature -= rtn.DiffScore;
                                InCrease += rtn.DiffScore;
                            }
                        }
                    }
                });
            });
            Console.WriteLine("减少成本：" + Decrease + " 增加成本：" + InCrease);
        }

        private static void PreExchangeForAdvance(ExchangeRecord score, List<Airline> firstairline, List<Airline> secondairline)
        {
            //时间测试(起飞关系不能乱,等于也不可以，防止前后航班关系错乱)
            var TakeoffAfterThisTime = secondairline[score.secondIndex - 1].ModifiedEndTime.AddMinutes(Utility.StayAtAirPortMinutes);
            var AddTime = TakeoffAfterThisTime.Subtract(firstairline[score.firstIndex].ModifiedStartTime);
            if (AddTime.TotalMinutes > 0)
            {
                //尝试是否能够将first整体后移
                for (int i = score.firstIndex; i < firstairline.Count; i++)
                {
                    firstairline[i].ModifiedStartTime += AddTime;
                }
            }

            TakeoffAfterThisTime = firstairline[score.firstIndex - 1].ModifiedEndTime.AddMinutes(Utility.StayAtAirPortMinutes);
            AddTime = TakeoffAfterThisTime.Subtract(secondairline[score.secondIndex].ModifiedStartTime);
            if (AddTime.TotalMinutes > 0)
            {
                //尝试是否能够将first整体后移
                for (int i = score.secondIndex; i < secondairline.Count; i++)
                {
                    secondairline[i].ModifiedStartTime += AddTime;
                }
            }
        }

        static bool Exchange(List<Airline> first, int firstIndex, List<Airline> second, int secondIndex)
        {
            //firstIndex,secondIndex也是需要交换的
            var firstPlaneId = first.First().ModifiedPlaneID;
            var secondPlandId = second.First().ModifiedPlaneID;
            //1.设定ModifyPlaneID
            for (int i = secondIndex; i < second.Count; i++)
            {
                second[i].ModifiedPlaneID = firstPlaneId;
                if (!CheckCondition.IsAirlinePlaneAvalible(second[i].StartAirPort, second[i].EndAirPort, second[i].ModifiedPlaneID))
                {
                    return false;
                }
            }
            for (int i = firstIndex; i < first.Count; i++)
            {
                first[i].ModifiedPlaneID = secondPlandId;
                if (!CheckCondition.IsAirlinePlaneAvalible(first[i].StartAirPort, first[i].EndAirPort, first[i].ModifiedPlaneID))
                {
                    return false;
                }
            }
            //2.修改交接处的NextAirline和PreviousAirline
            first[firstIndex - 1].NextAirLine = second[secondIndex];
            second[secondIndex - 1].NextAirLine = first[firstIndex];
            first[firstIndex].PreviousAirline = second[secondIndex - 1];
            second[secondIndex].PreviousAirline = first[firstIndex - 1];
            return true;
        }

        [Serializable]
        class ExchangeRecord
        {
            public string firstPlaneId;

            public int firstIndex;

            public string secondPlaneId;

            public int secondIndex;

            public double firstScore;

            public double secondScore;

            public double DiffScore;
        }
    }
}
