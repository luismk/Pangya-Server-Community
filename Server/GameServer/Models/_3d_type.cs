using Pangya_GameServer.UTIL;
namespace Pangya_GameServer.Models
{
    public enum eTYPE_DISTANCE : byte
    {
        LESS_10,
        LESS_15,
        LESS_28,
        LESS_58,
        BIGGER_OR_EQUAL_58
    }

    public enum ePOWER_SHOT_FACTORY : byte
    {
        NO_POWER_SHOT,
        ONE_POWER_SHOT,
        TWO_POWER_SHOT,
        ITEM_15_POWER_SHOT
    }

    public enum eCLUB_TYPE : byte
    {
        WOOD,
        IRON,
        PW,
        PT
    }

    public class options3D
    {

        // Struct options3D
        public options3D(uSpecialShot _shot,
            Vector3D _position,
            IExtraPower _extra_power,
            ePOWER_SHOT_FACTORY _power_shot,
            float _distance,
            float _power_slot,
            float _percent_shot,
            float _spin, float _curve,
            float _mira)
        {
            this.m_shot = _shot;
            this.m_position = _position;
            this.m_extra_power = _extra_power;
            this.m_power_shot = _power_shot;
            this.m_distance = _distance;
            this.m_power_slot = _power_slot;
            this.m_percent_shot = _percent_shot;
            this.m_spin = _spin;
            this.m_curve = _curve;
            this.m_mira = _mira;
        }


        public uSpecialShot m_shot = new uSpecialShot();
        public Pangya_GameServer.UTIL.Vector3D m_position;
        public IExtraPower m_extra_power;
        public ePOWER_SHOT_FACTORY m_power_shot = new ePOWER_SHOT_FACTORY();

        public float m_distance;
        public float m_power_slot;
        public float m_percent_shot;

        public float m_spin;
        public float m_curve;

        public float m_mira;

    }

}
