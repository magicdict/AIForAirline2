using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace AIForAirline
{

    public static partial class Solution
    {
        //原始的航班数据（航班ID是主键，NO在联行的时候是重复的！！）
        public static Dictionary<string, Airline> AirlineDic = new Dictionary<string, Airline>();
        //按照飞机号整理的航班表
        //Key:PlaneID Value:Airline
        public static Dictionary<string, List<Airline>> PlaneIdAirlineDic = new Dictionary<string, List<Airline>>();
        //联运
        public static List<CombinedVoyage> CombinedVoyageList = new List<CombinedVoyage>();
        //飞行时间表
        public static Dictionary<string, int> FlyTimeDic = new Dictionary<string, int>();
        //国内机场列表
        public static List<string> DomaticAirport = new List<string>();
        public static Dictionary<string, List<string>> PlaneTypeDic = new Dictionary<string, List<string>>();
        //计划航班机型字典
        public static Dictionary<string, string> PlaneTypeSearchDic = new Dictionary<string, string>();
        //机型座位数字典
        public static Dictionary<string, int> PlaneTypeSeatCntDic = new Dictionary<string, int>();

        public static List<TransTime> TransTimeList = new List<TransTime>();

        //读取CSV文件
        public static void ReadCSV()
        {
            StreamReader reader = null;
            //飞行表
            if (File.Exists(Utility.FlyTimeCSV))
            {
                reader = new StreamReader(Utility.FlyTimeCSV);
                Utility.Log("读取飞行时间表文件:" + Utility.FlyTimeCSV);
                //去除标题
                reader.ReadLine();
                while (!reader.EndOfStream)
                {
                    var items = reader.ReadLine().Split(",");
                    var key = items[0] + int.Parse(items[1]).ToString("D2") + int.Parse(items[2]).ToString("D2");
                    //原数据存在主键重复的问题
                    if (!FlyTimeDic.ContainsKey(key)) FlyTimeDic.Add(key, int.Parse(items[3]));
                }
                reader.Close();
                Utility.Log("飞行时间记录数:" + FlyTimeDic.Count);
            }
            //读取航班信息表
            Utility.Log("读取航班信息文件:" + Utility.AirlineCSV);
            reader = new StreamReader(Utility.AirlineCSV);
            List<Airline> AirlineList = new List<Airline>();
            //去除标题
            reader.ReadLine();
            while (!reader.EndOfStream)
            {
                var airline = new Airline(reader.ReadLine());
                AirlineList.Add(airline);
                AirlineDic.Add(airline.ID, airline);
            }
            reader.Close();
            var SameComboAirKey = AirlineList.GroupBy((x) => { return x.ComboAirLineKey; });
            foreach (var item in SameComboAirKey)
            {
                if (item.Count() == 2)
                {
                    Airline first;
                    Airline second;
                    if (item.First().StartTime > item.Last().StartTime)
                    {
                        first = item.Last();
                        second = item.First();
                    }
                    else
                    {
                        first = item.First();
                        second = item.Last();
                    }
                    var combined = new CombinedVoyage(first, second);
                    first.IsFirstCombinedVoyage = true;
                    first.ComboAirline = combined;
                    second.ComboAirline = combined;
                    CombinedVoyageList.Add(combined);
                }
            }
            Utility.Log("航班数:" + AirlineDic.Count + " 联程数:" + CombinedVoyageList.Count);

            if (File.Exists(Utility.TyphoonCSV))
            {
                Utility.Log("读取台风场景文件:" + Utility.TyphoonCSV);
                reader = new StreamReader(Utility.TyphoonCSV);
                //去除标题
                reader.ReadLine();
                while (!reader.EndOfStream)
                {
                    var typhoon = new Typhoon(reader.ReadLine());
                    CheckCondition.TyphoonList.Add(typhoon);
                    if (!CheckCondition.TyphoonAirport.Contains(typhoon.AirPort)) CheckCondition.TyphoonAirport.Add(typhoon.AirPort);
                    Utility.Log(typhoon.ToString());
                }
                reader.Close();
                Utility.Log("台风场景数:" + CheckCondition.TyphoonList.Count);
            }

            if (File.Exists(Utility.AirportProhibitCSV))
            {
                Utility.Log("读取机场限制文件:" + Utility.AirportProhibitCSV);
                reader = new StreamReader(Utility.AirportProhibitCSV);
                //去除标题
                reader.ReadLine();
                while (!reader.EndOfStream)
                {

                    CheckCondition.AirPortProhibitList.Add(new AirPortProhibit(reader.ReadLine()));
                }
                reader.Close();
                Utility.Log("机场限制数:" + CheckCondition.AirPortProhibitList.Count);
            }

            if (File.Exists(Utility.PlaneProhibitCSV))
            {
                Utility.Log("读取航线-飞机限制文件:" + Utility.PlaneProhibitCSV);
                reader = new StreamReader(Utility.PlaneProhibitCSV);
                //去除标题
                reader.ReadLine();
                while (!reader.EndOfStream)
                {

                    CheckCondition.PlaneProhibitList.Add(new PlaneProhibit(reader.ReadLine()));
                }
                reader.Close();
                Utility.Log("航线-飞机限制数:" + CheckCondition.AirPortProhibitList.Count);
            }

            if (File.Exists(Utility.TransTimeCSV))
            {
                Utility.Log("读取旅客中转时间限制表文件:" + Utility.TransTimeCSV);
                reader = new StreamReader(Utility.TransTimeCSV);
                //去除标题
                reader.ReadLine();
                while (!reader.EndOfStream)
                {

                    TransTimeList.Add(new TransTime(reader.ReadLine()));
                }
                reader.Close();
                Utility.Log("旅客中转时间限制数:" + TransTimeList.Count);
            }

            if (File.Exists(Utility.AirportCSV))
            {
                Utility.Log("读取机场表文件:" + Utility.AirportCSV);
                reader = new StreamReader(Utility.AirportCSV);
                //去除标题
                reader.ReadLine();
                while (!reader.EndOfStream)
                {
                    var temp = reader.ReadLine().Split(",".ToCharArray());
                    if (temp[1] == "1") DomaticAirport.Add(temp[0]);
                }
                reader.Close();
                Utility.Log("国际机场数:" + DomaticAirport.Count);
            }


            Utility.Log("执飞表建立:");
            GetAirlineDicByPlaneId();
            Utility.Log("飞机数量:" + PlaneIdAirlineDic.Count());

        }



        //按照飞机编号整理航班表
        public static void GetAirlineDicByPlaneId()
        {
            //按照飞机ID分析每架飞机的航班情况
            PlaneIdAirlineDic.Clear();
            PlaneTypeDic.Clear();
            var airlines = AirlineDic.Values.ToList();
            var planeGroup = airlines.GroupBy(x => { return x.ModifiedPlaneID; });
            int NotEnoughStayTimeCnt = 0;
            foreach (var planeAirlines in planeGroup)
            {
                //需要按照航班时间进行排序（计算过站时间用）
                var list = planeAirlines.ToList();
                list.Sort((x, y) => { return x.ModifiedStartTime.CompareTo(y.ModifiedStartTime); });
                //标注上一个和下一个航班
                for (int i = 0; i < list.Count; i++)
                {
                    if (i != 0)
                    {
                        //除去第一班
                        list[i].PreviousAirline = list[i - 1];
                        if (list[i].StayBeforeTakeOffTimeMinutes < Utility.StayAtAirPortMinutes)
                        {
                            //原本航班过站时间小于50分钟
                            NotEnoughStayTimeCnt++;
                            if (Utility.PrintInfo)
                            {
                                Utility.Log("警告:原始航班信息，过站时间不足！本航班航班号ID：[" + list[i].ID + "]起飞时间：" +
                                list[i].StartTime + " 上一个航班航班号ID:[" + list[i - 1].ID + "]降落时间：" + list[i - 1].EndTime +
                                "过站时间：" + list[i].StayBeforeTakeOffTimeMinutes);
                            }
                        }
                    }
                    if (i != list.Count - 1)
                    {
                        //除去最后一班
                        list[i].NextAirLine = list[i + 1];
                    }
                    if (list[i].IsFirstCombinedVoyage)
                    {
                        //对于联航拉直的处理：这里需要实时处理DirectAirLine
                        if (i != 0) list[i].ComboAirline.DirectAirLine.PreviousAirline = list[i - 1];
                        if (i < list.Count - 2) list[i].ComboAirline.DirectAirLine.NextAirLine = list[i + 2];
                    }
                }
                PlaneIdAirlineDic.Add(planeAirlines.Key, list.ToList());
                var PlaneType = list.First().PlaneType;
                if (!PlaneTypeDic.ContainsKey(PlaneType)) PlaneTypeDic.Add(PlaneType, new List<string>());
                PlaneTypeDic[PlaneType].Add(planeAirlines.Key);

                if (!PlaneTypeSearchDic.ContainsKey(planeAirlines.Key)) PlaneTypeSearchDic.Add(planeAirlines.Key, PlaneType);
            }
            Utility.Log("[警告]过站时间不足航班数：" + NotEnoughStayTimeCnt);
        }
    }
}