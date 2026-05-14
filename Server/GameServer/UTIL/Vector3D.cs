using System;

namespace Pangya_GameServer.UTIL
{
    public class Vector3D
    {

        public Vector3D(float _x,
            float _y, float _z)
        {
            this.m_x = _x;
            this.m_y = _y;
            this.m_z = _z;
        }

        public Vector3D normalize()
        {
            return divideScalar(length());
        }

        public Vector3D negate()
        {
            return multiplyScalar(-1F);
        }

        public float length()
        {
            return (float)Math.Sqrt(m_x * m_x + m_y * m_y + m_z * m_z);
        }

        public float dot(Vector3D _vector3d)
        {
            return dot(_vector3d.m_x,
                _vector3d.m_y, _vector3d.m_z);
        }

        public float dot(float _x,
            float _y, float _z)
        {
            return m_x * _x + m_y * _y + m_z * _z;
        }

        public Vector3D copy(Vector3D _vector3d)
        {

            m_x = _vector3d.m_x;
            m_y = _vector3d.m_y;
            m_z = _vector3d.m_z;

            return this;
        }

        public Vector3D clone()
        {
            return new Vector3D(m_x,
                m_y, m_z);
        }

        public Vector3D addScalar(float _value)
        {

            m_x += _value;
            m_y += _value;
            m_z += _value;

            return this;
        }

        public Vector3D subScalar(float _value)
        {

            m_x -= _value;
            m_y -= _value;
            m_z -= _value;

            return this;
        }

        public Vector3D multiplyScalar(float _value)
        {

            m_x *= _value;
            m_y *= _value;
            m_z *= _value;

            return this;
        }

        public Vector3D divideScalar(float _value)
        {

            m_x /= _value;
            m_y /= _value;
            m_z /= _value;

            return this;
        }

        public Vector3D add(Vector3D _vector3d)
        {
            return add(_vector3d.m_x,
                _vector3d.m_y, _vector3d.m_z);
        }

        public Vector3D add(float _x,
            float _y, float _z)
        {

            m_x += _x;
            m_y += _y;
            m_z += _z;

            return this;
        }

        public Vector3D sub(Vector3D _vector3d)
        {
            return sub(_vector3d.m_x,
                _vector3d.m_y, _vector3d.m_z);
        }

        public Vector3D sub(float _x,
            float _y, float _z)
        {

            m_x -= _x;
            m_y -= _y;
            m_z -= _z;

            return this;
        }

        public Vector3D multiply(Vector3D _vector3d)
        {
            return multiply(_vector3d.m_x,
                _vector3d.m_y, _vector3d.m_z);
        }

        public Vector3D multiply(float _x,
            float _y, float _z)
        {

            m_x *= _x;
            m_y *= _y;
            m_z *= _z;

            return this;
        }

        public Vector3D divide(Vector3D _vector3d)
        {
            return divide(_vector3d.m_x,
                _vector3d.m_y, _vector3d.m_z);
        }

        public Vector3D divide(float _x,
            float _y, float _z)
        {

            m_x /= _x;
            m_y /= _y;
            m_z /= _z;

            return this;
        }

        public Vector3D cross(Vector3D _vector3d)
        {
            return cross(_vector3d.m_x,
                _vector3d.m_y, _vector3d.m_z);
        }

        public Vector3D cross(float _x,
            float _y, float _z)
        {

            float x = m_x;
            float y = m_y;
            float z = m_z;

            m_x = y * _z - z * _y;
            m_y = z * _x - x * _z;
            m_z = x * _y - y * _x;

            return this;
        }

        public float distanceTo(Vector3D _vector3d)
        {
            return distanceTo(_vector3d.m_x,
                _vector3d.m_y, _vector3d.m_z);
        }

        public float distanceTo(float _x,
            float _y, float _z)
        {

            float dx = m_x - _x;
            float dy = m_y - _y;
            float dz = m_z - _z;

            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public float distanceXZTo(Vector3D _vector3d)
        {
            return distanceXZTo(_vector3d.m_x, _vector3d.m_z);
        }

        public float distanceXZTo(float _x, float _z)
        {
            return distanceTo(_x,
                0.0f, _z);
        }

        public float angleTo(Vector3D _vector3d)
        {
            return angleTo(_vector3d.m_x,
                _vector3d.m_y, _vector3d.m_z);
        }

        public float angleTo(float x, float y, float z)
        {
            float theta = dot(x, y, z) / (length() * new Vector3D(x, y, z).length());

            theta = Math.Max(theta, Math.Min(-1.0f, 1.0f));  // Clamp no intervalo [-1, 1]

            return (float)Math.Acos(theta);
        }


        public bool isEqual(Vector3D _vector3d)
        {
            return isEqual(_vector3d.m_x,
                _vector3d.m_y, _vector3d.m_z);
        }

        public bool isEqual(float _x,
            float _y, float _z)
        {
            return (m_x == _x) && (m_y == _y) && (m_z == _z);
        }

        public Vector3D applyMatrix3(Matrix3D _matrix3d)
        {

            float x = m_x;
            float y = m_y;
            float z = m_z;

            m_x = _matrix3d.m_v1.m_x * x + _matrix3d.m_v2.m_x * y + _matrix3d.m_v3.m_x * z;
            m_y = _matrix3d.m_v1.m_y * x + _matrix3d.m_v2.m_y * y + _matrix3d.m_v3.m_y * z;
            m_z = _matrix3d.m_v1.m_z * x + _matrix3d.m_v2.m_z * y + _matrix3d.m_v3.m_z * z;

            return this;
        }

        public Vector3D applyMatrix4(Matrix3D _matrix3d)
        {

            float x = m_x;
            float y = m_y;
            float z = m_z;

            m_x = _matrix3d.m_v1.m_x * x + _matrix3d.m_v2.m_x * y + _matrix3d.m_v3.m_x * z + _matrix3d.m_v4.m_x;
            m_y = _matrix3d.m_v1.m_y * x + _matrix3d.m_v2.m_y * y + _matrix3d.m_v3.m_y * z + _matrix3d.m_v4.m_y;
            m_z = _matrix3d.m_v1.m_z * x + _matrix3d.m_v2.m_z * y + _matrix3d.m_v3.m_z * z + _matrix3d.m_v4.m_z;

            return this;
        }

        public float m_x;
        public float m_y;
        public float m_z;
    }
}