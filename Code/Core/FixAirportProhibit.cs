using System.Collections.Generic;
using System;
namespace AIForAirline
{
    //核心算法
    public static partial class CoreAlgorithm
    {

        private static bool FixAirportProblemByChangeTakeOffTime(List<Airline> PlaneAirlineList, int index, Airline airline)
        {
            var ProblemDescript = string.Empty;
            if (PlaneAirlineList[index].Problem == null) return false;
            switch (PlaneAirlineList[index].Problem.DetailType)
            {
                case ProblemType.TyphoonLand:
                    ProblemDescript = "[台风：降落]";
                    break;
                case ProblemType.TyphoonTakeOff:
                    ProblemDescript = "[台风：起飞]";
                    break;
                case ProblemType.TyphoonStay:
                    ProblemDescript = "[台风：停机]";

                    if (CheckCondition.TyphoonAirportRemain[PlaneAirlineList[index].StartAirPort] != 0 && Utility.IsUseTyphoonStayRoom)
                    {
                        CheckCondition.TyphoonAirportRemain[PlaneAirlineList[index].StartAirPort]--;
                        PlaneAirlineList[index].IsUseTyphoonRoom = true;
                        return true;
                    }
                    else
                    {
                        Utility.Log("[无法修复]航班ID：" + PlaneAirlineList[index].ID);
                        return false;
                    }
                default:
                    break;
            }

            var StandardScore = Statistics.WriteResult(PlaneAirlineList);
            Utility.Log("调整前基准分数：" + StandardScore);

            //调整独立的            
            Utility.Log(ProblemDescript + "尝试调整航班（独立）：航班ID：" + airline.ID);
            var CloneAirline_SingleChange = Utility.DeepCopy(PlaneAirlineList);
            var SingleResult = FixByChangeTakeOffTime(CloneAirline_SingleChange[index]);
            Utility.Log((SingleResult ? "[成功]" : "[失败]") + "调整航班（独立）增加分数：" + (Statistics.WriteResult(CloneAirline_SingleChange) - StandardScore));
            if (SingleResult)
            {
                FixByChangeTakeOffTime(PlaneAirlineList[index]);
                StandardScore = Statistics.WriteResult(PlaneAirlineList);
                Utility.Log("调整后基准分数：" + StandardScore);
                return true;
            }

            //延迟单个航班可能无法解决问题，则考虑顺延所有航班(温和的)
            Utility.Log(ProblemDescript + "尝试调整航班（温和的链式）：航班ID：" + airline.ID);
            var CloneAirline_ChainChange = Utility.DeepCopy(PlaneAirlineList);
            var ChainResult = FixByChangeTakeOffTime(CloneAirline_ChainChange, index);
            Utility.Log((ChainResult ? "[成功]" : "[失败]") + "调整航班（温和的链式）增加分数：" + (Statistics.WriteResult(CloneAirline_ChainChange) - StandardScore));
            if (ChainResult)
            {
                FixByChangeTakeOffTime(PlaneAirlineList, index);
                StandardScore = Statistics.WriteResult(PlaneAirlineList);
                Utility.Log("调整后基准分数：" + StandardScore);
                return true;
            }

            //延迟单个航班可能无法解决问题，则考虑顺延所有航班(强制的)
            Utility.Log(ProblemDescript + "尝试调整航班（强制的链式）：航班ID：" + airline.ID);
            var CloneAirline_ChainChange_Force = Utility.DeepCopy(PlaneAirlineList);
            var ChainForceResult = FixChainByChangeTakeOffTime_Force(CloneAirline_ChainChange_Force, index);
            Utility.Log((ChainForceResult ? "[成功]" : "[失败]") + "调整航班（强制的链式）增加分数：" + (Statistics.WriteResult(CloneAirline_ChainChange_Force) - StandardScore));
            if (ChainForceResult)
            {
                FixChainByChangeTakeOffTime_Force(PlaneAirlineList, index);
                StandardScore = Statistics.WriteResult(PlaneAirlineList);
                Utility.Log("调整后基准分数：" + StandardScore);
                return true;
            }
            Utility.Log("[失败]通过修改起飞时间来修复错误");
            return false;
        }

        //调整航班的起飞时间
        //返回值表示是否能成功
        public static bool FixByChangeTakeOffTime(Airline airline)
        {
            if (airline.Problem.DetailType == ProblemType.TyphoonStay) return false;
            //这里的Early是这样考虑的，起飞时间选取早的那种，但是，这种早有一个前提条件，就是和原计划相比是延迟的。
            bool CanEarly = true;
            var TakeOffTime = airline.Problem.TakeOffBeforeThisTime;
            var LandTime = TakeOffTime.Add(airline.FlightSpan);
            if (TakeOffTime > airline.StartTime)
            {
                //是否影响上一个航班的过站时间
                CanEarly = TakeOffTime.Subtract(airline.PreviousAirline.ModifiedEndTime).TotalMinutes >= Utility.StayAtAirPortMinutes;
                if (CanEarly)
                {
                    //延迟：国内24小时，国际36小时
                    if (airline.InterKbn == "国内")
                    {
                        CanEarly = TakeOffTime.Subtract(airline.StartTime).TotalMinutes <= Utility.DelayDemasticMaxMinute;
                    }
                    else
                    {
                        CanEarly = TakeOffTime.Subtract(airline.StartTime).TotalMinutes <= Utility.DelayInternationalMaxMinute;
                    }
                }
            }
            else
            {
                CanEarly = false;
            }
            if (CanEarly)
            {
                var TempStartTime = airline.ModifiedStartTime;
                airline.ModifiedStartTime = airline.Problem.TakeOffBeforeThisTime;
                Utility.Log("[起飞（机场限制）]调整完毕航班：航班ID：" + airline.ID + " 修改起飞时间：" + TempStartTime + "->" + airline.ModifiedStartTime);
                return true;
            }

            //尝试延迟起飞
            bool CanLate = true;

            if (airline.Problem.IsNotEnoughStayAirportTime)
            {
                TakeOffTime = airline.Problem.GetTakeOffTime();
            }
            else
            {
                TakeOffTime = airline.Problem.TakeoffAfterThisTime;
            }
            if (TakeOffTime == DateTime.MaxValue)
            {
                CanLate = false;
            }
            else
            {
                LandTime = TakeOffTime.Add(airline.FlightSpan);
                //是否影响上一班的过站时间(这里的延迟只是相对于计划时间来说的，可能上一个步骤修改过时间了)
                if (airline.PreviousAirline != null)
                {
                    CanLate = TakeOffTime.Subtract(airline.PreviousAirline.ModifiedEndTime).TotalMinutes >= Utility.StayAtAirPortMinutes;
                }
                //是否影响下一班的过站时间
                if (airline.NextAirLine != null && CanLate) CanLate = airline.NextAirLine.ModifiedStartTime.Subtract(LandTime).TotalMinutes >= Utility.StayAtAirPortMinutes;
                if (CanLate)
                {
                    //延迟：国内24小时，国际36小时
                    CanLate = IsLatable(airline, TakeOffTime);
                }
            }

            if (CanLate)
            {
                var TempStartTime = airline.ModifiedStartTime;
                airline.ModifiedStartTime = TakeOffTime;
                Utility.Log("[起飞（机场限制）]调整完毕航班：航班ID：" + airline.ID + " 修改起飞时间：" + TempStartTime + "->" + airline.ModifiedStartTime);
                return true;
            }
            return false;
        }

        private static bool IsLatable(Airline airline, System.DateTime TakeOffTime)
        {
            bool CanLate;
            if (airline.InterKbn == "国内")
            {
                CanLate = TakeOffTime.Subtract(airline.StartTime).TotalMinutes <= Utility.DelayDemasticMaxMinute;
            }
            else
            {
                CanLate = TakeOffTime.Subtract(airline.StartTime).TotalMinutes <= Utility.DelayInternationalMaxMinute;
            }

            return CanLate;
        }

        public static bool FixByChangeTakeOffTime(List<Airline> PlaneAirlines, int index)
        {
            var adjustAirline = PlaneAirlines[index];
            var TakeOffTime = adjustAirline.Problem.IsNotEnoughStayAirportTime ? adjustAirline.Problem.GetTakeOffTime() : adjustAirline.Problem.TakeoffAfterThisTime;
            //如果上一个航班也是经过台风修正的，这里起飞和降落将重叠
            if (adjustAirline.PreviousAirline != null)
            {
                if (TakeOffTime.Subtract(adjustAirline.PreviousAirline.ModifiedEndTime).TotalMinutes < Utility.StayAtAirPortMinutes)
                {
                    TakeOffTime = adjustAirline.PreviousAirline.ModifiedEndTime.AddMinutes(Utility.StayAtAirPortMinutes);
                }
            }
            if (!IsLatable(adjustAirline, TakeOffTime)) return false;
            var LandTime = TakeOffTime.Add(adjustAirline.FlightSpan);
            //实际过站时间
            if (adjustAirline.NextAirLine == null)
            {
                //最后航班
                adjustAirline.ModifiedStartTime = TakeOffTime;
                return true;
            }
            var ActualStayMinutes = adjustAirline.NextAirLine.ModifiedStartTime.Subtract(LandTime).TotalMinutes;
            //过站时间不足
            var NotEnoughMinutes = Utility.StayAtAirPortMinutes - ActualStayMinutes;
            //寻找足够大的时间，使得整个延迟链可以完成
            var LargeStayMinutes = Utility.StayAtAirPortMinutes + NotEnoughMinutes;
            for (int i = index + 1; i < PlaneAirlines.Count - 1; i++)
            {
                //i开始计算的是下一个和下两个之间的时间差。如果计算未更改的当前和下一个之间的差距，则会出现问题
                //注意，最后一班是无法修改起飞时间的！
                var staytime = PlaneAirlines[i + 1].ModifiedStartTime.Subtract(PlaneAirlines[i].ModifiedEndTime).TotalMinutes;
                if (staytime >= LargeStayMinutes)
                {
                    Utility.Log("航班：" + PlaneAirlines[i].ID + "和航班" + PlaneAirlines[i + 1].ID + "之间的停机时间为：" + staytime);
                    Utility.Log("航班：" + PlaneAirlines[i].ID + "降落时间：" + PlaneAirlines[i].ModifiedEndTime);
                    Utility.Log("航班：" + PlaneAirlines[i + 1].ID + "起飞时间：" + PlaneAirlines[i + 1].ModifiedStartTime);

                    var TempStartTime = adjustAirline.ModifiedStartTime;
                    adjustAirline.ModifiedStartTime = TakeOffTime;
                    if (CheckCondition.IsExistTyphoon(adjustAirline).DetailType != ProblemType.None) return false;
                    Utility.Log("[起飞（机场限制）]调整完毕航班：航班ID：" + adjustAirline.ID + " 修改起飞时间：" + TempStartTime + "->" + adjustAirline.ModifiedStartTime);
                    for (int j = index + 1; j < i + 1; j++)
                    {
                        //从待处理航班的下一个开始，到过站时间允许为止，都需要处理
                        //每个航班都相应推迟
                        var airline = PlaneAirlines[j];
                        if (airline.FixMethod == enmFixMethod.Cancel) continue;
                        if (airline.Problem == null) airline.Problem = new Problem();
                        TempStartTime = airline.ModifiedStartTime;
                        //无需每个航班都添加不足时间，有可能原来的航班就已经有多余时间出现了。
                        //airline.ModifiedStartTime = airline.ModifiedStartTime.AddMinutes(NotEnoughMinutes);
                        var NewStartTime = airline.PreviousAirline.ModifiedEndTime.AddMinutes(Utility.StayAtAirPortMinutes);
                        var NewEndTime = NewStartTime.Add(airline.FlightSpan);
                        if (!CheckCondition.IsAirPortAvalible(airline.StartAirPort, NewStartTime).IsAvalible ||
                            !CheckCondition.IsAirPortAvalible(airline.EndAirPort, NewEndTime).IsAvalible)
                        {
                            return false;
                        }
                        if (!IsLatable(airline, NewStartTime)) return false;
                        //如果新的起飞时间比原来的起飞时间还要早，则直接退出循环
                        if (NewStartTime > airline.ModifiedStartTime)
                        {
                            if (!airline.IsAllowAdjust) return false;
                            airline.ModifiedStartTime = NewStartTime;
                            if (CheckCondition.IsExistTyphoon(airline).DetailType != ProblemType.None) return false;
                            Utility.Log("[起飞（机场限制）]调整完毕航班：航班ID：" + airline.ID + " 修改起飞时间：" + TempStartTime + "->" + airline.ModifiedStartTime);
                        }
                        else
                        {
                            break;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public static bool FixChainByChangeTakeOffTime_Force(List<Airline> PlaneAirlines, int index)
        {
            var adjustAirline = PlaneAirlines[index];

            var TakeOffTime = adjustAirline.Problem.IsNotEnoughStayAirportTime ? adjustAirline.Problem.GetTakeOffTime() : adjustAirline.Problem.TakeoffAfterThisTime;
            //如果上一个航班也是经过台风修正的，这里起飞和降落将重叠
            if (adjustAirline.PreviousAirline != null)
            {
                if (TakeOffTime.Subtract(adjustAirline.PreviousAirline.ModifiedEndTime).TotalMinutes < Utility.StayAtAirPortMinutes)
                {
                    TakeOffTime = adjustAirline.PreviousAirline.ModifiedEndTime.AddMinutes(Utility.StayAtAirPortMinutes);
                }
            }
            if (!IsLatable(adjustAirline, TakeOffTime)) return false;

            var LandTime = TakeOffTime.Add(adjustAirline.FlightSpan);
            //实际过站时间
            var ActualStayMinutes = adjustAirline.NextAirLine.ModifiedStartTime.Subtract(LandTime).TotalMinutes;
            //过站时间不足
            var NotEnoughMinutes = Utility.StayAtAirPortMinutes - ActualStayMinutes;
            var TempStartTime = adjustAirline.ModifiedStartTime;
            adjustAirline.ModifiedStartTime = TakeOffTime;
            var typhoon = CheckCondition.IsExistTyphoon(adjustAirline).DetailType;
            if (typhoon != ProblemType.None) return false;
            Utility.Log("[起飞（机场限制）]调整完毕航班：航班ID：" + adjustAirline.ID + " 修改起飞时间：" + TempStartTime + "->" + adjustAirline.ModifiedStartTime);
            for (int i = index + 1; i < PlaneAirlines.Count; i++)
            {
                //从待处理航班的下一个开始，到过站时间允许为止，都需要处理
                //每个航班都相应推迟
                var airline = PlaneAirlines[i];
                if (airline.FixMethod == enmFixMethod.Cancel) continue;
                if (airline.Problem == null) airline.Problem = new Problem();
                TempStartTime = airline.ModifiedStartTime;
                //无需每个航班都添加不足时间，有可能原来的航班就已经有多余时间出现了。
                //airline.ModifiedStartTime = airline.ModifiedStartTime.AddMinutes(NotEnoughMinutes);
                var NewStartTime = airline.PreviousAirline.ModifiedEndTime.AddMinutes(Utility.StayAtAirPortMinutes);
                var NewEndTime = NewStartTime.Add(airline.FlightSpan);

                var AirportCloseTakeoff = CheckCondition.IsAirPortAvalible(airline.StartAirPort, NewStartTime);
                var AirportCloseLand = CheckCondition.IsAirPortAvalible(airline.EndAirPort, NewEndTime);
                if (!AirportCloseLand.IsAvalible)
                {
                    var TempProblem = new Problem();
                    var AirClone = Utility.DeepCopy(airline);
                    AirClone.ModifiedStartTime = NewStartTime;
                    CheckCondition.SetRecoverInfo(AirClone, TempProblem, AirportCloseLand, false);
                    //只能延迟。。。
                    NewStartTime = TempProblem.IsNotEnoughStayAirportTime ? TempProblem.GetTakeOffTime() : TempProblem.TakeoffAfterThisTime;
                }

                if (!AirportCloseTakeoff.IsAvalible)
                {
                    var TempProblem = new Problem();
                    var AirClone = Utility.DeepCopy(airline);
                    AirClone.ModifiedStartTime = NewStartTime;
                    CheckCondition.SetRecoverInfo(AirClone, TempProblem, AirportCloseTakeoff, true);
                    //只能延迟。。。
                    NewStartTime = TempProblem.IsNotEnoughStayAirportTime ? TempProblem.GetTakeOffTime() : TempProblem.TakeoffAfterThisTime;
                }
                if (!IsLatable(airline, NewStartTime)) return false;
                //如果新的起飞时间比原来的起飞时间还要早，则直接退出循环
                if (NewStartTime > airline.ModifiedStartTime)
                {
                    if (!airline.IsAllowAdjust) return false;
                    airline.ModifiedStartTime = NewStartTime;
                    if (CheckCondition.IsExistTyphoon(airline).DetailType != ProblemType.None) return false;
                    Utility.Log("[起飞（机场限制）]调整完毕航班：航班ID：" + airline.ID + " 修改起飞时间：" + TempStartTime + "->" + airline.ModifiedStartTime);
                }
                else
                {
                    break;
                }
            }
            return true;
        }
    }
}