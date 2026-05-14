namespace Pangya_GameServer.UTIL
{
    public class Matrix3D
    {
        public Matrix3D(Vector3D _v1,
            Vector3D _v2, Vector3D _v3,
            Vector3D _v4)
        {
            this.m_v1 = _v1;
            this.m_v2 = _v2;
            this.m_v3 = _v3;
            this.m_v4 = _v4;
        }


        public static Matrix3D crossMatrix(Matrix3D _m1, Matrix3D _m2)
        {
            return new Matrix3D(_m1.m_v1.applyMatrix3(_m2),
                _m1.m_v2.applyMatrix3(_m2),
                _m1.m_v3.applyMatrix3(_m2),
                _m1.m_v4.applyMatrix4(_m2));
        }

        public Vector3D m_v1;
        public Vector3D m_v2;
        public Vector3D m_v3;
        public Vector3D m_v4;
    }
}