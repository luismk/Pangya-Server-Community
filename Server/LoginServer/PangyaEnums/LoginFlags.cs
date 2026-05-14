using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pangya_LoginServer.PangyaEnums
{
    public enum SubLoginCode
    {
        InvalidoIdPw = 0x01,
        InvalidoId = 0x02,
        Login = 0x3,
        UsuarioEmUso = 0x04,
        Banido = 0x05,
        UsernameOuSenhaInvalido = 0x06,
        ContaSuspensa = 0x07,
        Unk = 0x08,
        Player13AnosOuMenos = 0x09, 
        SSNIncorreto = 0x0C,
        UsuarioIncorreto = 0x0D,
        OnlyUserAllowed = 0x0E,
        ServerInMaintenance = 0x0F, //Cannot login due to server maintenance
        NaoDisponivelNaSuaArea = 0x10, //By LuisMk
        CreateNickName_US = 0xD8, //by LuisMK (usado no US)
        CreateNickName = 0xD9, //by LuisMK (usado no TH)
    }
}
