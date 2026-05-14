namespace Pangya_GameServer.UTIL
{
    public class Ball3D
    {

        public Ball3D()
        {
            this.m_position = new Vector3D(0.0f,
                0.0f, 0.0f);
            this.m_slope = new Vector3D(0.0f,
                1.0f, 0.0f);
            this.m_velocity = new Vector3D(0.0f,
                0.0f, 0.0f);
            this.m_max_height = 0.0f;
            this.m_num_max_height = -1;
            this.m_count = 0;
            this.m_curve = 0.0f;
            this.m_spin = 0.0f;
            this.m_rotation_curve = 0.0f;
            this.m_rotation_spin = 0.0f;
            this.m_acumulation_curve = 0.0f;
            this.m_acumulation_spin = 0.0f;
        }


        public Vector3D m_position;
        public Vector3D m_slope;
        public Vector3D m_velocity;

        public float m_max_height;

        public int m_num_max_height = new int();
        public uint m_count = new uint();

        public float m_curve;
        public float m_spin;

        public float m_rotation_curve;
        public float m_rotation_spin;

        public float m_acumulation_curve;
        public float m_acumulation_spin;

        public readonly float m_mass = 0.045926999f;
        public readonly float m_diametro = 0.14698039f;
    }
}