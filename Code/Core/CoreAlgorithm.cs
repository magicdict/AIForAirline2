using System;
using System.Collections.Generic;

namespace AIForAirline
{
    //核心算法
    public static partial class CoreAlgorithm
    {
        //调整航班
        public static bool AdjustAirLineList(List<Airline> PlaneAirlineList)
        {
            //按照飞机进行调整
            //故障情况必须边调整边检查
            for (int i = 0; i < PlaneAirlineList.Count; i++)
            {
                var airline = PlaneAirlineList[i];
                if (airline.FixMethod == enmFixMethod.Cancel) continue;
                if (airline.ComboAirline == null ||
                   (airline.ComboAirline != null && !airline.IsFirstCombinedVoyage))
                {
                    //普通
                    Problem problem = GetProblem(airline);
                    if (problem.DetailType == ProblemType.None && !problem.IsNotEnoughStayAirportTime) continue;
                    //这里还需要判断一下，是否Problem已经存在，存在的原因是以前的航班修复时候，可能修改了FixMethod
                    //这里应该将问题和修改方式分离开来
                    airline.Problem = problem;
                    switch (problem.DetailType)
                    {
                        case ProblemType.TyphoonTakeOff:
                            //台风引起的无法起飞，可以提前起飞！
                            if (!FixTyphoonTakeOffByChangeTakeOffTime(airline))
                            {
                                if (!FixAirportProblemByChangeTakeOffTime(PlaneAirlineList, i, airline))
                                {
                                    Utility.Log("[无法修复]航班ID：" + airline.ID);
                                    return false;
                                }
                            }
                            break;
                        case ProblemType.AirportProhibitTakeOff:
                        case ProblemType.AirportProhibitLand:
                        case ProblemType.TyphoonLand:
                            //无法提前的情况
                            //在链式调整中，problem会被赋予航班，但是实际是没有问题的
                            if (!FixAirportProblemByChangeTakeOffTime(PlaneAirlineList, i, airline))
                            {
                                Utility.Log("[无法修复]航班ID：" + airline.ID);
                                return false;
                            }
                            break;
                        case ProblemType.AirLinePlaneProhibit:
                            //航班航线限制(无法修复)
                            return false;
                        case ProblemType.TyphoonStay:
                            if (CheckCondition.TyphoonAirportRemain[airline.StartAirPort] != 0 && Utility.IsUseTyphoonStayRoom)
                            {
                                CheckCondition.TyphoonAirportRemain[airline.StartAirPort]--;
                                airline.IsUseTyphoonRoom = true;
                            }
                            else
                            {
                                Utility.Log("[无法修复]航班ID：" + airline.ID);
                                return false;
                            }
                            break;
                        default:
                            //这个IsNotEnoughStayAirportTime其实不需要
                            if (problem.IsNotEnoughStayAirportTime)
                            {
                                if (!FixAirportProblemByChangeTakeOffTime(PlaneAirlineList, i, airline))
                                {
                                    Utility.Log("[无法修复]航班ID：" + airline.ID);
                                    return false;
                                }
                            }
                            break;
                    }
                }
                else
                {
                    //联程的时候，本航班和下一个航班都为空时才继续
                    var first = airline;
                    var second = airline.NextAirLine;
                    var firstProblem = GetProblem(first);
                    var secondProblem = GetProblem(second);
                    var IsFirstNeedFix = (firstProblem.DetailType != ProblemType.None || firstProblem.IsNotEnoughStayAirportTime) &&
                                         (first.FixMethod != enmFixMethod.Cancel && first.FixMethod != enmFixMethod.EmptyFly);

                    var IsSecondNeedFix = (secondProblem.DetailType != ProblemType.None || secondProblem.IsNotEnoughStayAirportTime) &&
                                         (second.FixMethod != enmFixMethod.Cancel && second.FixMethod != enmFixMethod.EmptyFly);
                    if (IsFirstNeedFix || IsSecondNeedFix)
                    {
                        first.Problem = firstProblem;
                        second.Problem = secondProblem;
                        //拉直航班的处理：
                        if (!FixCombinedVoyage(i, PlaneAirlineList))
                        {
                            Utility.Log("[无法修复]联程航班ID：" + airline.ID);
                            return false;

                        }
                    }
                    i++; //处理联程后续航班
                }
            }
            return true;
        }

        private static Problem GetProblem(Airline airline)
        {
            //机场限制和航班限制
            var problem = CheckCondition.CheckAirLine(airline);
            //限制没有问题，则检查故障表
            var TyphoonProblem = CheckCondition.IsExistTyphoon(airline);

            if (airline.PreviousAirline != null)
            {
                if (airline.PreviousAirline.FixMethod == enmFixMethod.EmptyFly ||
                   (airline.ModifiedPlaneID != null && airline.PlaneID != null && !airline.ModifiedPlaneID.Equals(airline.PlaneID)))
                {
                    //向上查找第一个不是取消的航班
                    var PreviousNotCancel = airline.PreviousAirline;
                    while (PreviousNotCancel.FixMethod == enmFixMethod.Cancel)
                    {
                        if (PreviousNotCancel.PreviousAirline != null)
                            PreviousNotCancel = PreviousNotCancel.PreviousAirline;
                    }
                    //从这个开始的最后一个不是取消的航班
                    //检查一下是否过站时间有问题
                    if (airline.ModifiedStartTime.Subtract(PreviousNotCancel.ModifiedEndTime).TotalMinutes < Utility.StayAtAirPortMinutes)
                    {
                        problem.IsNotEnoughStayAirportTime = true;
                        problem.TakeoffAfterThisTimeFixStayTime = PreviousNotCancel.ModifiedEndTime.AddMinutes(Utility.StayAtAirPortMinutes);
                    }
                }
            }

            //检查是否同时有两个问题
            if (problem.DetailType != ProblemType.None && TyphoonProblem.DetailType != ProblemType.None)
            {
                //台风提早起飞的6小时检查时候，可能遇见台风和
                problem.DetailType = ProblemType.AirPortTyphoonMix;
                return problem;
            };
            if (TyphoonProblem.DetailType != ProblemType.None)
            {
                TyphoonProblem.IsNotEnoughStayAirportTime = problem.IsNotEnoughStayAirportTime;
                TyphoonProblem.TakeoffAfterThisTimeFixStayTime = problem.TakeoffAfterThisTimeFixStayTime;
                return TyphoonProblem;
            }
            return problem;
        }
    }
}