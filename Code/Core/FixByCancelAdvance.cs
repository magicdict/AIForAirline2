using System.Collections.Generic;
using System;
using System.Linq;

namespace AIForAirline
{
    /// <summary>
    /// 核心算法
    /// </summary>
    public static partial class CoreAlgorithm
    {

        //是否通过取消代替调机
        public static bool CanEscapeTyphoonByCancel(List<Airline> AirlineList)
        {
            int StartIndex, EndIndex;
            var range = GetTyphoonRange(AirlineList);
            StartIndex = range.StartIndex;
            EndIndex = range.EndIndex;
            //StartIndex 是受   台风影响的航班
            //EndIndex   是不受 台风影响的航班！
            if (EndIndex == -1 || StartIndex == -1 || EndIndex == AirlineList.Count) return false;
            string StartAirPort = AirlineList[StartIndex].StartAirPort;
            if (CheckCondition.TyphoonAirport.Contains(StartAirPort)) return false;
            for (int i = EndIndex; i < AirlineList.Count; i++)
            {
                //如果存在开始为StartPort的航班
                if (AirlineList[i].StartAirPort.Equals(StartAirPort))
                {
                    for (int j = StartIndex; j < i; j++)
                    {
                        AirlineList[j].FixMethod = enmFixMethod.Cancel;
                    }
                    return CoreAlgorithm.AdjustAirLineList(AirlineList);
                }
            }
            return false;
        }


        public static bool CanEscapeTyphoonByFrontCancel(List<Airline> AirlineList)
        {
            int StartIndex, EndIndex;
            var range = GetTyphoonRange(AirlineList);
            StartIndex = range.StartIndex;
            EndIndex = range.EndIndex;
            //StartIndex 是受   台风影响的航班
            //EndIndex   是不受 台风影响的航班！
            if (EndIndex == -1 || StartIndex == -1 || EndIndex == AirlineList.Count) return false;
            string StartAirPort = AirlineList[EndIndex].StartAirPort;
            if (CheckCondition.TyphoonAirport.Contains(StartAirPort)) return false;
            for (int i = EndIndex - 1; i > Math.Max(1, StartIndex - 5); i--)
            {
                //如果存在开始为StartPort的航班
                if (AirlineList[i].StartAirPort.Equals(StartAirPort))
                {
                    for (int j = i; j < EndIndex; j++)
                    {
                        AirlineList[j].FixMethod = enmFixMethod.Cancel;
                    }
                    return CoreAlgorithm.AdjustAirLineList(AirlineList);
                }
            }
            return false;
        }

        public static bool CanEscapeTyphoonByCancelAdvanced(List<Airline> AirlineList)
        {
            int StartIndex, EndIndex;
            var range = GetTyphoonRange(AirlineList);
            StartIndex = range.StartIndex;
            EndIndex = range.EndIndex;
            //StartIndex 是受   台风影响的航班
            //EndIndex   是不受 台风影响的航班！
            if (EndIndex == -1 || StartIndex == -1 || EndIndex == AirlineList.Count) return false;
            string StartAirPort = AirlineList[StartIndex].StartAirPort;
            if (CheckCondition.TyphoonAirport.Contains(StartAirPort)) return false;
            for (int i = StartIndex; i < EndIndex; i++)
            {
                //如果存在开始为StartPort的航班
                if (AirlineList[i].StartAirPort.Equals(StartAirPort))
                {
                    //是否能够通过延迟的方式，使得航班降落
                    var CloneAirlineList = Utility.DeepCopy(AirlineList);
                    CloneAirlineList[i].Problem = GetProblem(CloneAirlineList[i]);
                    if (FixAirportProblemByChangeTakeOffTime(CloneAirlineList, i, CloneAirlineList[i]))
                    {
                        for (int j = StartIndex; j < i; j++)
                        {
                             AirlineList[j].FixMethod = enmFixMethod.Cancel;
                        }
                        return CoreAlgorithm.AdjustAirLineList(AirlineList);
                    }
                }
            }
            return false;
        }

        public static bool FixByComplexAdjust(List<Airline> airlineList)
        {
            //将所有的航班分组(假设没有从台风飞到台风的航班，如果有的话，暂时不处理)
            //分组的规则为，第一个是起飞是台风关联航班，最后一个结尾是台风关联航班
            var airlinegrpList = new List<List<Airline>>();
            var IsGroupStarted = false;
            List<Airline> CurrentGroup = null;

            for (int i = 0; i < airlineList.Count; i++)
            {
                if (CheckCondition.TyphoonAirport.Contains(airlineList[i].StartAirPort))
                {
                    //起飞是台风航班
                    CurrentGroup = new List<Airline>();
                    airlinegrpList.Add(CurrentGroup);
                    CurrentGroup.Add(airlineList[i]);
                    IsGroupStarted = true;
                }
                else
                {
                    if (CheckCondition.TyphoonAirport.Contains(airlineList[i].EndAirPort))
                    {
                        //降落是台风航班
                        if (IsGroupStarted)
                        {
                            //分组开始状态
                            CurrentGroup.Add(airlineList[i]);
                            IsGroupStarted = false;
                        }
                        else
                        {
                            //分组未开始状态（第一个航班降落在台风机场的情况）
                            CurrentGroup = new List<Airline>();
                            airlinegrpList.Add(CurrentGroup);
                            CurrentGroup.Add(airlineList[i]);
                        }
                    }
                    else
                    {
                        //起飞降落都是台风航班
                        if (IsGroupStarted)
                        {
                            //分组开始状态
                            CurrentGroup.Add(airlineList[i]);
                        }
                        else
                        {
                            //普通航班
                            CurrentGroup = new List<Airline>();
                            airlinegrpList.Add(CurrentGroup);
                            CurrentGroup.Add(airlineList[i]);
                        }
                    }
                }
            }
            var firstTyphoonGroupId = -1;

            for (int i = 0; i < airlinegrpList.Count; i++)
            {
                foreach (var airline in airlinegrpList[i])
                {
                    //判断被台风影响的航班
                    var Problem = CheckCondition.IsExistTyphoon(airline);
                    if (Problem.DetailType == ProblemType.TyphoonLand)
                    {
                        firstTyphoonGroupId = i;
                        break;
                    }
                }
                if (firstTyphoonGroupId != -1) break;
            }
            //索引检查
            if (firstTyphoonGroupId >= airlinegrpList.Count - 1) return false;
            if (firstTyphoonGroupId <= 0) return false;
            // Console.WriteLine("台风开始组号：[" + firstTyphoonGroupId + "]");
            var firstAirline = airlinegrpList[firstTyphoonGroupId + 1].First();
            var lastAirline = airlinegrpList[firstTyphoonGroupId - 1].Last();
            //台风上一组的结束和台风下一组的开始能否衔接？
            if (lastAirline.EndAirPort != firstAirline.StartAirPort) return false;
            //下一组的第一个航班是否可以通过提早飞行而成功？
            firstAirline.Problem = GetProblem(firstAirline);
            //检查之前，需要正确设置上一个航班的信息！
            firstAirline.PreviousAirline = lastAirline;
            if (firstAirline.Problem.DetailType == ProblemType.None || firstAirline.Problem.DetailType == ProblemType.TyphoonStay)
            {
                return false;
            }
            if (!FixTyphoonTakeOffByChangeTakeOffTime(firstAirline)) return false;
            //尝试将第一组台风影响航班取消
            foreach (var item in airlinegrpList[firstTyphoonGroupId])
            {
                item.FixMethod = enmFixMethod.Cancel;
            }
            return CoreAlgorithm.AdjustAirLineList(airlineList);
        }

        //对于连续插入的状态，可能会造成后续航班大幅度延误
        //这个时候反而插入一个调机比较好
        public static void InsertEmptyFlyToCancelList(List<Airline> airlineList)
        {
            //是否为连续取消的判断
            var StartCancelIndex = -1;
            var EndCancelIndex = -1;

            for (int i = 0; i < airlineList.Count; i++)
            {
                //调机的情况，退出
                if (airlineList[i].FixMethod == enmFixMethod.EmptyFly) return;
                if (airlineList[i].FixMethod == enmFixMethod.Cancel)
                {
                    if (StartCancelIndex == -1)
                    {
                        StartCancelIndex = i;
                    }
                }
                else
                {
                    if (StartCancelIndex != -1)
                    {
                        EndCancelIndex = i - 1;
                    }
                }

            }
            if (StartCancelIndex == -1) return;
            if (EndCancelIndex - StartCancelIndex < 2) return;

            //改动前分数
            var BeforeFix = Statistics.WriteResult(airlineList, false);
            if (BeforeFix < Statistics.EmptyFlyParm + 2 * Statistics.CancelAirlineParm) return;
            var AirlineListClone = Utility.DeepCopy(airlineList);
            var StartIndexClone = -1;
            var EndIndexClone = -1;
            //选择第一个可以空飞的航班
            for (int i = StartCancelIndex + 1; i < EndCancelIndex; i++)
            {
                if (!CheckCondition.TyphoonAirport.Contains(airlineList[i].StartAirPort))
                {
                    StartIndexClone = StartCancelIndex;
                    EndIndexClone = i;
                    if (IsCanEmptyFly(airlineList, ref StartIndexClone, ref EndIndexClone))
                    {
                        if (StartIndexClone != StartCancelIndex || EndIndexClone != i) continue;
                        var EmptyFly = GetEmptyFly(AirlineListClone, StartIndexClone, EndIndexClone);
                        for (int j = EndIndexClone; j < EndCancelIndex; j++)
                        {
                            AirlineListClone[j].FixMethod = enmFixMethod.UnFixed;
                        }
                        break;
                    }
                }
            }
            if (StartIndexClone == -1) return;
            if (AdjustAirLineList(AirlineListClone))
            {
                //可以修复
                var AfterFix = Statistics.WriteResult(AirlineListClone, false) + Statistics.EmptyFlyParm;
                if (AfterFix < BeforeFix)
                {
                    var EmptyFly = GetEmptyFly(AirlineListClone, StartIndexClone, EndIndexClone);
                    CoreAlgorithm.EmptyFlyList.Add(EmptyFly);
                    for (int j = EndIndexClone; j < EndCancelIndex; j++)
                    {
                        airlineList[j].FixMethod = enmFixMethod.UnFixed;
                    }
                    AdjustAirLineList(airlineList);
                }
            }
        }
    }
}