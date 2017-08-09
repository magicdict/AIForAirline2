using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace AIForAirline
{
    //工具类
    public static class Utility
    {

        public static bool IsMac = false;
        //调试模式
        public static bool IsDebugMode = true;
        //打印详细调试信息
        public static bool PrintInfo = false;

        //飞机过站时间
        public const int StayAtAirPortMinutes = 50;
        //最大提早时间
        public const int EarlyMaxMinute = 6 * 60;
        //国内航班最大延迟时间
        public const int DelayDemasticMaxMinute = 24 * 60;
        //国际航班最大延迟时间
        public const int DelayInternationalMaxMinute = 36 * 60;
        //5月6日16点换机分数
        public static DateTime TyphoonStartTime = new DateTime(2017, 5, 6, 16, 0, 0);
        //起飞时刻位于5月6日06:00-5月8日24:00之间才能调整
        //5月8日24:00 -> 5月9日00:00
        public static DateTime RecoverStart = new DateTime(2017, 5, 6, 6, 0, 0);
        public static DateTime RecoverEnd = new DateTime(2017, 5, 9, 0, 0, 0);


        //特限定起飞或者降落受影响的机场在5月6日16:00前一小时，5月7日17:00后两小时，每5分钟内仅允许2个航班起飞和降落
        public static DateTime UnitTimeLimitBeforeTyphoonStart = new DateTime(2017, 5, 6, 15, 0, 0);
        public static DateTime UnitTimeLimitBeforeTyphoonEnd = new DateTime(2017, 5, 6, 16, 0, 0);
        public static DateTime UnitTimeLimitAfterTyphoonStart = new DateTime(2017, 5, 7, 17, 0, 0);
        public static DateTime UnitTimeLimitAfterTyphoonEnd = new DateTime(2017, 5, 7, 19, 0, 0);

        //日期分隔符
        public static string DateSplitChar = "/";
        //日期格式
        public static string DateFormat = "yyyy/MM/dd";
        //时间格式24小时补零(精确到分钟)
        public const string TimeFormat = "HH:mm";

        public const string FullDateFormat = "yyyy/MM/dd HH:mm";
        //选手昵称
        public const string UserId = "胡八一";

        public static string WorkSpaceRoot;
        public static string AirlineCSV;
        public static string TyphoonCSV;
        public static string PlaneProhibitCSV;
        public static string AirportProhibitCSV;
        public static string AirportCSV;
        public static string TransTimeCSV;
        public static string FlyTimeCSV;
        public static string ResultPath;
        //日志路径
        public static string LogPath;
        //测评程序
        public static string XMAEvaluationFilename = "E:\\WorkSpace2017\\AIForAirline\\External\\XMAEvaluation.jar";
        //测评原始数据
        public static string DataSetXLSFilename = "E:\\WorkSpace2017\\AIForAirline\\External\\厦航大赛数据20170808.xlsx";

        //复制结果到测评程序目录
        public static string XMAEvaluationDatasetFilename;

        public static bool IsEvalute = true;

        public static string RunAirline = string.Empty;
        //是否使用停机库
        public static bool IsUseTyphoonStayRoom = false;

        public static void Init(string root)
        {
            WorkSpaceRoot = root;
            AirlineCSV = WorkSpaceRoot + "Dataset" + Path.DirectorySeparatorChar + "AirLine.csv";
            TyphoonCSV = WorkSpaceRoot + "Dataset" + Path.DirectorySeparatorChar + "Typhoon.csv";
            PlaneProhibitCSV = WorkSpaceRoot + "Dataset" + Path.DirectorySeparatorChar + "PlaneProhibit.csv";
            AirportProhibitCSV = WorkSpaceRoot + "Dataset" + Path.DirectorySeparatorChar + "AirportProhibit.csv";
            FlyTimeCSV = WorkSpaceRoot + "Dataset" + Path.DirectorySeparatorChar + "FlyTime.csv";
            AirportCSV = WorkSpaceRoot + "Dataset" + Path.DirectorySeparatorChar + "Airport.csv";
            TransTimeCSV = WorkSpaceRoot + "Dataset" + Path.DirectorySeparatorChar + "TransTime.csv";
            ResultPath = WorkSpaceRoot + "Result" + Path.DirectorySeparatorChar;
            LogPath = WorkSpaceRoot + "Log" + Path.DirectorySeparatorChar;
            if (!Directory.Exists(LogPath))
            {
                Directory.CreateDirectory(LogPath);
            }
            if (!Directory.Exists(ResultPath))
            {
                Directory.CreateDirectory(ResultPath);
            }
        }
        //时间字符串格式化
        public static string FormatTime(string rawValue)
        {
            //6:00 -> 06:00
            string Hour = int.Parse(rawValue.Split(":".ToCharArray())[0]).ToString("D2");
            string Minute = rawValue.Split(":".ToCharArray())[1];
            return Hour + ":" + Minute;
        }

        //日期字符串格式化
        public static string FormatDate(string rawValue)
        {
            //2016-6-15 -> 2016-06-15
            string Year = rawValue.Split(DateSplitChar.ToCharArray())[0];
            string Month = int.Parse(rawValue.Split(DateSplitChar.ToCharArray())[1]).ToString("D2");
            string Day = int.Parse(rawValue.Split(DateSplitChar.ToCharArray())[2]).ToString("D2");
            return Year + DateSplitChar + Month + DateSplitChar + Day;
        }

        public static bool WriteLogToFile = false;

        public static StringBuilder LogStringBuilder = new StringBuilder();

        public static void Log(string content)
        {
            if (!IsDebugMode) return;
            if (WriteLogToFile)
            {
                LogStringBuilder.AppendLine(content);
            }
            else
            {
                Console.WriteLine(content);
            }
        }

        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static T DeepCopy<T>(T obj)
        {
            object retval;
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                //序列化成流
                bf.Serialize(ms, obj);
                ms.Seek(0, SeekOrigin.Begin);
                //反序列化成对象
                retval = bf.Deserialize(ms);
                ms.Close();
            }
            return (T)retval;
        }

    }
}