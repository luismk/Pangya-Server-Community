using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pangya_LoginServer.PangyaEnums
{
    public enum NICK_CHECK : byte
    {
        SUCCESS,                // Sucesso por trocar o nick por que ele está disponível
        UNKNOWN_ERROR,          // Erro desconhecido, Error ao verificar NICK
        NICK_IN_USE,            // NICKNAME já em uso
        INCORRECT_NICK,         // INCORRET nick, tamanho < 4 ou tem caracteres que não pode
        NOT_ENOUGH_COOKIE,      // Não tem points suficiente
        HAVE_BAD_WORD,          // Tem palavras que não pode no NICK
        ERROR_DB,               // Erro DB
        EMPETY_ERROR,           // Erro Vazio
        EMPETY_ERROR_2,         // ERRO VAZIO 2
        SAME_NICK_USED,         // O Mesmo nick vai ser usado, estou usando para o mesmo que o ID
        EMPETY_ERROR_3,         // ERRO VAZIO 3
        CODE_ERROR_INFO = 12    // CODE  ERROR INFO arquivo iff, o código do erro para mostra no cliente
    }
}
