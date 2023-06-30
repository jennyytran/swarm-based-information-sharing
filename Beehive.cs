using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing; 

namespace Swarm
{
    public class Beehive
    {
        public Point Location;
        public int HoneyCount;

        public Beehive() {
            HoneyCount = 0;
            Location = new Point();
         }

        public void updateHoney(int nectar)
        {
            HoneyCount += nectar;
        }
    }
}