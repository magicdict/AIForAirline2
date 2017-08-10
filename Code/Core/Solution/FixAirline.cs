using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AIForAirline
{

    public static partial class Solution
    {
        public static (bool IsOK, double Score) FixAirline(List<Airline> PlaneAirlineList, bool IsTry = false)
        {
            //停机库的备份
            var CloneTyphoonAirportRemain = Utility.DeepCopy(CheckCondition.TyphoonAirportRemain);
            //多线程，防止出现异常循环体内外写不同的变量
            foreach (var airport in CloneTyphoonAirportRemain.Keys)
            {
                CheckCondition.TyphoonAirportRemain[airport] = 999;
            }
            //复杂调整
            var PlaneAirlineListCloneComplexAdjust = Utility.DeepCopy(PlaneAirlineList);
            var ScoreComplexAdjust = double.MaxValue;
            if (CoreAlgorithm.FixByComplexAdjust(PlaneAirlineListCloneComplexAdjust))
                ScoreComplexAdjust = Statistics.WriteResult(PlaneAirlineListCloneComplexAdjust);

            //单纯调机
            var PlaneAirlineListCloneWithEmptyFly = Utility.DeepCopy(PlaneAirlineList);
            var ScoreWithEmptyFly = double.MaxValue;
            //调机的结果
            if (CoreAlgorithm.FixByEmptyFly(PlaneAirlineListCloneWithEmptyFly, true))
            {
                //计算空飞分数正式输出的时候，是最后一起算的，所以，这里必须要补上空飞的惩罚
                ScoreWithEmptyFly = Statistics.WriteResult(PlaneAirlineListCloneWithEmptyFly) + Statistics.EmptyFlyParm;
            }

            //取消一些航班
            var PlaneAirlineListCloneCancel = Utility.DeepCopy(PlaneAirlineList);
            var ScoreCancel = double.MaxValue;
            if (CoreAlgorithm.CanEscapeTyphoonByCancel(PlaneAirlineListCloneCancel))
            {
                ScoreCancel = Statistics.WriteResult(PlaneAirlineListCloneCancel);
            }

            //取消一些航班-高级
            var PlaneAirlineListCloneCancelAdvanced = Utility.DeepCopy(PlaneAirlineList);
            var ScoreCancelAdvanced = double.MaxValue;
            if (CoreAlgorithm.CanEscapeTyphoonByCancelAdvanced(PlaneAirlineListCloneCancelAdvanced))
            {
                ScoreCancelAdvanced = Statistics.WriteResult(PlaneAirlineListCloneCancelAdvanced);
            }

            //普通调整
            var ScoreNormal = double.MaxValue;
            var PlaneAirlineListCloneNormal = Utility.DeepCopy(PlaneAirlineList);
            if (CoreAlgorithm.AdjustAirLineList(PlaneAirlineListCloneNormal))
            {
                ScoreNormal = Statistics.WriteResult(PlaneAirlineListCloneNormal);
            }

            var PlaneAirlineListCloneFrontCancel = Utility.DeepCopy(PlaneAirlineList);
            var ScoreFrontCancel = double.MaxValue;
            if (CoreAlgorithm.CanEscapeTyphoonByFrontCancel(PlaneAirlineListCloneFrontCancel))
            {
                ScoreFrontCancel = Statistics.WriteResult(PlaneAirlineListCloneFrontCancel);
            }

            var PlaneAirlineListCloneConvertToEmpty = Utility.DeepCopy(PlaneAirlineList);
            var ScoreConvertToEmpty = double.MaxValue;
            if (CoreAlgorithm.FixByConvertToEmptyFly(PlaneAirlineListCloneConvertToEmpty, true))
            {
                //计算空飞分数正式输出的时候，是最后一起算的，所以，这里必须要补上空飞的惩罚
                ScoreConvertToEmpty = Statistics.WriteResult(PlaneAirlineListCloneConvertToEmpty) + Statistics.EmptyFlyParm;
            }

            var PlaneAirlineListCloneEmptyAdvanced = Utility.DeepCopy(PlaneAirlineList);
            var ScoreEmptyAdvanced = double.MaxValue;
            int BestEndIndex = CoreAlgorithm.GetFixByEmptyFlyAdvanced(PlaneAirlineListCloneEmptyAdvanced);
            if (BestEndIndex != -1)
            {
                ScoreEmptyAdvanced = CoreAlgorithm.FixByEmptyFlyAdvanced(PlaneAirlineListCloneEmptyAdvanced, BestEndIndex, true);
            }

            var PlaneAirlineListCloneCancelSomeSection = Utility.DeepCopy(PlaneAirlineList);
            var ScoreCancelSomeSection = double.MaxValue;
            var BestStartEndIndex = CoreAlgorithm.GetCancelSomeSectionIndex(PlaneAirlineListCloneCancelSomeSection);
            if (BestStartEndIndex.StartCancelIndex != -1)
            {
                ScoreCancelSomeSection = CoreAlgorithm.FixByCancelSomeSection(PlaneAirlineListCloneCancelSomeSection, BestStartEndIndex);
            }


            //寻找最小的一种方式
            double MinScore = Math.Min(ScoreComplexAdjust, ScoreWithEmptyFly);
            MinScore = Math.Min(ScoreCancel, MinScore);
            MinScore = Math.Min(ScoreCancelAdvanced, MinScore);
            MinScore = Math.Min(ScoreNormal, MinScore);
            MinScore = Math.Min(ScoreFrontCancel, MinScore);
            MinScore = Math.Min(ScoreConvertToEmpty, MinScore);
            MinScore = Math.Min(ScoreEmptyAdvanced, MinScore);
            MinScore = Math.Min(ScoreCancelSomeSection, MinScore);
            if (IsTry) return (MinScore != double.MaxValue, MinScore);
            CheckCondition.TyphoonAirportRemain = CloneTyphoonAirportRemain;
            if (Math.Round(MinScore, 0) == Math.Round(ScoreComplexAdjust, 0))
            {
                Utility.Log("FixByComplexAdjust:" + PlaneAirlineList[0].ModifiedPlaneID);
                return (CoreAlgorithm.FixByComplexAdjust(PlaneAirlineList), MinScore);
            }

            if (Math.Round(MinScore, 0) == Math.Round(ScoreWithEmptyFly, 0))
            {
                Utility.Log("FixByEmptyFly:" + PlaneAirlineList[0].ModifiedPlaneID);
                return (CoreAlgorithm.FixByEmptyFly(PlaneAirlineList, false), MinScore);
            }
            if (Math.Round(MinScore, 0) == Math.Round(ScoreCancel, 0))
            {
                Utility.Log("CanEscapeTyphoonByCancel:" + PlaneAirlineList[0].ModifiedPlaneID);
                return (CoreAlgorithm.CanEscapeTyphoonByCancel(PlaneAirlineList), MinScore);
            }
            if (Math.Round(MinScore, 0) == Math.Round(ScoreCancelAdvanced, 0))
            {
                Utility.Log("CanEscapeTyphoonByCancelAdvanced:" + PlaneAirlineList[0].ModifiedPlaneID);
                return (CoreAlgorithm.CanEscapeTyphoonByCancelAdvanced(PlaneAirlineList), MinScore);
            }
            if (Math.Round(MinScore, 0) == Math.Round(ScoreNormal, 0))
            {
                //普通调整
                Utility.Log("AdjustAirLineList:" + PlaneAirlineList[0].ModifiedPlaneID);
                return (CoreAlgorithm.AdjustAirLineList(PlaneAirlineList), MinScore);
            }
            if (Math.Round(MinScore, 0) == Math.Round(ScoreFrontCancel, 0))
            {
                Utility.Log("CanEscapeTyphoonByFrontCancel:" + PlaneAirlineList[0].ModifiedPlaneID);
                return (CoreAlgorithm.CanEscapeTyphoonByFrontCancel(PlaneAirlineList), MinScore);
            }
            if (Math.Round(MinScore, 0) == Math.Round(ScoreConvertToEmpty, 0))
            {
                Utility.Log("FixByConvertToEmptyFly:" + PlaneAirlineList[0].ModifiedPlaneID);
                return (CoreAlgorithm.FixByConvertToEmptyFly(PlaneAirlineList, false), MinScore);
            }
            if (Math.Round(MinScore, 0) == Math.Round(ScoreEmptyAdvanced, 0))
            {
                Utility.Log("FixByEmptyFlyAdvanced:" + PlaneAirlineList[0].ModifiedPlaneID);
                CoreAlgorithm.FixByEmptyFlyAdvanced(PlaneAirlineList, BestEndIndex, false);
                return (true, MinScore);
            }
            if (Math.Round(MinScore, 0) == Math.Round(ScoreCancelSomeSection, 0))
            {
                Utility.Log("ScoreCancelSomeSection:" + PlaneAirlineList[0].ModifiedPlaneID);
                CoreAlgorithm.FixByCancelSomeSection(PlaneAirlineList, BestStartEndIndex);
                return (true, MinScore);
            }
            return (false, double.MaxValue);
        }
    }
}