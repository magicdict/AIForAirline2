using System.Collections.Generic;
using System;
using System.Linq;

namespace AIForAirline
{
    /// <summary>
    /// 优化结果
    /// </summary>
    public static partial class ResultOptimize
    {
        public static void TryChangePlane()
        {
            var NewEmptyList = new List<Airline>();
            foreach (var empty in CoreAlgorithm.EmptyFlyList)
            {
                if (!ResultOptimize.CanChangePlane(Solution.PlaneIdAirlineDic[empty.ModifiedPlaneID]))
                {
                    NewEmptyList.Add(empty);
                }
                else
                {
                    Console.WriteLine("CanChangePlane:" + empty.ModifiedPlaneID);
                }
            }
            CoreAlgorithm.EmptyFlyList = NewEmptyList;
        }
        public static bool CanChangePlane(List<Airline> AirlineList)
        {
            int StartIndex, EndIndex;
            var range = CoreAlgorithm.GetTyphoonRange(AirlineList);
            StartIndex = range.StartIndex;
            EndIndex = range.EndIndex;
            if (StartIndex <= 0 || EndIndex == -1) return false;
            /*StartIndex 是受   台风影响的航班
            EndIndex   是不受 台风影响的航班！
            空飞航班起点在 StartIndex，终点在 EndIndex
            第一：机型相同（不同机型惩罚太大）
            第二：被取代的航班的飞机，其被取代航班的前期航班，能有一个目的地为当前调整航班台风后的机场的机会
            第三：代价不能太大*/

            var CurrentAirline = AirlineList[StartIndex - 1];
            var MinTakeOffTime = CurrentAirline.ModifiedEndTime.AddMinutes(Utility.StayAtAirPortMinutes);
            var Airport = CurrentAirline.EndAirPort;
            var orgPlaneType = CurrentAirline.PlaneType;
            int MinScore = int.MaxValue;
            var OrgAirlineList = new List<Airline>();
            var RepAirlineList = new List<Airline>();
            var BaseAirlineList = new List<Airline>();
            var OrgAirlineIdx = new List<int>();
            var RepAirlineIdx = new List<int>();
            var BaseIdx = new List<int>();
            foreach (var info in AirportIdAirlineDic[Airport])
            {
                //机型相同（不同机型惩罚太大）
                if (info.IsTakeOff && info.EventTime >= MinTakeOffTime && info.EventAirline.PlaneType == orgPlaneType)
                {
                    //已经有调机或者取消的情况，则跳过
                    if (CoreAlgorithm.EmptyFlyList.Count((x => { return x.ModifiedPlaneID == info.EventAirline.ModifiedPlaneID; })) == 1) continue;
                    if (Solution.PlaneIdAirlineDic[info.EventAirline.ModifiedPlaneID].Count((x => { return x.FixMethod == enmFixMethod.Cancel; })) > 0) continue;
                    var ReplaceAirline = Solution.PlaneIdAirlineDic[info.EventAirline.ModifiedPlaneID];
                    //被取代的航班的飞机，其被取代航班的前期航班，能有一个目的地为当前调整航班台风后的机场的机会
                    for (int org = EndIndex; org < AirlineList.Count; org++)
                    {
                        //遍历原来航班的台风后的航班（包括台风后的第一个航班）
                        //当前被替代航班为止(倒序，越接近越好)
                        var replbase = 0; //被取代航班索引号
                        for (int i = 0; i < ReplaceAirline.Count; i++)
                        {
                            if (ReplaceAirline[i].ID == info.EventAirline.ID) replbase = i;
                        }

                        for (int rep = replbase - 1; rep > 0; rep--)
                        {
                            if (ReplaceAirline[rep].EndAirPort.Equals(AirlineList[org].EndAirPort))
                            {
                                var Score = replbase - rep + org - EndIndex;
                                /*
                                Console.Write("被取代航班在原飞行计划中的索引:" + RepIdx);
                                Console.WriteLine("  前导航班索引：" + rep + "  间隔航班数：" + (RepIdx - rep));
                                Console.WriteLine("被前导航班：" + ReplaceAirline[rep].ToString());
                                Console.Write("台风后续首航班索引:" + EndIndex);
                                Console.WriteLine("台风后续目标航班索引:" + org + "  间隔航班数：" + (org - EndIndex));
                                Console.WriteLine("总体偏差:" + Score);
                                */
                                if (Score < MinScore)
                                {
                                    OrgAirlineList.Clear();
                                    RepAirlineList.Clear();
                                    BaseAirlineList.Clear();
                                    OrgAirlineIdx.Clear();
                                    RepAirlineIdx.Clear();
                                    BaseIdx.Clear();
                                    MinScore = Score;
                                    OrgAirlineList.Add(AirlineList[org]);
                                    RepAirlineList.Add(ReplaceAirline[rep]);
                                    BaseAirlineList.Add(info.EventAirline);
                                    OrgAirlineIdx.Add(org);
                                    RepAirlineIdx.Add(rep);
                                    BaseIdx.Add(replbase);
                                }
                                else
                                {
                                    if (Score == MinScore)
                                    {
                                        OrgAirlineList.Add(AirlineList[org]);
                                        RepAirlineList.Add(ReplaceAirline[rep]);
                                        BaseAirlineList.Add(info.EventAirline);
                                        OrgAirlineIdx.Add(org);
                                        RepAirlineIdx.Add(rep);
                                        BaseIdx.Add(replbase);
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }
            if (OrgAirlineList.Count != 0)
            {
                //选择基准时间最早的航班，理由如下
                //在衔接的时候，越是早交换，则越可以避免过站时间不足的问题
                var SetIdx = 0;
                var OrgPlandId = AirlineList[0].ModifiedPlaneID;
                var RelPlaneId = BaseAirlineList[SetIdx].ModifiedPlaneID;
                var ReplaceAirlineList = Solution.PlaneIdAirlineDic[RelPlaneId];
                //取消的时候牵涉到台风场景
                if (CheckCondition.TyphoonAirport.Contains(ReplaceAirlineList[RepAirlineIdx[SetIdx]].EndAirPort)) return false;
                if (CheckCondition.TyphoonAirport.Contains(AirlineList[OrgAirlineIdx[SetIdx]].EndAirPort)) return false;
                //飞机航线的测试
                for (int i = BaseIdx[SetIdx]; i < ReplaceAirlineList.Count; i++)
                {
                    if (!CheckCondition.IsAirlinePlaneAvalible(ReplaceAirlineList[i].StartAirPort, ReplaceAirlineList[i].EndAirPort, OrgPlandId)) return false;
                }
                for (int i = OrgAirlineIdx[SetIdx] + 1; i < AirlineList.Count; i++)
                {
                    if (!CheckCondition.IsAirlinePlaneAvalible(AirlineList[i].StartAirPort, AirlineList[i].EndAirPort, RelPlaneId)) return false;
                }
                var AirlineListClone = Utility.DeepCopy(AirlineList);
                var ReplaceAirlineListClone = Utility.DeepCopy(ReplaceAirlineList);
                //1.从StartIndex开始，到ORG为止的航班取消掉
                for (int i = StartIndex; i < OrgAirlineIdx[SetIdx] + 1; i++)
                {
                    AirlineListClone[i].FixMethod = enmFixMethod.Cancel;
                }
                //2.被取代的部分取消
                for (int i = RepAirlineIdx[SetIdx] + 1; i < BaseIdx[SetIdx]; i++)
                {
                    ReplaceAirlineListClone[i].FixMethod = enmFixMethod.Cancel;
                }
                //3.取代操作（取代者）
                for (int i = BaseIdx[SetIdx]; i < ReplaceAirlineList.Count; i++)
                {
                    ReplaceAirlineListClone[i].ModifiedPlaneID = OrgPlandId;
                }
                //4.取代操作（被取代者）
                for (int i = OrgAirlineIdx[SetIdx] + 1; i < AirlineList.Count; i++)
                {
                    AirlineListClone[i].ModifiedPlaneID = RelPlaneId;
                }

                var NewOrgPlaneList = new List<Airline>();
                var NewRelPlaneList = new List<Airline>();
                for (int i = 0; i < AirlineListClone.Count; i++)
                {
                    if (AirlineListClone[i].ModifiedPlaneID == OrgPlandId) NewOrgPlaneList.Add(AirlineListClone[i]);
                    if (AirlineListClone[i].ModifiedPlaneID == RelPlaneId) NewRelPlaneList.Add(AirlineListClone[i]);
                }
                for (int i = 0; i < ReplaceAirlineListClone.Count; i++)
                {
                    if (ReplaceAirlineListClone[i].ModifiedPlaneID == OrgPlandId) NewOrgPlaneList.Add(ReplaceAirlineListClone[i]);
                    if (ReplaceAirlineListClone[i].ModifiedPlaneID == RelPlaneId) NewRelPlaneList.Add(ReplaceAirlineListClone[i]);
                }
                NewOrgPlaneList.Sort((x, y) => { return x.ModifiedEndTime.CompareTo(y.ModifiedStartTime); });
                NewRelPlaneList.Sort((x, y) => { return x.ModifiedEndTime.CompareTo(y.ModifiedStartTime); });

                //检查时间间隔
                for (int i = 0; i < NewOrgPlaneList.Count; i++)
                {
                    if (i != 0)
                    {
                        if (NewOrgPlaneList[i].PlaneID != NewOrgPlaneList[i].ModifiedPlaneID)
                        {
                            if (NewOrgPlaneList[i].ModifiedStartTime.Subtract(NewOrgPlaneList[i - 1].ModifiedEndTime).TotalMinutes < Utility.StayAtAirPortMinutes) return false;
                        }
                    }
                }

                for (int i = 0; i < NewRelPlaneList.Count; i++)
                {
                    if (i != 0)
                    {
                        if (NewRelPlaneList[i].PlaneID != NewRelPlaneList[i].ModifiedPlaneID)
                        {
                            if (NewRelPlaneList[i].ModifiedStartTime.Subtract(NewRelPlaneList[i - 1].ModifiedEndTime).TotalMinutes < Utility.StayAtAirPortMinutes) return false;
                        }
                    }
                }
                //1.从StartIndex开始，到ORG为止的航班取消掉
                for (int i = StartIndex; i < OrgAirlineIdx[SetIdx] + 1; i++)
                {
                    AirlineList[i].FixMethod = enmFixMethod.Cancel;
                }
                //2.被取代的部分取消
                for (int i = RepAirlineIdx[SetIdx] + 1; i < BaseIdx[SetIdx]; i++)
                {
                    ReplaceAirlineList[i].FixMethod = enmFixMethod.Cancel;
                }
                //3.取代操作（取代者）
                for (int i = BaseIdx[SetIdx]; i < ReplaceAirlineList.Count; i++)
                {
                    ReplaceAirlineList[i].ModifiedPlaneID = OrgPlandId;
                    ReplaceAirlineList[i].FixMethod = enmFixMethod.ChangePlane;
                }
                //4.取代操作（被取代者）
                for (int i = OrgAirlineIdx[SetIdx] + 1; i < AirlineList.Count; i++)
                {
                    AirlineList[i].ModifiedPlaneID = RelPlaneId;
                    AirlineList[i].FixMethod = enmFixMethod.ChangePlane;
                }
                //5.Solution的飞机表更新
                Solution.PlaneIdAirlineDic[OrgPlandId].Clear();
                Solution.PlaneIdAirlineDic[RelPlaneId].Clear();
                foreach (var airline in AirlineList)
                {
                    Solution.PlaneIdAirlineDic[airline.ModifiedPlaneID].Add(airline);
                }
                foreach (var airline in ReplaceAirlineList)
                {
                    Solution.PlaneIdAirlineDic[airline.ModifiedPlaneID].Add(airline);
                }

                Console.WriteLine(OrgPlandId + "->" + RelPlaneId);

                return true;
            }
            return false;
        }
    }
}