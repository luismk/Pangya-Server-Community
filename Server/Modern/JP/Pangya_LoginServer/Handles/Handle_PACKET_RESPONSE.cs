using Pangya_LoginServer.PangyaEnums;
using Pangya_LoginServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Models;
using PangyaAPI.Utilities;
namespace Pangya_LoginServer.Handles
{
    public static class Handle_PACKET_RESPONSE
    { 
        public static Packet pacote001(Player _session, byte option = 0, int sub_opt = 0, string message = "")
        {
            var subID = (SubLoginCode)option;
            var p = new Packet(0x01);
            p.WriteByte(option);
            switch (option)
            {
                case 0:
                    p.WriteString(_session.UserInfo.Login);
                    p.WriteUInt32(_session.UserInfo.UID);
                    p.WriteUInt32(_session.UserInfo.Capability);
                    p.WriteByte(1);           // 1 Level, 1 pc bang(ACHO), com base no S4
                    p.WriteInt32(0);// valor 0 Unknown
                    p.WriteByte(1);// nada
                    p.WriteInt32(5);// valor 5 Unknown, opcao 0 é pra enviar sem a chave, 
                    p.WriteTime();   // - JP S9 ler mais ignora ele
                    p.WriteString(_session.UserInfo.ShopToken);// Alguma AuthKey aleatória para minha conta que eu não sei - JP S9 ler mais ignora ele
                    p.WriteUInt64(0); // Unknown valor - JP S9 ler mais ignora ele
                    p.WriteString(_session.UserInfo.NickName);
                    break;
                case 6:
                case 1:
                    p.WriteInt32(0);  // add 4 bytes vazios
                    break;
                case 0xD8:
                    // First Login
                    p.WriteInt32(-1);
                    p.WriteInt16(0);
                    break;
                case 0xD9:
                    p.WriteInt16(0);
                    break;
                case 0x0c:
                case 0xE2:
                case 16:
                    p.WriteInt32(sub_opt);
                    break;
                case 7:
                    var tempo = _session.UserInfo.BlockFlag.State.TimeBlock / 60 / 60/*Hora*/; // Hora
                    //24(Horas)x15(Dias)=360(horas)
                    p.WriteInt32(_session.UserInfo.BlockFlag.State.TimeBlock == -1 || tempo == 0 ? 360/*Menos de uma hora*/ : tempo);   // Block Por Tempo
                    if (!string.IsNullOrEmpty(message))
                    {
                        p.WriteString(message);
                    }
                    break;

                default:
                    break;
            }
            return p;
        }

        // Mensagem do Tutorial
        public static Packet pacote00F(Player _session, int option = 0)
        {
            var p = new Packet(0x0F);
            p.WriteByte(option);
            p.WriteString(_session.UserInfo.Login);
            p.WriteUInt32(0);                             // valor 0 Unknown
            p.WriteUInt32(5);                             // valor 5 Unknown
            p.WriteString(UtilTime.formatDateLocal(0));   // Time Build Login Server (ACHO)							- JP S9 ler mais ignora ele
            p.WriteString(_session.UserInfo.ShopToken);                      // Alguma AuthKey aleatória para minha conta que eu não sei - JP S9 ler mais ignora ele
            return p;
        }

        public static Packet pacote002(List<ServerInfo> v_element)
        {

            var p = new Packet(0x02);

            p.WriteByte((byte)(v_element.Count & 0xFF)); // 1 Game Server online

            for (int i = 0; i < v_element.Count; i++)
                p.WriteBytes(v_element[i].ToArray());

            return p;
        }

        public static Packet pacote003(string AuthKeyLogin, int option = 0)
        {

            var p = new Packet(0x03);

            p.WriteInt32(option);

            p.WriteString(AuthKeyLogin);

            return p;
        }

        public static Packet pacote006(ChatMacroUser _mu)
        {
            var p = new Packet(0x06);

            p.WriteBytes(_mu.ToArray());

            return p;
        }

        public static Packet pacote009(List<ServerInfo> v_element)
        {

            var p = new Packet(0x09);

            p.WriteByte((byte)(v_element.Count & 0xFF)); // nenhum Msn Server on

            for (int i = 0; i < v_element.Count; i++)
                p.WriteBytes(v_element[i].ToArray());

            return p;
        }


        public static Packet pacote00E(Player _session, string nick, int option = 0, uint error = 0)
        {
            var p = new Packet(0x0E);

            p.WriteInt32(option);

            if (option == 0)
                p.WriteString(nick);
            else if (option == 12)
                p.WriteUInt32(error);

            return p;
        }



        public static Packet pacote010(string AuthKey)
        {

            var p = new Packet(0x10);

            p.WriteString(AuthKey);

            return p;
        }

        public static Packet pacote011(int option = 0)
        {

            var p = new Packet(0x11);

            p.WriteUInt16((ushort)option);

            return p;
        }

        // Antigo pacote00E - Error Message
        public static Packet Error(int errorCode)
        {
            var p = new Packet(0x0E);
            p.WriteInt32(errorCode);
            return p;
        }
    }
}
