using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace AIForAirline
{
    public partial class Statistics
    {
        internal static void WriteDebugInfo(List<Airline> airlineList)
        {
            airlineList.AddRange(CoreAlgorithm.EmptyFlyList);
            airlineList.Sort((x, y) =>
            {
                if (int.Parse(x.ModifiedPlaneID).ToString("D3").Equals(int.Parse(y.ModifiedPlaneID).ToString("D3")))
                {
                    return x.StartTime.CompareTo(y.StartTime);
                }
                else
                {
                    return int.Parse(x.ModifiedPlaneID).ToString("D3").CompareTo(int.Parse(y.ModifiedPlaneID).ToString("D3"));
                }
            });
            string filename = Utility.WorkSpaceRoot + "Result" + Path.DirectorySeparatorChar + "Debug.csv";
            var writer = new StreamWriter(filename);
            var outputline = new string[19];
            outputline[0] = "AirLineID";
            outputline[1] = "StartAirPort";
            outputline[2] = "EndAirPort";
            outputline[3] = "StartTime";
            outputline[4] = "EndTime";
            outputline[5] = "ModifiedStartTime";
            outputline[6] = "ModifiedEndTime";
            outputline[7] = "ModifiedPlaneID";
            outputline[8] = "IsCancel";
            outputline[9] = "IsDirect";
            outputline[10] = "IsEmptyFly";
            outputline[11] = "PlanStayTime";
            outputline[12] = "ActureStayTime";
            outputline[13] = "StartTimeDiff";
            outputline[14] = "AirlineNo";
            outputline[15] = "InterKbn";
            outputline[16] = "Important";
            outputline[17] = "PlaneID";
            outputline[18] = "DiffScore";
            writer.WriteLine(string.Join(",", outputline));
            foreach (var airline in airlineList)
            {
                outputline[0] = airline.ID;
                outputline[1] = airline.StartAirPort;
                outputline[2] = airline.EndAirPort;
                outputline[3] = airline.StartTime.ToString();
                outputline[4] = airline.EndTime.ToString();
                outputline[5] = airline.ModifiedStartTime.ToString();
                outputline[6] = airline.ModifiedEndTime.ToString();
                outputline[7] = airline.ModifiedPlaneID;
                outputline[8] = "0"; //是否取消
                outputline[9] = "0"; //是否拉直
                outputline[10] = "0"; //是否调机
                outputline[14] = "";  //联程
                outputline[15] = airline.InterKbn;  //国际国内
                outputline[16] = airline.ImportFac.ToString();
                outputline[17] = airline.PlaneID;
                var Diff = airline.ModifiedStartTime.Subtract(airline.StartTime).TotalHours;
                if (Diff > 0)
                {
                    //延迟
                    outputline[18] = Math.Round((Diff * airline.ImportFac * TotalDelayParm), 0).ToString();
                }
                else
                {
                    //提早(Diff是负数，这里用减法)
                    outputline[18] = Math.Round((-Diff * airline.ImportFac * TotalEarlyParm), 0).ToString();
                }

                switch (airline.FixMethod)
                {
                    case enmFixMethod.Cancel:
                        outputline[8] = "1"; //是否取消
                        break;
                    case enmFixMethod.Direct:
                        outputline[8] = "0"; //是否取消
                        outputline[9] = "1"; //是否拉直
                        break;
                    case enmFixMethod.CancelByDirect:
                        //使用原数据填充
                        outputline[8] = "1"; //是否取消
                        outputline[9] = "1"; //是否拉直
                        break;
                    case enmFixMethod.EmptyFly:
                        outputline[10] = "1"; //是否调机
                        break;
                }
                if (airline.PreviousAirline != null)
                {
                    outputline[11] = airline.StartTime.Subtract(airline.PreviousAirline.EndTime).TotalMinutes.ToString();
                    outputline[12] = airline.ModifiedStartTime.Subtract(airline.PreviousAirline.ModifiedEndTime).TotalMinutes.ToString();
                }
                else
                {
                    outputline[11] = "-";
                    outputline[12] = "-";
                }
                outputline[13] = Math.Abs(airline.StartTime.Subtract(airline.ModifiedStartTime).TotalMinutes).ToString();
                if (airline.ComboAirline != null)
                {
                    outputline[14] = airline.ComboAirline.No;
                }
                writer.WriteLine(string.Join(",", outputline));
            }
            writer.Close();
        }
    }
}