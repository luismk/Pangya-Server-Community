using System.Collections.Generic;
using Pangya_GameServer.Models;
using PangyaAPI.Utilities;

namespace Pangya_GameServer.UTIL
{
    public class ClubInfo3D
    {

        public ClubInfo3D(eCLUB_TYPE _type,
            float _rotation_spin,
            float _rotation_curve,
            float _power_factor,
            float _degree,
            float _power_base)
        {
            this.m_type = _type;
            this.m_rotation_spin = _rotation_spin;
            this.m_rotation_curve = _rotation_curve;
            this.m_power_factor = _power_factor;
            this.m_degree = _degree;
            this.m_power_base = _power_base;
        }

        public eCLUB_TYPE m_type = new eCLUB_TYPE();

        public float m_rotation_spin;
        public float m_rotation_curve;
        public float m_power_factor;
        public float m_degree;
        public float m_power_base;
    }
    public class AllClubInfo3D
    {
        public List<ClubInfo3D> m_clubs;
        public AllClubInfo3D()
        {
            this.m_clubs = new List<ClubInfo3D>();

            // Wood
            m_clubs.Add(new ClubInfo3D(eCLUB_TYPE.WOOD, // 1W
                0.55f, 1.61f, 236.0f, 10.0f,
                230.0f));
            m_clubs.Add(new ClubInfo3D(eCLUB_TYPE.WOOD, // 2W
                0.50f, 1.41f, 204.0f, 13.0f,
                210.0f));
            m_clubs.Add(new ClubInfo3D(eCLUB_TYPE.WOOD, // 3W
                0.45f, 1.26f, 176.0f, 16.0f,
                190.0f));

            // Iron
            m_clubs.Add(new ClubInfo3D(eCLUB_TYPE.IRON, // I2
                0.45f, 1.07f, 161.0f, 20.0f,
                180.0f));
            m_clubs.Add(new ClubInfo3D(eCLUB_TYPE.IRON, // I3
                0.45f, 0.95f, 149.0f, 24.0f,
                170.0f));
            m_clubs.Add(new ClubInfo3D(eCLUB_TYPE.IRON, // I4
                0.45f, 0.83f, 139.0f, 28.0f,
                160.0f));
            m_clubs.Add(new ClubInfo3D(eCLUB_TYPE.IRON, // I5
                0.45f, 0.73f, 131.0f, 32.0f,
                150.0f));
            m_clubs.Add(new ClubInfo3D(eCLUB_TYPE.IRON, // I6
                0.41f, 0.67f, 124.0f, 36.0f,
                140.0f));
            m_clubs.Add(new ClubInfo3D(eCLUB_TYPE.IRON, // I7
                0.36f, 0.61f, 118.0f, 40.0f,
                130.0f));
            m_clubs.Add(new ClubInfo3D(eCLUB_TYPE.IRON, // I8
                0.30f, 0.57f, 114.0f, 44.0f,
                120.0f));
            m_clubs.Add(new ClubInfo3D(eCLUB_TYPE.IRON, // I9
                0.25f, 0.53f, 110.0f, 48.0f,
                110.0f));

            // PW e SW
            m_clubs.Add(new ClubInfo3D(eCLUB_TYPE.PW, // PW
                0.18f, 0.49f, 107.0f, 52.0f,
                100.0f));
            m_clubs.Add(new ClubInfo3D(eCLUB_TYPE.PW, // SW
                0.17f, 0.42f, 93.0f, 56.0f,
                80.0f));

            // Putt
            m_clubs.Add(new ClubInfo3D(eCLUB_TYPE.PT, // PT1
                0.00f, 0.00f, 30.0f, 0.00f,
                20.0f));
            m_clubs.Add(new ClubInfo3D(eCLUB_TYPE.PT, // PT2
                0.00f, 0.00f, 21.0f, 0.00f,
                10.0f));
        }
    }

    public class sAllClubInfo3D : Singleton<AllClubInfo3D>
    {
        public sAllClubInfo3D()
        { }
    }
}