using System;
using System.Runtime.InteropServices;
using PangyaAPI.IFF.JP.Models.Flags;
namespace PangyaAPI.IFF.JP.Models.General
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 16)]
    public partial class IFFShopData
    {
        public uint Price { get; set; }//116 start position
        public uint DiscountPrice { get; set; }//120 start position
        public uint UsedPrice { get; set; }//124 start position(Aqui é a condição do angel wing seu valor é 6, as outras angel wings do outros characters variam entre 1, 5, 6 e 0 (acho que seja a sexta condição de quit rate menor que 3%)
        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 4)]
        public FlagShop flag_shop { get; set; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 4)]
    public class FlagShop
    {
        /// <summary>
        /// shop flag
        /// </summary>
        public ShopFlagEnum ShopFlag { get; set; }//128 start position
        public MoneyFlagEnum MoneyFlag { get; set; }//129 start position(0x01 in stock; 0x02 disable gift; 0x03 Special; 0x08 new; 0x10 hot)
        //-------------------- TIME IFF--------------\\
        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 2)]
        public TimeShop time_shop { get; set; }
        //-------------------- TIME IFF--------------\\      
        /// <summary>
        /// true = cookie
        /// </summary>
        public bool IsCash
        {
            get
            {
                if (ShopFlag == (ShopFlagEnum)6 && MoneyFlag == 0)
                {
                    return false;
                }
                if (MoneyFlag == 0 && ShopFlag == 0)
                {
                    return false;
                }
                if (MoneyFlag == 0 && ShopFlag == ShopFlagEnum.NonGiftable)
                {
                    return false;
                }
                var result = (byte)ShopFlag & 2;// é cookies (as vezes da 0, não sei o que é)
                if (MoneyFlag == 0 && ShopFlag == 0)
                {
                    return false;
                }
                if (ShopFlag == ShopFlagEnum.Pang)
                {
                    return false;
                }
                if (ShopFlag == ShopFlagEnum.Combine96)
                {
                    return false;
                }
                if (ShopFlag == ShopFlagEnum.Combine97)// cookies
                {
                    return true;
                }
                if (result == 0) //  cookie = 0, 1 free
                {
                    if (!IsShop)
                    {
                        return false;
                    }
                    return true;
                }
                if (((byte)ShopFlag & 1) == 1)
                    return true;
                return false;
            }
        }
        /// <summary>
        /// tue is Pang
        /// </summary>
        public bool IsPang
        {
            get
            {
                if (ShopFlag == ShopFlagEnum.NonGiftable && MoneyFlag == MoneyFlagEnum.Active)
                {
                    return false;
                }
                if (ShopFlag == (ShopFlagEnum)6 && MoneyFlag == 0)
                {
                    return false;
                }
                if (MoneyFlag == 0 && ShopFlag == 0)
                {
                    return false;
                }
                if (MoneyFlag == 0 && ShopFlag == ShopFlagEnum.NonGiftable)
                {
                    return true; // é pangs, pq uso isso
                }
                var result = (byte)ShopFlag & 2;// é cookies (as vezes da 0, não sei o que é)       
                if (ShopFlag == 0 && MoneyFlag == 0)
                {
                    return false;
                }
                if (ShopFlag == ShopFlagEnum.Pang)
                {
                    return true;
                }
                if (ShopFlag == ShopFlagEnum.Combine96)
                {
                    return true;
                }
                if (result == 2)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// IsReserve  (IFF_personal_shop)
        /// </summary>
        public bool can_send_mail_and_personal_shop
        {
            get { return GetBit((byte)ShopFlag, 1); }
            set { ShopFlag = (ShopFlagEnum)SetBit((byte)ShopFlag, 1, value); }
        }

        public bool IsDuplication
        {
            get { return GetBit((byte)ShopFlag, 2); }
            set { ShopFlag = (ShopFlagEnum)SetBit((byte)ShopFlag, 2, value); }
        }

        public bool unknown
        {
            //(ShopFlag == ShopFlag.Unknown32 && MoneyFlag == (MoneyFlag)3) || (ShopFlag == ShopFlag.Cookies_0 && MoneyFlag == (MoneyFlag)3);
            get { return GetBit((byte)ShopFlag, 3); }
        }

        /// <summary>
        /// IsNew
        /// </summary>
        public bool block_mail_and_personal_shop
        {
            get { return GetBit((byte)ShopFlag, 4); }
            set { ShopFlag = (ShopFlagEnum)SetBit((byte)ShopFlag, 4, value); }
        }
        /// <summary>
        /// is hot ou sale
        /// </summary>
        public bool is_saleable
        {
            get
            {
                var result = ((byte)ShopFlag & 5);// é cookies
                if ((result == 1 || result == 0))
                {
                    if (ShopFlag == 0 || MoneyFlag == MoneyFlagEnum.None)
                    {
                        if (ShopFlag == ShopFlagEnum.Cookies_0)
                            return true;
                        if (ShopFlag == (ShopFlagEnum)34)
                            return true;
                        return false;
                    }
                    return false;
                }
                if (result == 4)
                {
                    if (ShopFlag == (ShopFlagEnum)38)
                    {
                        return true;
                    }
                    return true;
                }
                return false;
            }
            set { ShopFlag = (ShopFlagEnum)SetBit((byte)ShopFlag, 5, value); }
        }

        public bool IsGift
        {
            get
            {
                var result = ((byte)ShopFlag & 6) == 4;// é cookies
                if (result)
                {
                    return true;
                }
                if ((ShopFlag == (ShopFlagEnum)32 || ShopFlag == ShopFlagEnum.Cookies_0) && MoneyFlag == 0)// é gift, não sei se essa regra acabe no pang
                {
                    return true;
                }

                if ((/*ShopFlag == (ShopFlag)32 ||*/ ShopFlag == ShopFlagEnum.Cookies_0) && MoneyFlag == MoneyFlagEnum.Active)// é gift, não sei se essa regra acabe no pang
                {
                    return true;
                }
                if ((/*ShopFlag == (ShopFlag)32 ||*/ ShopFlag == ShopFlagEnum.Cookies_0/* || ShopFlag == ShopFlag.Combine*/) && MoneyFlag == MoneyFlagEnum.BannerNew)// é gift, não sei se essa regra acabe no pang
                {
                    return true;
                }
                if (ShopFlag == (ShopFlagEnum)32 && MoneyFlag == MoneyFlagEnum.BannerNew)// é gift, não sei se essa regra acabe no pang
                {
                    return false;
                }
                return false; //return GetBit((byte)ShopFlag, 6); 
            }
            set { ShopFlag = (ShopFlagEnum)SetBit((byte)ShopFlag, 6, value); }
        }

        public bool IsDisplay
        {
            get => ShopFlag == ShopFlagEnum.Only_Display && MoneyFlag == MoneyFlagEnum.None;
        }


        public bool IsNormal => ((ShopFlag == (ShopFlagEnum)38 && MoneyFlag == MoneyFlagEnum.None) || (ShopFlag == (ShopFlagEnum)32 && MoneyFlag == MoneyFlagEnum.Active) || (ShopFlag == (ShopFlagEnum)32 && MoneyFlag == MoneyFlagEnum.BannerNew) || (ShopFlag == ShopFlagEnum.Cookies_0 && MoneyFlag == MoneyFlagEnum.Active) || (ShopFlag == ShopFlagEnum.Cookies_0 && MoneyFlag == MoneyFlagEnum.BannerNew) || (ShopFlag == ShopFlagEnum.Combine97 && MoneyFlag == MoneyFlagEnum.None) || (ShopFlag == ShopFlagEnum.Combine96 && MoneyFlag == MoneyFlagEnum.None) || ShopFlag == (ShopFlagEnum)98 && MoneyFlag == MoneyFlagEnum.None) || ((byte)ShopFlag == 33 && MoneyFlag == MoneyFlagEnum.None) || ((byte)ShopFlag == 34 && MoneyFlag == MoneyFlagEnum.None) || (ShopFlag == ShopFlagEnum.Pang && MoneyFlag == MoneyFlagEnum.None) || (ShopFlag == (ShopFlagEnum)21 && MoneyFlag == MoneyFlagEnum.None);
        public bool IsInStock // ativado é hide
        {
            get
            {
                var result = (Convert.ToByte(MoneyFlag) & 0) == 0;// é cookies
                if (result)
                {
                    return true;
                }
                return false;
            }
        }
        public bool IsNew
        {
            get
            {
                var result = (Convert.ToByte(MoneyFlag) & 1) == 1;// é cookies
                if (result)
                {
                    return true;
                }
                if (MoneyFlag == 0)
                {
                    if (ShopFlag == (ShopFlagEnum)34)
                    {
                        return false;
                    }

                    if (ShopFlag == (ShopFlagEnum)33)
                    {
                        return false;
                    }
                    if (ShopFlag == ShopFlagEnum.NonGiftable && MoneyFlag == MoneyFlagEnum.None)
                    {
                        return false;
                    }
                    return ShopFlag == ShopFlagEnum.NonGiftable;
                }
                if (MoneyFlag == MoneyFlagEnum.Active)
                {
                    if (ShopFlag == (ShopFlagEnum)33)
                    {
                        return true;
                    }
                    if (ShopFlag == (ShopFlagEnum)34)
                    {
                        return true;
                    }
                    return ShopFlag == ShopFlagEnum.NonGiftable;
                }
                return false;
            }
        }

        public bool IsHot
        {
            get
            {
                var result = (Convert.ToByte(MoneyFlag) & 2) == 2;// é cookies
                if (result)
                {
                    return true;
                }
                return false;
            }
        }

        public bool IsSpecial // codigo 8 Bit 
        {
            get
            {
                var result = (Convert.ToByte(MoneyFlag) & 3) == 3;// é cookies

                if (result)
                {
                    return true;
                }
                return (ShopFlag == (ShopFlagEnum)32 && MoneyFlag == (MoneyFlagEnum)3) || (ShopFlag == (ShopFlagEnum)33 && MoneyFlag == (MoneyFlagEnum)3);
            }
        }

        public bool IsPack
        {
            get
            {
                var result = (Convert.ToByte(MoneyFlag) & 4) == 4;// é cookies
                if (result)
                {
                    return true;
                }
                return false;
            }
        }
        public bool IsTradeable
        {
            get
            {
                return ShopFlag == (ShopFlagEnum)6 && IsCash == false;
            }

        }
        public bool IsPSQ { get => (ShopFlag == ShopFlagEnum.NonGiftable && MoneyFlag == MoneyFlagEnum.None) || (ShopFlag == (ShopFlagEnum)98) || (ShopFlag == (ShopFlagEnum)7); }

        public bool IsShop
        {
            get
            {
                if (ShopFlag == (ShopFlagEnum)6 && MoneyFlag == 0)
                {
                    return false;
                }
                if (MoneyFlag == 0 && ShopFlag == 0)
                {
                    return false;
                }
                if (MoneyFlag == 0 && ShopFlag == ShopFlagEnum.NonGiftable)
                {
                    return false;
                }
                if (MoneyFlag == (MoneyFlagEnum)21 && ShopFlag == ShopFlagEnum.NonGiftable)
                {
                    return false;
                }
                if (ShopFlag == ShopFlagEnum.Giftable && MoneyFlag == MoneyFlagEnum.BannerNew)
                {
                    return false;
                }
                if (ShopFlag == 0 && MoneyFlag == MoneyFlagEnum.BannerNew)
                {
                    return false;
                }
                if (ShopFlag == 0 && MoneyFlag == MoneyFlagEnum.Active)
                {
                    return false;
                }
                return (IsNew || IsHot || IsSpecial || IsNormal || is_saleable || IsDisplay);
            }
        }
        // Método auxiliar para definir o valor de um bit específico
        private byte SetBit(byte b, int bitNumber, bool value)
        {
            if (value)
            {
                return (byte)(b | (1 << bitNumber));
            }
            else
            {
                return (byte)(b & ~(1 << bitNumber));
            }
        }
        private bool GetBit(byte b, int bitNumber)
        {
            return (b & (1 << bitNumber)) != 0;
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 2)]
    public class TimeShop
    {
        [field: MarshalAs(UnmanagedType.U1, SizeConst = 1)]
        public bool active { get; set; }//130 start position(Item por tempo)
        public ShopFlagDay dia { get; set; }//131 start position(Tempo por dias=1, 7, 15, 30 e 365 && 0xFF fica 0x6D, por que é 0x16D = 365)

        public void SetDay(decimal dec)
        {
            var value = (int)dec;
            if (value >= 365)
                value = 365;

            dia = (ShopFlagDay)(value & 0xFF);
        }

        public int getDay()
        {
            return (int)ShopFlagDay.Year >= 365 ? (int)365 : (int)dia;
        }
    }
}
