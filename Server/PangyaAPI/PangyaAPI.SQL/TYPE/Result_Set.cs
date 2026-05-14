using System;
using System.Data;
using PangyaAPI.Utilities;
namespace PangyaAPI.SQL
{
    public class ctx_res
    {
        public object[] data { get; set; }
        public object[] data_row;
        public uint cols { get; set; }
        public ctx_res next;

        //public T ConvertToClass<T>() where T : new()
        //{
        //    return data_row.ToObject<T>();
        //}

        public bool IsNotNull(int column)
        {
            try
            {
                if (data == null || column < 0 || column >= data.Length)
                {
                    return false; // Array nulo ou índice inválido é considerado vazio
                }

                var value = data[column];

                // Verifica se o valor é nulo ou uma string vazia
                if (value == null || (value is string str && string.IsNullOrEmpty(str)))
                {
                    return false;
                }
                return true; // O valor não é nulo, nem vazio
            }
            catch
            {
                return false;
            }
        }

        public bool GetBoolean(int colum)
        {
            return data[colum] != null && Convert.ToBoolean(data[colum]);
        }

        public float GetFloat(int colum)
        {
            return data[colum] != null ? Convert.ToSingle(data[colum]) : 0f;
        }

        public int GetInt32(int colum)
        {
            return data[colum] != null ? Convert.ToInt32(data[colum]) : 0;
        }

        public uint GetUInt32(int colum)
        {
            return data[colum] != null ? Convert.ToUInt32(data[colum]) : 0;
        }

        public long GetInt64(int colum)
        {
            return data[colum] != null ? Convert.ToInt64(data[colum]) : 0L;
        }

        public ulong GetUInt64(int colum)
        {
            return data[colum] != null ? Convert.ToUInt64(data[colum]) : 0UL;
        }

        public byte GetByte(int colum)
        {
            return data[colum] != null ? Convert.ToByte(data[colum]) : (byte)0;
        }

        public sbyte GetSByte(int colum)
        {
            return data[colum] != null ? Convert.ToSByte(data[colum]) : (sbyte)0;
        }

        public short GetInt16(int colum)
        {
            return data[colum] != null ? Convert.ToInt16(data[colum]) : (short)0;
        }

        public ushort GetUInt16(int colum)
        {
            return data[colum] != null ? Convert.ToUInt16(data[colum]) : (ushort)0;
        }

        public DateTime GetDateTime(int colum)
        {
            return data[colum] != null ? Convert.ToDateTime(data[colum]) : DateTime.MinValue;
        }

        public string GetString(int colum)
        {
            return data[colum]?.ToString() ?? string.Empty;
        }

    }

    public class Result_Set : System.IDisposable
    {
        public enum STATE_TYPE : uint
        {
            HAVE_DATA,
            _NO_DATA,
            _UPDATE_OR_DELETE,
            _ERROR
        }
        public Result_Set(uint _state)
        {
            this.m_state = (STATE_TYPE)_state;
            this.m_lines_affected = -1;
            this.m_data = null;
            this.m_curr_data = null;
            this.m_lines = 0;
            this.m_cols = 0;
        }

        public Result_Set(uint _state,
            uint _cols,
            int _lines_affected)
        {
            this.m_state = (STATE_TYPE)_state;
            this.m_lines_affected = _lines_affected;
            this.m_data = null;
            this.m_curr_data = null;
            this.m_lines = 0;
            this.m_cols = _cols;
        }

        public Result_Set(uint _state, uint _lines_affected, uint _cols, object[] row) : this(_state, _cols, (int)_lines_affected)
        {
            addLine();   // Adiciona linha
            if (row != null)
                setRow(row);
        }

        public void Dispose()
        {
            destroy();
        }

        public void destroy()
        {
            if (m_data != null)
            {
                ctx_res pNext = null;

                while (m_data != null)
                {
                    pNext = m_data.next; // Armazena o próximo nó antes de modificar m_data

                    if (m_data.data != null)
                    {
                        for (uint i = 0; i < m_data.cols; ++i)
                        {
                            m_data.data[i] = new string[] { "0" }; // Definindo para o caractere nulo
                        }

                        m_data.data = null; // Libera o array de dados
                    }

                    m_data = pNext; // Avança para o próximo
                }
            }

            m_data = null; // Garante que m_data seja nulo no final
        }

        public uint reserve_cols(int _cols)
        {
            if (_cols > 0)
            {
                addLineData();

                m_cols = m_curr_data.cols = (uint)_cols;

                if (m_curr_data.data == null)
                {
                    m_curr_data.data = new string[_cols][]; // Preenchido com 0 por padrão
                }
            }

            return (uint)_cols;
        }

        public ctx_res getFirstLine()
        {
            return m_data;
        }

        public void setLinesAffected(int _lines_affected)
        {
            m_lines_affected = _lines_affected;
        }

        public object getColAt(uint _index)
        {
            if (m_curr_data == null)
            {
                throw new Exception("Nao tem nenhum dados reservado, reserve primeiro.");
            }

            if ((int)_index < 0 || _index >= m_curr_data.cols)
            {
                throw new Exception("Index out of range.");
            }

            return m_curr_data.data[_index];
        }

        public uint getNumLines()
        {
            return m_lines;
        }

        public uint getNumCols()
        {
            return m_cols;
        }

        public void setState(uint _state)
        {
            m_state = (STATE_TYPE)_state;
        }

        public uint getState()
        {
            return (uint)m_state;
        }

        public void addLine()
        {
            if (m_state == STATE_TYPE.HAVE_DATA)
            {
                reserve_cols((int)m_cols);
            }
        }

        protected ctx_res addLineData()
        {
            if (m_data == null)
            {
                m_data = new ctx_res();

                m_curr_data = m_data;

                // Init Dados
                m_curr_data.next = null;
                m_curr_data.cols = 0;
                m_curr_data.data = null;
            }
            else
            {
                m_curr_data.next = new ctx_res();

                m_curr_data = m_curr_data.next;

                // Init Dados
                m_curr_data.next = null;
                m_curr_data.cols = 0;
                m_curr_data.data = null;
            }

            ++m_lines;

            return m_curr_data;
        }

        public void setRow(object[] dataRow)
        {
            var data = dataRow;
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] is DBNull)
                    data[i] = null;
            }
            m_data.data = data;
            m_data.data_row = dataRow;
        }

        protected STATE_TYPE m_state = 0;
        protected int m_lines_affected = 0;

        protected uint m_lines = 0;
        protected uint m_cols = 0;
        protected ctx_res m_data;
        protected ctx_res m_curr_data;
    }
}

