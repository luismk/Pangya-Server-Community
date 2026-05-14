using System;
using System.Runtime.InteropServices;
using System.Text;
using PangyaAPI.IFF.JP.Models.Flags;
using PangyaAPI.Utilities.Models;

namespace PangyaAPI.IFF.JP.Models.General
{
    /// <summary>
    /// Ref's:
    /// my code first: https://github.com/oung/Py_Source_JP/tree/master/Src/PangyaFileCore
    ///<code></code>
    /// replace: https://github.com/Acrisio-Filho/SuperSS-Dev/blob/master/Server%20Lib/Projeto%20IOCP/TYPE/data_iff.h
    /// update in 30/01/2025 - 10:40 AM by LuisMK
    ///<code></code>
    /// Common data structure found at the head of many IFF datasets
    ///<code></code>
    /// Size 192 bytes
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public partial class IFFCommon : ICloneable
    {
        //------------------- IFF BASIC ----------------------------\\
        /// <summary>
        /// Active item
        /// </summary>
        [field: MarshalAs(UnmanagedType.Bool, SizeConst = 4)]
        public bool Active { get; set; } = true;//0 start position
        /// <summary>
        /// Tipo Index do item
        /// </summary>
        public uint ID { get; set; }//4 start position
        /// <summary>
        /// nome do item em bytes
        /// </summary>
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        byte[] NameInBytes { get; set; } = new byte[0];//8 start position
        /// <summary>
        /// level do item
        /// </summary>
        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 1)]
        public IFFLevel Level { get; set; } = new IFFLevel(); //72 start position
        /// <summary>
        /// Nome do icone 
        /// </summary>
        [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 43)]  //is 40, 3 bytes isnt used 
        public string ShopIcon { get; set; } = "";//73 start position 
        //--------------------------end--------------------------------\\

        //------------------ SHOP DADOS ---------------------------------\\
        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 16)]
        public IFFShopData Shop { get; set; } = new IFFShopData();  //116 start position
        //-------------------  END  ------------------------------\\
        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 24)]
        public IFFTikiShopData tiki { get; set; } = new IFFTikiShopData(); //132 start position
        //-------------------- TIME IFF--------------\\
        [field: MarshalAs(UnmanagedType.Struct, SizeConst = 36)]
        public IFFDate date { get; set; } = new IFFDate(); //176 start position
        public string Name
        {
            get => Encoding.GetEncoding("Shift_JIS").GetString(NameInBytes ?? (new byte[0])).Replace("\0", "");
            set => NameInBytes = Encoding.GetEncoding("Shift_JIS").GetBytes(value.PadRight(64, '\0'));
        }
        /// <summary>
        /// voce pode carregar qualquer iff(que contem o Base)
        /// </summary>
        /// <param name="reader">binario de leitura</param>
        /// <param name="LenghtStr">tamanho do string name</param>
        public void Load(ref PangyaBinaryReader reader, uint LenghtStr, long recordLength = 0, uint version = 11, bool jump = false)
        {
            //------------------- IFF BASIC ----------------------------\\
            Active = reader.ReadBoolean();
            ID = reader.ReadUInt32();
            Name = reader.ReadPStr(LenghtStr);
            Level = new IFFLevel
            {
                level = reader.ReadSByte() //49 start position
            };
            ShopIcon = reader.ReadPStr(43); //89 start position
            //--------------------------end--------------------------------\\
            //------------------ SHOP DADOS ---------------------------------\\
            Shop = (IFFShopData)reader.Read(new IFFShopData(), 16);
            //-------------------  END  ------------------------------\\
            //------------------ Tiki SHOP---------------------\\
            if (version != 11)
            {
                tiki = (IFFTikiShopData)reader.Read(new IFFTikiShopData(), 24);
            }
            //-----------------------------------------------\\

            //-------------------- TIME IFF--------------\\
            date = (IFFDate)reader.Read(new IFFDate(), 36);
            //--------------------------------------------------\\
            if (jump)
            {
                reader.Skip(36);
            }
        }
        /// <summary>
        /// voce pode carregar qualquer iff(que contem o Base)
        /// </summary>
        /// <param name="reader">binario de leitura</param>
        /// <param name="LenghtStr">tamanho do string name</param>
        public void Load(ref PangyaBinaryReader reader, uint LenghtStr)
        {
            //------------------- IFF BASIC ----------------------------\\
            Active = reader.ReadBoolean();
            ID = reader.ReadUInt32();
            Name = reader.ReadPStr(LenghtStr);
            Level = new IFFLevel
            {
                level = reader.ReadSByte() //49 start position
            };
            ShopIcon = reader.ReadPStr(43); //89 start position
            //--------------------------end--------------------------------\\
            //------------------ SHOP DADOS ---------------------------------\\
            Shop = (IFFShopData)reader.Read(new IFFShopData(), 16);
            //-------------------  END  ------------------------------\\
            //------------------ Tiki SHOP---------------------\\
            tiki = (IFFTikiShopData)reader.Read(new IFFTikiShopData(), 24);
            //-----------------------------------------------\\

            //-------------------- TIME IFF--------------\\
            date = (IFFDate)reader.Read(new IFFDate(), 36);
            //--------------------------------------------------\\
        }


        public string GetItemName()
        {
            return Name;
        }
        public uint Price
        {
            get => Shop == null ? 0 : Shop.Price;
            set => Shop.Price = value;
        }
        public sbyte ItemLevel
        {
            get => (sbyte)(Level == null ? 0 : Level.level);
            set => Level.level = value;
        }
        public uint DiscountPrice
        {
            get => Shop == null ? 0 : Shop.DiscountPrice;
            set => Shop.DiscountPrice = value;
        }


        public bool IsExist()
        {
            return Convert.ToBoolean(Active);
        }

        public object Clone()
        {
            return MemberwiseClone();
        }


        public bool IsDupItem()
        {
            return Active && Shop.flag_shop.IsDuplication;
        }

        public bool IsSale()
        {
            return Active && Shop.flag_shop.IsShop;
        }

        public bool IsHot()
        {
            return Shop.flag_shop.IsHot && Active;
        }

        public bool IsNormal()
        {
            return Active && (Shop.flag_shop.IsNormal);
        }

        public bool IsNew()
        {
            if (IsHide)
            {
                return false;
            }
            return Active && Shop.flag_shop.IsNew;
        }

        public bool IsGiftItem()
        {
            // � saleable ou giftable nunca os 2 juntos por que � a flag composta Somente Purchase(compra)
            // ent�o fa�o o xor nas 2 flag se der o valor de 1 � por que ela � um item que pode presentear
            // Ex: 1 + 1 = 2 N�o �
            // Ex: 1 + 0 = 1 OK
            // Ex: 0 + 1 = 1 OK
            // Ex: 0 + 0 = 0 N�o �
            byte is_giftable = Convert.ToByte(Shop.flag_shop.IsGift);
            byte _is_saleable = Convert.ToByte(Shop.flag_shop.is_saleable);
            if (Active && Shop.flag_shop.IsCash
                    && (_is_saleable ^ is_giftable) == 1)
            {
                return true;
            }
            else if (Shop.flag_shop.IsGift)
            {
                return true;
            }
            return false;
        }

        public bool IsOnlyDisplay()
        {
            return (Active && Shop.flag_shop.IsDisplay);
        }

        public bool IsOnlyPurchase()
        {
            return (Active && Shop.flag_shop.is_saleable
                    && Shop.flag_shop.IsGift);
        }

        public bool IsOnlyGift()
        {
            return (Active && Shop.flag_shop.IsCash
                    && Shop.flag_shop.is_saleable && Shop.flag_shop.IsGift == false);
        }

        public bool IsPSQ()
        {
            if (Active)
            {
                if (Shop.flag_shop.IsPSQ)
                {
                    return true;
                }
                if (Shop.flag_shop.IsTradeable)
                {
                    return true;
                }
                if (Shop.flag_shop.can_send_mail_and_personal_shop)
                {
                    return true;
                }
                return false;
            }
            return false;
        }
        public bool IsHide  // 98% 
        {
            get
            {
                if (date.Start.TimeActive)
                {

                    if (date.Start.Year >= DateTime.Now.Year && date.Start.Day >= DateTime.Now.Day && date.Start.Month >= DateTime.Now.Month)
                        return false;
                    if (DateTime.Now.Year > date.Start.Year && DateTime.Now.Day > date.Start.Day && DateTime.Now.Month > date.Start.Month && !Shop.flag_shop.IsShop)
                    {

                        return true;
                    }
                    if (date.Start.Year < DateTime.Now.Year && Shop.flag_shop.IsShop)   //tempo antigo 2007-2008
                    {
                        return true;
                    }
                }
                if (DateTime.Now.Year > date.Start.Year && DateTime.Now.Day > date.Start.Day && DateTime.Now.Month > date.Start.Month && !Shop.flag_shop.IsShop)
                    if (date.Start.Year == 0)
                    {
                        date.Clear();
                        if (Shop.flag_shop.MoneyFlag == 0 && Shop.flag_shop.ShopFlag == 0)
                        {
                            return true;
                        }
                        if (Shop.flag_shop.MoneyFlag == 0 && Shop.flag_shop.ShopFlag == ShopFlagEnum.NonGiftable)
                        {
                            return false;
                        }

                        if (Shop.flag_shop.ShopFlag == (ShopFlagEnum)6 && Shop.flag_shop.MoneyFlag == 0)
                        {
                            return true;
                        }
                        if (Shop.flag_shop.MoneyFlag == 0 && Shop.flag_shop.ShopFlag == 0)
                        {
                            return true;
                        }
                        if (Shop.flag_shop.MoneyFlag == 0 && Shop.flag_shop.ShopFlag == ShopFlagEnum.NonGiftable)
                        {
                            return false;    // é psq
                        }
                        if (Shop.flag_shop.MoneyFlag == (MoneyFlagEnum)21 && Shop.flag_shop.ShopFlag == ShopFlagEnum.NonGiftable)
                        {
                            return true;
                        }
                        if (Shop.flag_shop.ShopFlag == ShopFlagEnum.Giftable && Shop.flag_shop.MoneyFlag == MoneyFlagEnum.BannerNew)
                        {
                            return true;
                        }
                        if (Shop.flag_shop.ShopFlag == 0 && Shop.flag_shop.MoneyFlag == MoneyFlagEnum.BannerNew)
                        {
                            return true;
                        }
                        if (Shop.flag_shop.ShopFlag == 0 && Shop.flag_shop.MoneyFlag == MoneyFlagEnum.Active)
                        {
                            return true;
                        }

                        if (Shop.flag_shop.ShopFlag == ShopFlagEnum.Combine96 && Shop.flag_shop.MoneyFlag == MoneyFlagEnum.BannerNew)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                        return true;

                if (Shop.flag_shop.ShopFlag == ShopFlagEnum.Combine96 && Shop.flag_shop.MoneyFlag == MoneyFlagEnum.BannerNew)
                {
                    return false;
                }
                if (date.Start.Year > 0 && date.Start.Day > 0 && DateTime.Now.Year > date.Start.Year && DateTime.Now.Day > date.Start.Day && Shop.flag_shop.IsShop)
                {
                    return true;
                }
                if (!Shop.flag_shop.IsNormal && (!Shop.flag_shop.is_saleable || !Shop.flag_shop.can_send_mail_and_personal_shop || !Shop.flag_shop.IsDuplication) && !Shop.flag_shop.IsNew && !IsGiftItem() && !Shop.flag_shop.IsHot && !Shop.flag_shop.IsDisplay)
                {
                    return true;
                }
                if (Shop.flag_shop.ShopFlag == (ShopFlagEnum)6 && Shop.flag_shop.MoneyFlag == 0)
                {
                    return true;
                }

                if (Shop.flag_shop.ShopFlag == ShopFlagEnum.Giftable && Shop.flag_shop.MoneyFlag == MoneyFlagEnum.Active)
                {
                    return true;
                }
                if (Shop.flag_shop.ShopFlag == ShopFlagEnum.Giftable && Shop.flag_shop.MoneyFlag == MoneyFlagEnum.Active)
                {
                    return true;
                }

                if (Shop.flag_shop.ShopFlag == ShopFlagEnum.Combine96 && Shop.flag_shop.MoneyFlag == MoneyFlagEnum.Active)
                {
                    return false;
                }

                return false;
            }
        }

        /// <summary>
        /// verifica � pang, cookie ou esta oculto dentro do shopping
        /// </summary>
        /// <returns>1= cookies, 2= pang, 0= hide </returns>
        public int GetTypeCash()
        {
            if (IsHide)
                return 0;

            else if (Shop.flag_shop.IsCash)
                return 1;
            else if (Shop.flag_shop.IsPang)
                return 2;
            else if (Shop.flag_shop.IsDisplay)
                return 3;
            else if (IsPSQ())  // � hide no shop normal, porem no psq � ativo, tenho que ver um codigo melhor depois
            {
                if (Shop.flag_shop.IsCash)
                    return 1;
                else if (Shop.flag_shop.IsPang)
                    return 2;
                return 0;
            }
            return 0;
        }

    }
}
