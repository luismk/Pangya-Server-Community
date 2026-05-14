namespace Pangya_GameServer.Models
{
    public class CalculeCoinCubeUpdateOrder
    {
        public enum eTYPE : byte
        {
            COIN,
            CUBE
        }
        public eTYPE type = new eTYPE();
        public uint uid = new uint(); // Player request
        public Location last_location = new Location();
        public Location pin = new Location();
        public ShotEndLocationData shot_data_for_cube = new ShotEndLocationData();
        public byte course;
        public byte hole;
        public CalculeCoinCubeUpdateOrder()
        {

        }
        public CalculeCoinCubeUpdateOrder(eTYPE tipo, uint uid, ShotSyncData.Location loc, Location pins, ShotEndLocationData _shot_data_for_cube, byte map, byte num)
        {
            this.uid = uid;
            type = tipo;
            pin = pins;
            last_location = loc;
            this.shot_data_for_cube = _shot_data_for_cube;
            hole = num;
            course = map;
        }
        public CalculeCoinCubeUpdateOrder(eTYPE tipo, uint uid, Location loc, Location pins, ShotEndLocationData _shot_data_for_cube, byte map, byte num)
        {
            this.uid = uid;
            type = tipo;
            pin = pins;
            last_location = loc;
            this.shot_data_for_cube = _shot_data_for_cube;
            hole = num;
            course = map;
        }
    }

    public class CoinCubeUpdate
    {
        public enum eTYPE : byte
        {
            INSERT,
            UPDATE
        }
        public eTYPE type = new eTYPE();
        public byte course_id;
        public byte hole_number;
        public CubeEx cube = new CubeEx();

        public CoinCubeUpdate(eTYPE type_, byte course_id_, byte hole_number_, CubeEx el_cube)
        {
            this.type = type_;
            this.course_id = course_id_;
            this.hole_number = hole_number_;
            this.cube = el_cube;
        }
        public CoinCubeUpdate()
        { }
    }
}
