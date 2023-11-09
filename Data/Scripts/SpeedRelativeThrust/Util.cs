using VRage.ModAPI;

namespace SpeedRelativeThrust
{
    public class Util
    {
        public static bool IsValid(IMyEntity obj)
        {
            return obj != null && !obj.MarkedForClose && !obj.Closed;
        }
    }
}
