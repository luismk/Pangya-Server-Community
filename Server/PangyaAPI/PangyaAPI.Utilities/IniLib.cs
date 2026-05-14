using System;
using System.Collections.Generic;
using System.IO;
using PangyaAPI.Utilities.Log;
namespace PangyaAPI.Utilities
{
    /// <summary>
    ///Criado por LuisMK, time=> 24/02/2022 as 19:36 PM
    ///,atualizado a string para melhoria de leitura =>
    ///14 11 2022
    /// </summary>
    public class IniHandle
    {
        private readonly string fn;
        private readonly string[] lines;

        // Constructor

        public IniHandle(string _filename)
        {
            try
            {
                //caso o arquivo não existir, é lançado uma exceção
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + _filename) == false)
                {
                    _smp.message_pool.getInstance().push(new message($"[IniLib::Init][Log]: File no Exist: {_filename}", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    throw new exception($"[IniHandle::construtor][ErrorSystem] File no Exist: {_filename}");
                }
                else
                {
                    //caso o arquivo existir, é adicionado um local + nome do arquivo
                    var FilePath = AppDomain.CurrentDomain.BaseDirectory + _filename;
                    using (var fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (StreamReader stream = new StreamReader(fs))
                    {
                        fn = FilePath;
                        lines = stream.ReadToEnd().Split(new[] { "\n", "\r\n" }, StringSplitOptions.None);
                    }

                }

            }
            //caso caia no exception
            catch (exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }



        // Get group-range
        int[] GroupPos(string group = "")
        {
            if (group == "")
            {
                return new[] { 0, 0 };// No Group.
            }

            if (lines == null)
            {
                return new[] { 0, 0 };// No Group.
            }

            string lowerline;
            int[] ret = new[] { -1, -1 };
            for (int i = 0; i < lines.Length; i++)
            {
                lowerline = lines[i].ToLower();

                if (ret[0] < 0)
                {
                    if (lowerline.Contains("[" + group.ToLower() + "]"))
                    {
                        ret[0] = i; // Group found.
                    }

                }
                else
                {
                    if (lowerline.StartsWith("[") || i == lines.Length - 1) // next group or end of file.
                    {
                        ret[1] = lines.Length; // End of group found.
                        return (ret);
                    }
                }
            }
            _smp.message_pool.getInstance().push(new message("[IniLib::GroupPos][Log]: Unable to find Group '" + group + "' in configuration file '" + fn + "'.", type_msg.CL_FILE_LOG_AND_CONSOLE));
            return ret; // Group not found.
        }

        public string ReadString(string group, string key, string _default = "", int min = int.MinValue, int max = int.MaxValue)
        {
            var group_index = GroupPos(group);
            if (group_index[0] < 0 || group_index[1] > (lines == null ? 0 : lines.Length))
            {
                throw new exception("[IniLib::ReadString][ErrorSystem] in " + group + ", no exist key: " + key);
            }

            string[] tarr = null;
            for (int i = group_index[0]; i < group_index[1]; i++)
            {
                if (lines[i].StartsWith(key, StringComparison.OrdinalIgnoreCase))
                {
                    tarr = lines[i].Split(new[] { "=" }, StringSplitOptions.None);
                    break;
                }
            }

            string ret = "";
            if (tarr == null)
            {
                return _default;
            }
            else
            {
                var value = tarr[1];
                bool chk = true;
                if (value.Contains("   ") || value.Contains("\t"))
                {
                    ret = value.Replace("   ", "");
                    if (value.Contains("\t"))
                    {
                        ret = value.Replace("\t", "");
                    }
                    chk = false;
                }
                if (value.Contains(" ") && chk)
                {
                    ret = tarr[1].Replace(" ", "");
                }
            }

            // Assuming integer value and checking min / max values.
            if (min != int.MinValue || max != int.MaxValue)
            {
                int iret = Convert.ToInt32(ret);
                if (iret < min || iret > max)
                {
                    _smp.message_pool.getInstance().push(new message("[IniLib::ReadString][Log]: Invalid value '" + iret.ToString() + "' (Min: " + min.ToString() + " Max: " + max.ToString() + ") for '" + key + "' in configuration file '" + fn + "'. Defaulting value...", type_msg.CL_FILE_LOG_AND_CONSOLE));
                    ret = _default;
                }
            }

            return ret;
        }
        public Int32 readInt(string section, string key, int def = 0)
        {
            return Convert.ToInt32(ReadString(section, key, def.ToString()));
        }
        /// <summary>
        /// Ler o arquivo .ini e retorna Int32
        /// </summary>
        /// <param name="section">Seção = cabeçario [Config]</param>
        /// <param name="key">Local = nomedealgo = 0 </param>
        /// <param name="def">padrao = caso não encontre o valor no key, retorna o def</param>
        /// <returns>string</returns>
        public Int32 ReadInt32(string section, string key, int def = 0)
        {
            return Convert.ToInt32(ReadString(section, key, def.ToString()));
        }
        /// <summary>
        /// Ler o arquivo .ini e retorna UInt32
        /// </summary>
        /// <param name="section">Seção = cabeçario [Config]</param>
        /// <param name="key">Local = nomedealgo = 0 </param>
        /// <param name="def">padrao = caso não encontre o valor no key, retorna o def</param>
        /// <returns>string</returns>
        public UInt32 ReadUInt32(string section, string key, uint def = 0)
        {
            var _key = ReadString(section, key, def.ToString());
            return Convert.ToUInt32(_key);
        }
        /// <summary>
        /// Ler o arquivo .ini e retorna Int64
        /// </summary>
        /// <param name="section">Seção = cabeçario [Config]</param>
        /// <param name="key">Local = nomedealgo = 0 </param>
        /// <param name="def">padrao = caso não encontre o valor no key, retorna o def</param>
        /// <returns>string</returns>
        public Int64 ReadInt64(string section, string key, long def = 0)
        {
            return Convert.ToInt64(ReadString(section, key, def.ToString()));

        }
        /// <summary>
        /// Ler o arquivo .ini e retorna UInt64
        /// </summary>
        /// <param name="section">Seção = cabeçario [Config]</param>
        /// <param name="key">Local = nomedealgo = 0 </param>
        /// <param name="def">padrao = caso não encontre o valor no key, retorna o def</param>
        /// <returns>string</returns>
        public UInt64 ReadUInt64(string section, string key, ulong def = 0)
        {
            return Convert.ToUInt64(ReadString(section, key, def.ToString()));

        }
        /// <summary>
        /// Ler o arquivo .ini e retorna Int16
        /// </summary>
        /// <param name="section">Seção = cabeçario [Config]</param>
        /// <param name="key">Local = nomedealgo = 0 </param>
        /// <param name="def">padrao = caso não encontre o valor no key, retorna o def</param>
        /// <returns>string</returns>
        public Int16 ReadInt16(string section, string key, short def = 0)
        {
            return Convert.ToInt16(ReadString(section, key, def.ToString()));
        }
        /// <summary>
        /// Ler o arquivo .ini e retorna UInt16
        /// </summary>
        /// <param name="section">Seção = cabeçario [Config]</param>
        /// <param name="key">Local = nomedealgo = 0 </param>
        /// <param name="def">padrao = caso não encontre o valor no key, retorna o def</param>
        /// <returns>string</returns>
        public UInt16 ReadUInt16(string section, string key, ushort def = 0)
        {
            return Convert.ToUInt16(ReadString(section, key, def.ToString()));
        }
        /// <summary>
        /// Ler o arquivo .ini e retorna Byte
        /// </summary>
        /// <param name="section">Seção = cabeçario [Config]</param>
        /// <param name="key">Local = nomedealgo = 0 </param>
        /// <param name="def">padrao = caso não encontre o valor no key, retorna o def</param>
        /// <returns>string</returns>
        public Byte ReadByte(string section, string key, Byte def = 0)
        {
            return Convert.ToByte(ReadString(section, key, def.ToString()));
        }
        /// <summary>
        /// Ler o arquivo .ini e retorna SByte
        /// </summary>
        /// <param name="section">Seção = cabeçario [Config]</param>
        /// <param name="key">Local = nomedealgo = 0 </param>
        /// <param name="def">padrao = caso não encontre o valor no key, retorna o def</param>
        /// <returns>string</returns>
        public SByte ReadSByte(string section, string key, sbyte def = 0)
        {
            return Convert.ToSByte(ReadString(section, key, def.ToString()));
        }

        /// <summary>
        /// Ler o arquivo .ini e retorna bool
        /// </summary>
        /// <param name="section">Seção = cabeçario [Config]</param>
        /// <param name="key">Local = nomedealgo = 0 </param>
        /// <param name="def">padrao = caso não encontre o valor no key, retorna o def</param>
        /// <returns>string</returns>
        public bool ReadBool(string section, string key, bool def = false)
        {
            var result = ReadUInt32(section, key, (uint)(def ? 1 : 0));
            return Convert.ToBoolean(result);
        }

        public List<string> ReadAllValues(string section, string key)
        {
            var values = new List<string>();
            string currentSection = null;

            foreach (var rawLine in File.ReadLines(fn))
            {
                var line = rawLine.Trim();

                if (string.IsNullOrEmpty(line) || line.StartsWith("//") || line.StartsWith(";"))
                    continue;

                // Verifica nova seção
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line.Substring(1, line.Length - 2).Trim();
                    continue;
                }

                if (!string.Equals(currentSection, section, StringComparison.OrdinalIgnoreCase))
                    continue;

                var parts = line.Split(new[] { ':' }, 2);
                if (parts.Length != 2)
                    continue;

                var k = parts[0].Trim();
                var v = parts[1].Trim();

                if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
                    values.Add(v);
            }

            return values;
        }

    }
}
