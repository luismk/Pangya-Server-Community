using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PangyaAPI.Utilities
{
    public class MultiMap<TKey, TValue>
    {
        private Dictionary<TKey, List<TValue>> _dict;
        public MultiMap()
        { _dict = new Dictionary<TKey, List<TValue>>(); }

        public MultiMap(TKey key, TValue value) : this()
        {
            Add(key, value);
        }
        /// <summary>
        /// Adiciona um valor associado a uma chave.
        /// </summary>
        public void Add(TKey key, TValue value)
        {
            try
            {
                if (!_dict.ContainsKey(key))
                    _dict.Add(key, new List<TValue>() { value });
                else
                {
                    _dict[key].Add(value);
                }
            }
            catch (exception e)
            {
                throw e;
            }
        }
        public TValue GetValue(TKey key)
        {
            return _dict.TryGetValue(key, out var values) ? values.FirstOrDefault() : default;
        }
        /// <summary>
        /// Obtém todos os valores associados a uma chave. Retorna uma lista vazia se a chave não existir.
        /// </summary>
        public IReadOnlyList<TValue> GetValues(TKey key)
        {
            return _dict.TryGetValue(key, out var values) ? values.AsReadOnly() : new List<TValue>().AsReadOnly();
        }

        /// <summary>
        /// Remove um valor específico de uma chave. Retorna true se o valor foi removido, false se não existir.
        /// </summary>
        public bool Remove(TKey key, TValue value)
        {
            if (_dict.TryGetValue(key, out var values) && values.Remove(value))
            {
                if (values.Count == 0) // Remove a chave se não houver mais valores
                    _dict.Remove(key);

                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove todos os valores associados a uma chave.
        /// </summary>
        public bool RemoveAll(TKey key)
        {
            return _dict.Remove(key);
        }

        /// <summary>
        /// Verifica se a chave existe.
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            return _dict.ContainsKey(key);
        }

        /// <summary>
        /// Verifica se um valor específico existe dentro de uma chave.
        /// </summary>
        public bool ContainsValue(TKey key, TValue value)
        {
            return _dict.TryGetValue(key, out var values) && values.Contains(value);
        }
        public List<TValue> Find(TKey key)
        {
            List<TValue> toReturn;
            if (!_dict.TryGetValue(key, out toReturn))
            {
                toReturn = new List<TValue>();
            }
            return toReturn;
        }

        /// <summary>
        /// Obtém a contagem total de chaves no dicionário.
        /// </summary>
        public int KeyCount => _dict.Count;

        /// <summary>
        /// Obtém a contagem total de valores armazenados.
        /// </summary>
        public int ValueCount => _dict.Values.Sum(v => v.Count);

        /// <summary>
        /// Remove todas as chaves e valores.
        /// </summary>
        public void Clear()
        {
            _dict.Clear();
        }

        public bool Any()
        {
            return _dict.Any();
        }

        public bool empty()
        {
            return _dict.empty();
        }

        /// <summary>
        /// Retorna todas as chaves únicas armazenadas.
        /// </summary>
        public IEnumerable<TKey> Keys => _dict.Keys;

        /// <summary>
        /// Retorna todos os valores armazenados no dicionário.
        /// </summary>
        public IEnumerable<TValue> Values => _dict.Values.SelectMany(v => v);
    }
}
