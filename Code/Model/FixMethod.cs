namespace AIForAirline
{
    public enum enmFixMethod
    {
        //换机
        ChangePlane,
        //联程拉直
        Direct,
        //拉直后的取消
        CancelByDirect,
        //取消
        Cancel,
        //调机
        EmptyFly,
        //未修复
        UnFixed
    }
}