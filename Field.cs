using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing; 


namespace Swarm
{
    public class Field
    {
        public int[] Size = new int[]{Constant.SizeX,Constant.SizeY}; //size of field grid
        public List<FlowerPatch> PatchList = new List<FlowerPatch>(); // list of FlowerPatch objects in field
        public List<Point> Available = new List<Point>(); // list of available locations with no patches
        public Beehive Hive  = new Beehive(); //beehive in field
        public List<Bee> BeesList = new List<Bee>(); //list of bee population

         public void InitialiseField(){            
            Console.WriteLine("Initalising...");
            var random = new Random();

            var posX = random.Next(0,Constant.SizeX+1);
            var posY = random.Next(0,Constant.SizeY+1);
            Hive.Location = new Point(posX,posY);

            //add flower patches to field
            var availableLocation = (Constant.SizeX+1)*(Constant.SizeY+1);
            var flowerCount = random.Next(1, availableLocation);
            
            for (var x = 0; x <= Constant.SizeX; x++){
                for (var y = 0; y <= Constant.SizeY; y++){
                    Point location = new Point(x,y);
                    if (location != Hive.Location){
                        Available.Add(location);
                    }
                }
            }

            //Console.WriteLine("total:" + flowerCount);
            for (var i = 0; i < flowerCount; i++){
                var randIndex = random.Next(0, Available.Count); 
                var location = Available[randIndex];
                var nectarCount = random.Next(1, Constant.MaxNectarCount);       
                var lifespan = random.Next(0, Constant.MaxLifespan);  

                FlowerPatch patch = new FlowerPatch(lifespan, location, nectarCount);
                Available.Remove(location);  
                PatchList.Add(patch);
            }
            
            //add bees to field
            for (int j = 0; j < Constant.BeePopulation; j++){
                // create bee objects
                Bee bee = new Bee();
                bee.Location = Hive.Location;
                BeesList.Add(bee);
            }
        }

        //updates flowerpatch list in field each time step
        public void UpdateField()
        { 
            //remove wilted and depleted patches from field
            foreach (var patch in PatchList.ToList())
            {
                patch.Lifespan--;

                if (patch.Lifespan == 0 || patch.NectarCount == 0)
                {
                    Available.Add(patch.Location);
                    PatchList.Remove(patch);
                }
            }

            //create new patches randomly on the field
            var random = new Random();
            int newPatches = random.Next(0, Available.Count/2);

            for (int i = 0; i < newPatches; i++){
                int randIndex = random.Next(Available.Count); 
                var location = Available[randIndex];
                var nectarCount = random.Next(1, Constant.MaxNectarCount);       
                var lifespan = random.Next(1, Constant.MaxLifespan);   
                
                var patch = new FlowerPatch(lifespan,location, nectarCount);
                Available.Remove(location);   
                PatchList.Add(patch);
            }
            
         } 
    
        //manual deep copy of field
        public Field DeepCopy(){
            var fieldCopy = new Field();
            
            fieldCopy.Hive.Location = new Point(Hive.Location.X, Hive.Location.Y);
            fieldCopy.Hive.HoneyCount = Hive.HoneyCount;

            foreach (var patch in PatchList.ToList()){
                fieldCopy.PatchList.Add(new FlowerPatch(patch.Lifespan, new Point(patch.Location.X, patch.Location.Y), patch.NectarCount));
            }
            foreach (var location in Available.ToList()){
                fieldCopy.Available.Add(new Point(location.X, location.Y));
            }

            foreach (var bee in BeesList.ToList()){
                var beeCopy = new Bee();
                beeCopy.Duty = bee.Duty;
                beeCopy.Location = new Point(bee.Location.X, bee.Location.Y);
                beeCopy.NectarCollected = bee.NectarCollected;
                beeCopy.Target = new Point(bee.Target.X, bee.Target.Y);
                beeCopy.RemainingDistance = bee.RemainingDistance;
                foreach (var data in bee.DataCollection.ToList()){
                    var dataCopy = new FlowerData();
                    dataCopy.Location = new Point(data.Location.X, data.Location.Y);
                    dataCopy.Timestamp = data.Timestamp; 
                    dataCopy.NectarCount = data.NectarCount;
                    dataCopy.DistanceFromHive = data.DistanceFromHive;

                    beeCopy.DataCollection.Add(dataCopy);
                }

                fieldCopy.BeesList.Add(beeCopy);
            }
            return fieldCopy;
        }

    } 
}