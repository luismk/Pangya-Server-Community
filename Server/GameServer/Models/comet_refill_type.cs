namespace Pangya_GameServer.Models
{
    public class ctx_comet_refill
    {

        public uint _typeid;

        public QntdRange qntd_range;
        public ctx_comet_refill()
        {
            clear();
        }
        public void clear()
        {
            _typeid = 0;
            qntd_range = new QntdRange();
            qntd_range.min = 0;
            qntd_range.max = 0;
        }

        public class QntdRange
        {
            public ushort min;
            public ushort max;

            public bool isValid()
            {
                return min > 0 && max > 0;
            }
        }
    }

}
