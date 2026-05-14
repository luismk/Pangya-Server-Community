using Pangya_GameServer.Models;
using static Pangya_GameServer.Models.DefineConstants;
namespace Pangya_GameServer.UTIL
{
    public class Club3D
    {

        public Club3D(ClubInfo3D _club_info, eTYPE_DISTANCE _type_distance = eTYPE_DISTANCE.BIGGER_OR_EQUAL_58)
        {
            this.m_club_info = _club_info;
            this.m_type_distance = _type_distance;
        }

        public virtual void Dispose()
        {
        }

        public void init(ClubInfo3D _clubInfo, eTYPE_DISTANCE _type_distance = eTYPE_DISTANCE.BIGGER_OR_EQUAL_58)
        {

            m_club_info = _clubInfo;
            m_type_distance = _type_distance;
        }

        public float getDegreeRad()
        {
            return (float)(m_club_info.m_degree * PI / 180.0f);
        }

        public float getDegreeRadByDistanceType(float _spin)
        {
            return getDegreeRad() + (float)(m_type_distance != eTYPE_DISTANCE.BIGGER_OR_EQUAL_58 ? (_spin * POWER_SPIN_PW_FACTORY) : 0.0f);
        }

        public float getPower(IExtraPower _extraPower,
            float _pwrSlot,
            ePOWER_SHOT_FACTORY _psf,
            float _spin)
        {

            float pwr = 0.0f;

            switch (m_club_info.m_type)
            {
                case eCLUB_TYPE.WOOD:

                    pwr = _extraPower.getTotal((byte)(_psf)) + getPowerShotFactory((byte)(_psf)) + ((_pwrSlot - BASE_POWER_CLUB) * 2);

                    pwr *= 1.5f;
                    pwr /= m_club_info.m_power_base;
                    pwr += 1.0f;
                    pwr *= m_club_info.m_power_factor;

                    break;
                case eCLUB_TYPE.IRON:
                    pwr = ((getPowerShotFactory((byte)(_psf)) / m_club_info.m_power_base + 1.0f) * m_club_info.m_power_factor) + (_extraPower.getTotal((byte)(_psf)) * m_club_info.m_power_factor * 1.3f) / m_club_info.m_power_base;
                    break;
                case eCLUB_TYPE.PW:
                    {

                        switch (m_type_distance)
                        {
                            case eTYPE_DISTANCE.LESS_10:
                            case eTYPE_DISTANCE.LESS_15:
                            case eTYPE_DISTANCE.LESS_28:
                                pwr = (getPowerByDegreeAndSpin(getDegreeRad(), _spin) * (52.0f + (_psf != ePOWER_SHOT_FACTORY.NO_POWER_SHOT ? 28.0f : 0.0f))) + (_extraPower.getTotal(((byte)(_psf))) * m_club_info.m_power_factor) / m_club_info.m_power_base;
                                break;
                            case eTYPE_DISTANCE.LESS_58:
                                pwr = (getPowerByDegreeAndSpin(getDegreeRad(), _spin) * (80.0f + (_psf != ePOWER_SHOT_FACTORY.NO_POWER_SHOT ? 18.0f : 0.0f))) + (_extraPower.getTotal(((byte)(_psf))) * m_club_info.m_power_factor) / m_club_info.m_power_base;
                                break;
                            case eTYPE_DISTANCE.BIGGER_OR_EQUAL_58:
                                pwr = ((getPowerShotFactory((byte)(_psf)) / m_club_info.m_power_base + 1.0f) * m_club_info.m_power_factor) + (_extraPower.getTotal(((byte)(_psf))) * m_club_info.m_power_factor) / m_club_info.m_power_base;
                                break;
                        }
                        break;
                    }
                case eCLUB_TYPE.PT:
                    pwr = m_club_info.m_power_factor;
                    break;
            }

            return pwr;
        }

        public float getRotationSpin(IExtraPower _extraPower,
            float _pwrSlot,
            ePOWER_SHOT_FACTORY _psf)
        {

            float rotation = (_extraPower.getTotal(((byte)(_psf))) / 2) + _pwrSlot;

            rotation /= 170.0f;

            return rotation + 1.5f;
        }

        public float getRange(IExtraPower _extraPower,
            float _pwrSlot,
            ePOWER_SHOT_FACTORY _psf)
        {

            float pwr = m_club_info.m_power_base + _extraPower.getTotal(((byte)(_psf))) + getPowerShotFactory((byte)(_psf));

            if (m_club_info.m_type == eCLUB_TYPE.WOOD)
            {
                pwr += ((_pwrSlot - BASE_POWER_CLUB) * 2);
            }

            if (m_club_info.m_type == eCLUB_TYPE.PW)
            {

                switch (m_type_distance)
                {
                    case eTYPE_DISTANCE.LESS_10:
                    case eTYPE_DISTANCE.LESS_15:
                    case eTYPE_DISTANCE.LESS_28:
                        pwr = 30.0f + (_psf != ePOWER_SHOT_FACTORY.NO_POWER_SHOT ? 30.0f : 0.0f) + _extraPower.getTotal(((byte)(_psf)));
                        break;
                    case eTYPE_DISTANCE.LESS_58:
                        pwr = 60.0f + (_psf != ePOWER_SHOT_FACTORY.NO_POWER_SHOT ? 20.0f : 0.0f) + _extraPower.getTotal(((byte)(_psf)));
                        break;
                    case eTYPE_DISTANCE.BIGGER_OR_EQUAL_58:
                        pwr = m_club_info.m_power_base + _extraPower.getTotal(((byte)(_psf))) + getPowerShotFactory((byte)(_psf));
                        break;
                }
            }

            return pwr;
        }

        public ClubInfo3D m_club_info;

        public eTYPE_DISTANCE m_type_distance = new eTYPE_DISTANCE();
    }
}
