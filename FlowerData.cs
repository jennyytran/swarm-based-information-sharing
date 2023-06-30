using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing; 

namespace Swarm
{
    public class FlowerData
    {
        public Point Location;
        public int Timestamp;
        public int NectarCount;
        public int DistanceFromHive;
        
        //flower data is created when bee records data of a patch
        public FlowerData(int tick, int distance, FlowerPatch patch) 
        {
            Timestamp = tick;
            DistanceFromHive = distance;
            Location = patch.Location;
            NectarCount = patch.NectarCount;
        } 
        public FlowerData(){}

        //data is updated when be revisits patch
        public void UpdateData(FlowerPatch patch, int tick){
            Timestamp = tick;
            NectarCount = patch.NectarCount;
        }

        //revisited patch has disappeared from field
        public void SetAsEmpty(int tick){
            Timestamp = tick;
            NectarCount = 0;
        }
    }
}