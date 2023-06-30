using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing; 

namespace Swarm
{
    public class FlowerPatch
    {
        public int Lifespan;
        public Point Location;
        public int NectarCount;

        //patches are dynamic and will last a certain lifetime with limited nectar units
         public FlowerPatch(int lifespan, Point location, int nectarCount) 
         {
             Lifespan = lifespan;
             Location = location;
             NectarCount = nectarCount;
          }

        //patches are depleted when a bee collects its nectar
        public void deplete(int nectarTaken)
        {
            NectarCount = NectarCount - nectarTaken;
        }

    }
}