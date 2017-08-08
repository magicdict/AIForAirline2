using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace AIForAirline
{

    public static partial class Solution
    {
        public static void Analyze()
        {
            var TotalImportFac = 0d;
            foreach (var airline in AirlineDic.Values)
            {
                if (!airline.IsTyphoonLandFixable)
                {
                    Console.WriteLine("取消航班:" + airline);
                    TotalImportFac += airline.ImportFac;
                }
                //从结果来看起飞问题总是可以修复
                if (!airline.IsTyphoonTakeOffFixable)
                {
                    Console.WriteLine("取消航班:" + airline);
                    TotalImportFac += airline.ImportFac;
                }
            }
            Console.WriteLine("取消航班总体损失：" + (TotalImportFac * Statistics.CancelAirlineParm));

            //分析航班环
            var TestPlaneId = "1";
            var circles = AirlineCircle.GetCircle(PlaneIdAirlineDic[TestPlaneId]);
            Console.WriteLine(TestPlaneId + "号飞机 发现航班环：" + circles.Count);
            foreach (var circle in circles)
            {
                var StartAirline = PlaneIdAirlineDic[TestPlaneId][circle.StartIndex];
                var EndAirline = PlaneIdAirlineDic[TestPlaneId][circle.EndIndex];
                Console.WriteLine(circle.Airport + "," + StartAirline.ID + "," + EndAirline.ID);
            }
        }
    }
}