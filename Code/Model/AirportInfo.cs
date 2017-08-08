using System;
using System.Collections.Generic;

namespace AIForAirline
{
    //机场信息
    public class AirportInfo
    {
        public bool IsTakeOff;
        //事件时间
        public DateTime EventTime = DateTime.MinValue;
        //事件航班
        public Airline EventAirline = null;

        public override string ToString()
        {
            if (string.IsNullOrEmpty(EventAirline.ModifiedPlaneID)) EventAirline.ModifiedPlaneID = EventAirline.PlaneID;
            if (string.IsNullOrEmpty(EventAirline.ID) && EventAirline.FixMethod == enmFixMethod.EmptyFly) EventAirline.ID = "9000";
            return int.Parse(EventAirline.ID).ToString("D4") + "," + int.Parse(EventAirline.ModifiedPlaneID).ToString("D3") + "," + (IsTakeOff ? "起飞" : "降落") + "," +
                   EventTime.ToString("yyyy/MM/dd HH:mm") + "," + (IsTakeOff ? EventAirline.EndAirPort : EventAirline.StartAirPort) + "," +
                   EventAirline.PlaneType + "," + EventAirline.FixMethod.ToString();
        }

    }
}