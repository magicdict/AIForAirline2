namespace AIForAirline
{
    //故障详细分类
    public enum ProblemType
    {
        /// <summary>
        /// 没有故障
        /// </summary>
        None,
        /// <summary>
        /// 无法降落(台风场景)
        /// </summary>
        TyphoonLand,
        /// <summary>
        /// 无法起飞(台风场景)
        /// </summary>
        TyphoonTakeOff,
        /// <summary>
        /// 无法停留(台风场景)
        /// </summary>       
        TyphoonStay,
        /// <summary>
        /// 无法降落(机场限制)
        /// </summary>
        AirportProhibitLand,
        /// <summary>
        /// 无法起飞(机场限制)
        /// </summary>
        AirportProhibitTakeOff,
        /// <summary>
        /// 飞机无法飞行某航班(航线-飞机)
        /// </summary>
        AirLinePlaneProhibit,
        /// <summary>
        /// 台风和机场关闭混合
        /// </summary>
        AirPortTyphoonMix,
        /// <summary>
        /// 过站时间不足
        /// </summary>
        //NotEnoughStayAirportTime
    }
}