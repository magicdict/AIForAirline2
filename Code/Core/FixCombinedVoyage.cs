using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AIForAirline
{
    //核心算法
    public static partial class CoreAlgorithm
    {
        private static bool FixCombinedVoyage(int index, List<Airline> PlaneAirlineList)
        {
            //同时，拉直航程，会出现提前问题
            //使用常规方法进行修复
            Utility.Log("开始修复联程问题-航班号：" + PlaneAirlineList[index].No);
            var StandScore = Statistics.WriteResult(PlaneAirlineList);
            Utility.Log("调整前的分数：" + StandScore);
            bool R1 = true;
            bool R2 = true;
            var CloneAirline = Utility.DeepCopy(PlaneAirlineList);
            Airline first = CloneAirline[index];
            Airline second = first.NextAirLine;
            if (first.Problem != null && (first.Problem.DetailType != ProblemType.None) || first.Problem.IsNotEnoughStayAirportTime)
            {
                Utility.Log("修复前半段:");
                R1 = FixAirportProblemByChangeTakeOffTime(CloneAirline, index, first);
            }
            second.Problem = GetProblem(second);
            if (second.Problem != null && (second.Problem.DetailType != ProblemType.None) || second.Problem.IsNotEnoughStayAirportTime)
            {
                Utility.Log("修复后半段:");
                R2 = FixAirportProblemByChangeTakeOffTime(CloneAirline, index + 1, second);
            }
            var AfterFixScore = Statistics.WriteResult(CloneAirline);
            Utility.Log("调整后的增加分数：" + (AfterFixScore - StandScore));
            CloneAirline = Utility.DeepCopy(PlaneAirlineList);
            first = CloneAirline[index];
            second = first.NextAirLine;
            CombinedVoyage combined = first.ComboAirline;

            if (combined.CanChangeDirect)
            {
                //CanDirect是拉直的前提条件
                //如果决定拉直航程，看一下拉直之后是否存在问题
                var problem = GetProblem(combined.DirectAirLine);
                var ResultDirect = true;
                //存在问题的情况下，起飞和降落是否能调整起飞时间解决问题
                switch (problem.DetailType)
                {
                    case ProblemType.TyphoonTakeOff:
                        //台风引起的无法飞行，可以提前飞行！
                        if (!FixTyphoonTakeOffByChangeTakeOffTime(combined.DirectAirLine))
                        {
                            ResultDirect = false;
                            Utility.Log("[无法修复]联程航班ID：" + first.ID);
                        }
                        else
                        {
                            first.Problem = combined.DirectAirLine.Problem;
                            first.ModifiedStartTime = combined.DirectAirLine.ModifiedStartTime;
                        }
                        break;
                    case ProblemType.AirportProhibitTakeOff:
                    case ProblemType.AirportProhibitLand:
                    case ProblemType.TyphoonLand:
                        //降落（机场限制）具有可用最早最晚降落时间
                        if (!FixByChangeTakeOffTime(combined.DirectAirLine))
                        {
                            ResultDirect = false;
                            Utility.Log("[无法修复]联程航班ID：" + first.ID);
                        }
                        else
                        {
                            first.Problem = combined.DirectAirLine.Problem;
                            first.ModifiedStartTime = combined.DirectAirLine.ModifiedStartTime;
                        }
                        break;
                    case ProblemType.None:
                        break;
                    default:
                        Utility.Log("联程错误:" + problem.DetailType.ToString());
                        ResultDirect = false;
                        break;
                }
                var AfterDirectFixScore = double.MaxValue;
                if (ResultDirect)
                {
                    //第一段航程进行标注：直航
                    if (first.Problem == null) first.Problem = new Problem();
                    first.FixMethod = enmFixMethod.Direct;
                    first.ImportFac = combined.ImportFac;
                    first.EndAirPort = combined.DirectAirLine.EndAirPort;
                    //第二段航程进行标注：取消
                    if (second.Problem == null) second.Problem = new Problem();
                    second.FixMethod = enmFixMethod.CancelByDirect;
                    second.ImportFac = combined.ImportFac;
                    AfterDirectFixScore = Statistics.WriteResult(CloneAirline);
                    Utility.Log("调整后的增加分数：" + (AfterDirectFixScore - StandScore));
                }

                if (R1 && R2 && (!ResultDirect || AfterFixScore < AfterDirectFixScore))
                {
                    //R1,R2成功，并且 拉直失败 或者 常规比较划算
                    if (first.Problem != null && first.Problem.DetailType != ProblemType.None) FixAirportProblemByChangeTakeOffTime(PlaneAirlineList, index, first);
                    if (second.Problem != null && second.Problem.DetailType != ProblemType.None) FixAirportProblemByChangeTakeOffTime(PlaneAirlineList, index + 1, second);
                    return true;
                }
                else
                {
                    //使用拉直
                    if (ResultDirect)
                    {
                        first = PlaneAirlineList[index];
                        second = first.NextAirLine;
                        //第一段航程进行标注：直航
                        if (first.Problem == null) first.Problem = new Problem();
                        first.FixMethod = enmFixMethod.Direct;
                        first.ImportFac = combined.ImportFac;
                        first.EndAirPort = combined.DirectAirLine.EndAirPort;
                        //第二段航程进行标注：取消
                        if (second.Problem == null) second.Problem = new Problem();
                        second.FixMethod = enmFixMethod.CancelByDirect;
                        second.ImportFac = combined.ImportFac;
                        AfterDirectFixScore = Statistics.WriteResult(CloneAirline);
                        return true;
                    }
                }
            }
            else
            {
                if (R1 && R2)
                {
                    //R1,R2成功，并且 拉直失败 或者 常规比较划算
                    if (first.Problem != null && first.Problem.DetailType != ProblemType.None) FixAirportProblemByChangeTakeOffTime(PlaneAirlineList, index, first);
                    if (second.Problem != null && second.Problem.DetailType != ProblemType.None) FixAirportProblemByChangeTakeOffTime(PlaneAirlineList, index + 1, second);
                    return true;
                }
            }
            return false;
        }
    }
}