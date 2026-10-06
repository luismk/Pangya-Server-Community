using Pangya_GameServer.Manager;
using Pangya_GameServer.Models;
using Pangya_GameServer.Repository;
using Pangya_GameServer.Session;
using PangyaAPI.Network;
using PangyaAPI.Network.Core;
using PangyaAPI.Utilities;
using PangyaAPI.Utilities.Log;
using snmdb;
using static Pangya_GameServer.Models.DefineConstants;

namespace Pangya_GameServer.Handles
{
    public class Handle_PLAYER_MAKE_TUTORIAL : HandleBase<Player, Packet_EXAMPLE>
    {
        public override async Task Handle()
        {
            Packet p = new Packet();

            try
            {
                stItem item = new stItem();

                item.type = 2;
                item.id = -1;

                string msg = "";

                //eu poderia fazer melhor, alias sem outras class(structs internas)
                var rmt = RequestMakeTutorial.Load(Packet.Message);

                switch (rmt.uTipo.stTipo.tipo)
                {
                    case 0: // Rookie
                        {
                            if (rmt.uTipo.stTipo.tipo == 0 && (Player.UserInfo.Tutorial.rookie & rmt.uValor.stValor.rookie.ucbyte) != 0)
                            {
                                throw new exception("[Error] Normal [UID=" + Player.UserInfo.UID + "] tentou fazer tutorial[Type=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele ja concluiu esse tutorial. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    550, 0x5300551));
                            }

                            if (rmt.uValor.stValor.rookie.st8bit._bit2 || rmt.uValor.stValor.rookie.st8bit._bit3)
                            {
                                if (Player.UserInfo.Tutorial.rookie < 3) // Error não concluiu os outros tutoriais para liberar esse
                                {
                                    throw new exception("[Error] Normal [UID=" + Player.UserInfo.UID + "] tentou fazer tutorial[Type=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele nao concluiu os outros tutoriais para poder completar o Rookie. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        553, 0x5300554));
                                }
                            }
                            else if (rmt.uValor.stValor.rookie.st8bit._bit4)
                            {
                                if ((Player.UserInfo.Tutorial.rookie & 7) <= 3) // Error não concluiu os outros tutoriais para liberar esse
                                {
                                    throw new exception("[Error] Normal [UID=" + Player.UserInfo.UID + "] tentou fazer tutorial[Type=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele nao concluiu os outros tutoriais para poder completar o Rookie. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        553, 0x5300554));
                                }
                            }
                            else if (rmt.uValor.stValor.rookie.st8bit._bit6)
                            {
                                if ((Player.UserInfo.Tutorial.rookie & 11) <= 3) // Error não concluiu os outros tutoriais para liberar esse
                                {
                                    throw new exception("[Error] Normal [UID=" + Player.UserInfo.UID + "] tentou fazer tutorial[Type=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele nao concluiu os outros tutoriais para poder completar o Rookie. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        553, 0x5300554));
                                }
                            }
                            else if (rmt.uValor.stValor.rookie.st8bit._bit5)
                            {
                                if ((Player.UserInfo.Tutorial.rookie & 15) <= 3) // Error não concluiu os outros tutoriais para liberar esse
                                {
                                    throw new exception("[Error] Normal [UID=" + Player.UserInfo.UID + "] tentou fazer tutorial[Type=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele nao concluiu os outros tutoriais para poder completar o Rookie. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        553, 0x5300554));
                                }
                            }
                            else if (((rmt.uValor.stValor.rookie.ucbyte - 1) & Player.UserInfo.Tutorial.rookie) != (rmt.uValor.stValor.rookie.ucbyte - 1)) // Error não concluiu os outros tutoriais para liberar esse
                            {
                                throw new exception("[Error] Normal [UID=" + Player.UserInfo.UID + "] tentou fazer tutorial[Type=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele nao concluiu os outros tutoriais para poder completar o Rookie. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    553, 0x5300554));
                            }

                            Player.UserInfo.Tutorial.rookie |= rmt.uValor.ulValor;

                            // Send Item Reward Clear Tutorial
                            switch (rmt.uValor.stValor.rookie.st8bit.whatBit())
                            {
                                case 1: // Pang Mastery
                                    item._typeid = 0x1A000002;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 10);
                                    break;
                                case 2: // Tranquilizande de Cookies
                                    item._typeid = 0x1800000B;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 3);
                                    break;
                                case 3: // Power Milk
                                    item._typeid = 0x18000025;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 3);
                                    break;
                                case 4: // Olho Magico
                                    item._typeid = 0x18000005;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 3);
                                    break;
                                case 5: // Açai
                                    item._typeid = 0x18000004;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 3);
                                    break;
                                case 6: // Duostar lucky pangya cookie
                                    item._typeid = 0x1800000A;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 3);
                                    break;
                                case 7: // Spin Mastery(Guaraná)
                                    item._typeid = 0x18000000;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 3);
                                    break;
                                case 8: // Pang Pouch
                                    item._typeid = PANG_POUCH_TYPEID;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 1000);
                                    break;
                                case 0:
                                default:
                                    throw new exception("[Error] Normal [UID=" + Player.UserInfo.UID + "] tentou fazer tutorial[Type=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], o valor do tutorial é desconhecido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        555, 0x5300556));
                            }

                            msg = "NICE TUTORIAL ROOKIE CLEAR";

                            // Send Item para mailbox do Player que concluiu o Tutorial
                            MailManager.SendMessageWithItem(0,
                                Player.UserInfo.UID, msg, item);

                            _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_MAKE_TUTORIAL][Sucess] Normal [UID=" + Player.UserInfo.UID + "] Concluiu Tutorial Rookie", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // Concluiu o Tutorial Rookie
                            if ((Player.UserInfo.Tutorial.rookie & 0xFF) != 0 && rmt.uTipo.stTipo.finish != 0)
                            {

                                List<stItem> v_item = new List<stItem>();

                                stItem itemFinal = new stItem();
                                itemFinal.type = 2;
                                itemFinal.id = (int)-1;
                                itemFinal._typeid = 0x1C000000; // Papel
                                itemFinal.qntd = Convert.ToInt32(itemFinal.STDA_C_ITEM_QNTD = 1);

                                v_item.Add(new stItem(itemFinal));

                                itemFinal = new stItem();
                                itemFinal.type = 2;
                                itemFinal.id = (int)-1;
                                itemFinal._typeid = 0x10000014; // Air Knight Lucky Set
                                itemFinal.qntd = Convert.ToInt32(itemFinal.STDA_C_ITEM_QNTD = 1);

                                v_item.Add(new stItem(itemFinal));

                                msg = "NICE ALL TUTORIAL ROOKIE CLEAR";

                                // Send Item para mailbox do Player que concluiu todos os Tutoriais Rookie
                                MailManager.SendMailWithItem(0,
                                    Player.UserInfo.UID, msg, v_item);

                                // UPDATE ON DB
                                NormalManagerDB.Instance.add(14,
                                     new CmdTutoEventClear(Player.UserInfo.UID, CmdTutoEventClear.T_ROOKIE),
                                    null, null);

                                _smp.LogManager.Instance.push(new AppMessage("[Tutorial][Sucess] Normal [UID=" + Player.UserInfo.UID + "] Concluiu Todos Tutoriais Rookie", type_msg.CL_FILE_LOG_AND_CONSOLE)); // UPDATE ON DB
                            }
                            break;
                        }
                    case 1: // Beginner
                        {
                            if (rmt.uTipo.stTipo.tipo == 1 && (Player.UserInfo.Tutorial.beginner & rmt.uValor.stValor.beginner.ucbyte) != 0)
                            {
                                throw new exception("[Error] Normal [UID=" + Player.UserInfo.UID + "] tentou fazer tutorial[Type=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele ja concluiu esse tutorial. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    550, 0x5300551));
                            }

                            // Check Rookie Concluido
                            if (Player.UserInfo.Tutorial.rookie == 1 && 0xFF != 0xFF)
                            {
                                throw new exception("[Error] Normal [UID=" + Player.UserInfo.UID + "] tentou fazer tutorial[Type=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele nao concluiu o tutorial rookie. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    554, 0x5300555));
                            }

                            RequestMakeTutorial.u2 tutu = new RequestMakeTutorial.u2() { ulValor = Player.UserInfo.Tutorial.beginner };

                            if (rmt.uValor.stValor.beginner.st8bit._bit1 || rmt.uValor.stValor.beginner.st8bit._bit2)
                            {
                                if (tutu.stValor.beginner.ucbyte < 1)
                                {
                                    throw new exception("[Error] Normal [UID=" + Player.UserInfo.UID + "] tentou fazer tutorial[Type=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele nao concluiu os outros tutoriais para poder completar o Beginner. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        553, 0x5300554));
                                }
                            }
                            else if (rmt.uValor.stValor.beginner.st8bit._bit4 || rmt.uValor.stValor.beginner.st8bit._bit5)
                            {
                                if (tutu.stValor.beginner.ucbyte < 15)
                                {
                                    throw new exception("[Error] Normal [UID=" + Player.UserInfo.UID + "] tentou fazer tutorial[Type=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele nao concluiu os outros tutoriais para poder completar o Beginner. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        553, 0x5300554));
                                }
                            }
                            else if (((rmt.uValor.stValor.beginner.ucbyte - 1) & tutu.stValor.beginner.ucbyte) != (rmt.uValor.stValor.beginner.ucbyte - 1))
                            {
                                throw new exception("[Error] Normal [UID=" + Player.UserInfo.UID + "] tentou fazer tutorial[Type=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele nao concluiu os outros tutoriais para poder completar o Beginner. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    553, 0x5300554));
                            }

                            Player.UserInfo.Tutorial.beginner |= rmt.uValor.ulValor;

                            // Send Item Reward Clear Tutorial
                            switch (rmt.uValor.stValor.beginner.st8bit.whatBit())
                            {
                                case 1: // Pang Mastery
                                    item._typeid = 0x1A000002;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 10);
                                    break;
                                case 2: // Safety
                                    item._typeid = 0x18000028;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 1);
                                    break;
                                case 3: // Corta vento
                                    item._typeid = 0x18000006;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 1);
                                    break;
                                case 4: // Duostar lucky pangya cookie
                                    item._typeid = 0x1800000A;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 3);
                                    break;
                                case 5: // Spin Mastery(Guaraná)
                                    item._typeid = 0x18000000;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 4);
                                    break;
                                case 6: // Banana
                                    item._typeid = 0x18000001;
                                    item.qntd = Convert.ToInt32(item.STDA_C_ITEM_QNTD = 3);
                                    break;
                                case 7:
                                case 8:
                                case 0:
                                default:
                                    throw new exception("[Error] Normal [UID=" + Player.UserInfo.UID + "] tentou fazer tutorial[Type=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], o valor do tutorial é desconhecido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                        555, 0x5300556));
                            }

                            msg = "NICE TUTORIAL BEGINNER CLEAR";

                            // Send Item para mailbox do Player que concluiu o Tutorial
                            MailManager.SendMessageWithItem(0,
                                Player.UserInfo.UID, msg, item);

                            _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_MAKE_TUTORIAL][Sucess] Normal [UID=" + Player.UserInfo.UID + "] Concluiu Tutorial Beginner", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // Concluiu o Tutorial Beginner
                            if (Player.UserInfo.Tutorial.beginner == (0x3F << 8))
                            {

                                List<stItem> v_item = new List<stItem>();

                                stItem itemFinal = new stItem();
                                itemFinal.type = 2;
                                itemFinal.id = (int)-1;
                                itemFinal._typeid = 0x18000027; // Power +15y Item
                                itemFinal.qntd = (int)(itemFinal.STDA_C_ITEM_QNTD = 10);

                                v_item.Add(new stItem(itemFinal));

                                itemFinal = new stItem();
                                itemFinal.type = 2;
                                itemFinal.id = (int)-1;
                                itemFinal._typeid = PANG_POUCH_TYPEID; // Pang Pouch 10k Pang
                                itemFinal.qntd = (int)(itemFinal.STDA_C_ITEM_QNTD = 10000);

                                v_item.Add(new stItem(itemFinal));

                                msg = "NICE ALL TUTORIAL BEGINNER CLEAR";

                                // Send Item para mailbox do Player que concluiu todos os Tutoriais Beginner
                                MailManager.SendMailWithItem(0,
                                    Player.UserInfo.UID, msg, v_item);

                                // UPDATE ON DB
                                NormalManagerDB.Instance.add(14,
                                     new CmdTutoEventClear(Player.UserInfo.UID, CmdTutoEventClear.T_BEGINNER),
                                    null, null);

                                _smp.LogManager.Instance.push(new AppMessage("[Tutorial][Sucess] Normal [UID=" + Player.UserInfo.UID + "] Concluiu Todos Tutoriais Beginner", type_msg.CL_FILE_LOG_AND_CONSOLE)); // UPDATE ON DB
                            }
                            break;
                        }
                    case 2: // Advancer(ACHO)
                        {
                            if (rmt.uTipo.stTipo.tipo == 2 && (Player.UserInfo.Tutorial.advancer & rmt.uValor.stValor.advancer.ucbyte) != 0)
                            {
                                throw new exception("[Error] Normal [UID=" + Player.UserInfo.UID + "] tentou fazer tutorial[Type=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele ja concluiu esse tutorial. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    550, 0x5300551));
                            }

                            // Check Rookie Concluido
                            if (Player.UserInfo.Tutorial.rookie == 1 && 0xFF != 0xFF)
                            {
                                throw new exception("[Error] Normal [UID=" + Player.UserInfo.UID + "] tentou fazer tutorial[Type=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele nao concluiu o tutorial rookie. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    554, 0x5300555));
                            }

                            // Check Beginner Concluido
                            if (Player.UserInfo.Tutorial.beginner == 1 && 0x3F != 0x3F)
                            {
                                throw new exception("[Error] Normal [UID=" + Player.UserInfo.UID + "] tentou fazer tutorial[Type=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele nao concluiu o tutorial Beginner. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    554, 0x5300555));
                            }

                            RequestMakeTutorial.u2 tutu = new RequestMakeTutorial.u2() { ulValor = Player.UserInfo.Tutorial.advancer };

                            if (((rmt.uValor.stValor.advancer.ucbyte - 1) & tutu.stValor.advancer.ucbyte) != (rmt.uValor.stValor.advancer.ucbyte - 1))
                            {
                                throw new exception("[Error] Normal [UID=" + Player.UserInfo.UID + "] tentou fazer tutorial[Type=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], mas ele nao concluiu os outros tutoriais para poder completar o Advancer. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                                    553, 0x5300554));
                            }

                            Player.UserInfo.Tutorial.advancer |= rmt.uValor.ulValor;

                            msg = "NICE TUTORIAL ADVANCER CLEAR";

                            // Send Item para mailbox do Player que concluiu o Tutorial
                            MailManager.SendMessageWithItem(0,
                                Player.UserInfo.UID, msg, item);

                            _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_MAKE_TUTORIAL][Sucess] Normal [UID=" + Player.UserInfo.UID + "] Concluiu Tutorial Advancer", type_msg.CL_FILE_LOG_AND_CONSOLE));

                            // Concluiu o Tutorial Advancer (ACHO)
                            if (Player.UserInfo.Tutorial.advancer == (0x7 << 16) && rmt.uTipo.stTipo.finish != 0)
                            {

                                // Esse não tem estou deixando por questão de quando eu implementar ou só para ter mesmo
                                List<stItem> v_item = new List<stItem>();

                                stItem itemFinal = new stItem();
                                itemFinal.type = 2;
                                itemFinal.id = (int)-1;
                                itemFinal._typeid = 0x18000000; // Spin Mastery(Guaraná)
                                itemFinal.qntd = (int)(itemFinal.STDA_C_ITEM_QNTD = 1);

                                v_item.Add(new stItem(itemFinal));

                                itemFinal = new stItem();
                                itemFinal.type = 2;
                                itemFinal.id = (int)-1;
                                itemFinal._typeid = PANG_POUCH_TYPEID; // Pang Pouch 30k Pang
                                itemFinal.qntd = (int)(itemFinal.STDA_C_ITEM_QNTD = 30000);

                                v_item.Add(new stItem(itemFinal));

                                msg = "NICE ALL TUTORIAL ADVANCER CLEAR";

                                // Send Item para mailbox do Player que concluiu todos os Tutoriais Advancer
                                MailManager.SendMailWithItem(0,
                                    Player.UserInfo.UID, msg, v_item);

                                // UPDATE ON DB
                                NormalManagerDB.Instance.add(14,
                                     new CmdTutoEventClear(Player.UserInfo.UID, CmdTutoEventClear.T_ADVANCER),
                                    null, null);

                                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_MAKE_TUTORIAL][Sucess] Normal [UID=" + Player.UserInfo.UID + "] Concluiu Todos Tutoriais Advancer", type_msg.CL_FILE_LOG_AND_CONSOLE)); // UPDATE ON DB
                            }
                            break;
                        }
                    default:
                        throw new exception("[Error] Normal [UID=" + Player.UserInfo.UID + "] tentou fazer tutorial[Type=" + (rmt.uTipo.stTipo.tipo) + ", value=" + (rmt.uValor.ulValor) + "], Type desconhecido. Hacker ou Bug", ExceptionError.STDA_MAKE_ERROR_TYPE(STDA_ERROR_TYPE.CHANNEL,
                            551, 0x5300552));
                }

                // UPDATE ON DB
                NormalManagerDB.Instance.add(13,
                     new CmdUpdateTutorial(Player.UserInfo.UID, Player.UserInfo.Tutorial),
                    null, null);

                // Resposta do Make Tutorial
                p.init_plain(0x11F);
                p.WriteByte(rmt.uTipo.stTipo.tipo); // 0 Rookie, 1 Beginner, 2 Advancer(ACHO), 3 Init Todos
                p.WriteByte(1); // Finish Tutorial Normal ou O Tipo
                switch (rmt.uTipo.stTipo.tipo)
                {
                    case 0:
                        p.WriteUInt32(Player.UserInfo.Tutorial.rookie);
                        break;
                    case 1:
                        p.WriteUInt32(Player.UserInfo.Tutorial.beginner);
                        break;
                    case 2:
                        p.WriteUInt32(Player.UserInfo.Tutorial.advancer);
                        break;
                    default:
                        break;
                }
                Player.Send(p);

            }
            catch (exception e)
            {

                _smp.LogManager.Instance.push(new AppMessage("[Handle_PLAYER_MAKE_TUTORIAL][ErrorSystem] " + e.getFullMessageError(), type_msg.CL_FILE_LOG_AND_CONSOLE));

                // Tenho que achar outro pacote que só envie erro para o cliente, esse pacote é de inicializar os info do Player
                p.init_plain(0x44); 
                p.WriteByte(0xE2); // Error
                p.WriteUInt32((ExceptionError.STDA_SOURCE_ERROR_DECODE_TYPE(e.getCodeError()) == STDA_ERROR_TYPE.CHANNEL) ? ExceptionError.STDA_SYSTEM_ERROR_DECODE(e.getCodeError()) : 0x5300550);

                Player.Send(p);
            }
        }
    }
}