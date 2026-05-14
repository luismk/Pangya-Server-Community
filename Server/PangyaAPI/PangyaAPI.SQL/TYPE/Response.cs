using System.Collections.Generic;

namespace PangyaAPI.SQL
{
    public class Response : System.IDisposable
    {
        public Response()
        {
            this.m_rows_affected = 0;
        }

        public void Dispose()
        {
            clear();
        }

        public void clear()
        {
            while (m_result_set.Count > 0)
            {
                if (m_result_set[0] != null)
                {
                    if (m_result_set[0] != null)
                    {
                        m_result_set[0].Dispose();
                    }
                }
            }

            m_result_set.Clear();
        }

        public void addResultSet(Result_Set _result_set)
        {
            m_result_set.Add(_result_set);
        }

        public uint getNumResultSet()
        {
            return (uint)m_result_set.Count;
        }

        public Result_Set getResultSetAt(uint _index)
        {
            if ((int)_index < 0 || _index >= m_result_set.Count)
            {
                throw new System.Exception("Index out of range.");
            }

            return m_result_set[(int)_index];
        }

        public List<Result_Set> getResultSet()
        {
            return m_result_set;
        }


        public void setRowsAffected(long _rows_affected)
        {
            m_rows_affected = _rows_affected;
        }

        public long getRowsAffected()
        {
            return m_rows_affected;
        }

        protected List<Result_Set> m_result_set = new List<Result_Set>(); // result_set

        protected long m_rows_affected;
    }
}
