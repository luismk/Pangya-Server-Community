using System;
namespace PangyaAPI.Utilities
{
    public class Singleton<_ST> where _ST : class
    {
        public static _ST myInstance = default;

        public static _ST getInstance()
        {
            try
            {
                if (myInstance == null)
                    myInstance = (_ST)Activator.CreateInstance(typeof(_ST));
                return myInstance;
            }
            catch (Exception e)
            { 
                throw e;
            }
        }

        protected Singleton()
        {
        }
    }
}
