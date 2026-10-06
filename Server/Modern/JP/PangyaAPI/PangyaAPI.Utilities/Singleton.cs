using System;
namespace PangyaAPI.Utilities
{
    public class Singleton<_ST> where _ST : class
    {
        public static _ST myInstance = null;
         

        public static _ST Instance
        {
            get
            {
                if (myInstance == null)
                    myInstance = (_ST)Activator.CreateInstance(typeof(_ST));

                return myInstance;
            }
            set
            {
                myInstance = value;
            }
        }

        protected Singleton()
        {
        }
    }
}
