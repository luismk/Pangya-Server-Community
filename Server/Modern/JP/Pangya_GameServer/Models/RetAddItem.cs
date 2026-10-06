using System;
using System.Collections.Generic;
using System.Text;

namespace Pangya_GameServer.Models
{
    public class RetAddItem
    {
        public const int INIT_VALUE = -5;//inicia os valores
        public const int ERROR = -4;//erro
        public const int SUCCESS_WITH_ERROR = -3;//adiicionou porem acontenceu um erro
        public const int SUCCES_PANG_AND_EXP_AND_CP_WITH_ERROR = -2;//adiicionou porem acontenceu um erro
        public const int SUCCESS_PANG_AND_EXP_AND_CP_POUCH_WITH_ERROR = -1;//adiicionou porem acontenceu um erro
        public const int SUCCESS_PANG_AND_EXP_AND_CP_POUCH = 0;//adiicionou item Pang/cp/Experience pouch
        public const int SUCCESS = 1;//adiicionou com sucesso!

        public RetAddItem() => Clear();

        public void Clear()
        {
            fails = new();
            type = -5;
        }

        public List<stItem> fails;
        public int type = -5;
    }
}
