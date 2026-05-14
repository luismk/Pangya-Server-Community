using response = PangyaAPI.SQL.Response;
namespace PangyaAPI.SQL.Manager
{
    public class exec_query : System.IDisposable
    {
        public enum QUERY_TYPE : byte
        {
            _QUERY,
            _PROCEDURE,
            _INSERT,
            _UPDATE,
            _DELETE
        }
        public exec_query()
        {
            m_type = 0;
        }
        public exec_query(string _query, byte _type = 0)
        {
            setQuery(_query);
            m_type = _type;
        }


        public exec_query(string _name_proc, string[] _params_proc, byte _tipo = (byte)QUERY_TYPE._PROCEDURE)
        {
            setQuery(_name_proc);
            setParam(_params_proc);
        }



        public void Dispose() { }


        public string getQuery() { return m_query; }

        public string[] getParams() { return m_params; }

        public response getRes() { return m_res; }


        public void setQuery(string _query) { }

        public void setParam(string[] _params) { }


        public void setRes(response _res) { }


        public void waitEvent(int milliseconds = -1) { }

        public void setEvent() { }

        public void resetEvent() { }

        public void pulseEvent() { }


        public byte getType()
        {
            return m_type;
        }


        public static void enter() { }

        public static void release() { }

        public static int getSpinCount() { return m_spin_count; }

        protected string m_query; // Aqui pode ser nome procedure, quanto query completa
        protected string[] m_params;
        protected response m_res;

        protected byte m_type;
        protected static int m_spin_count;
    }
}
