using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing; 

namespace Swarm
{
    public class Bee
    {
        public enum State { 
            idle, //bee remains stationary at hive until further actions
            forage, //bee searches field for data
            collect, //bee collects nectar from patches in field
            home, //bee returns to hive from field
            exchange, //bee exchange data about the hive to idle bees at the hive
            honey //bee produced honey from collected nectat
        }
        public State Duty = State.idle;
        public Point Location = new Point(0,0);
        public int MaxCapacity = Constant.MaxCapacity;
        public int NectarCollected = 0;
        public List<FlowerData> DataCollection = new List<FlowerData>();
        public Point Target = new Point(-1,-1); //bee's current target location
        public int RemainingDistance = -1; // distance needed to be travelled to reach target

        public Bee() {}

        //calculates distance between two points on the field grid
        public int GetDistance(Point p1, Point p2){
            var distance = Math.Sqrt((Math.Pow( p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2)));
            return Convert.ToInt32(Math.Ceiling(distance));
        }
        
         //Generates random location in field 
        public Point SetRandomTarget(Beehive hive){
            var random = new Random();
            var searchLocation = hive.Location;

            //assign new location that is not hive or bee's current location
            while (searchLocation.Equals(hive.Location) || (searchLocation.Equals(Location))){
                var x2 = random.Next(0, Constant.SizeX + 1);
                var y2 = random.Next(0, Constant.SizeY + 1);
                searchLocation = new Point(x2,y2);
            }

            return searchLocation;
        }

        //Bee searches random patch on field until it finds data on a patch
        public void Forage(Field area, int tick) 
        { 
            //allocate new target to forage
            if (RemainingDistance <= -1){             
                Target = SetRandomTarget(area.Hive);
                RemainingDistance = GetDistance(area.Hive.Location, Target);  
                RemainingDistance--;
            }
            
            //Bee travels 1 unit closer to target location
            else if (RemainingDistance > 0){
                RemainingDistance--;
            }
            
            //Destination reached
            else if (RemainingDistance == 0) {
                Location = Target; //update bees location 

                var patch = 
                area.PatchList
                .FirstOrDefault(patch => patch.Location.Equals(Target));

                var data = 
                DataCollection
                .FirstOrDefault(data => data.Location.Equals(Target));


                //Update revisited patch
                if (!(patch is null) && !(data is null)){
                    DataCollection.First(data => data.Location.Equals(Target)).UpdateData(patch, tick);
                    RemainingDistance = -1; // reached destination and reset distance
                    Target = area.Hive.Location;
                }
                
                //Set revisited patch as empty
                else if (patch is null && !(data is null)){
                    DataCollection.First(data => data.Location.Equals(Target)).SetAsEmpty(tick); 
                }
                
                //Record data of newly discovered patch
                else if (!(patch is null) && (data is null))
                {
                    var distancefromHive = GetDistance(Location, area.Hive.Location);
                    DataCollection.Add(new FlowerData(tick, distancefromHive, patch));
                    
                    RemainingDistance = -1; // reached destination and reset distance
                    Target = area.Hive.Location;
                }
                //No data found - set new target and keep foraging
                else if (patch is null && data is null){
                    Target = SetRandomTarget(area.Hive); 
                    RemainingDistance = GetDistance(Location, Target);
                }
            }
        }
        public void CollectNectar(Field area, int tick) 
        {   
            //set bestpatch as target location - greatest nectar at shortest distance
            if (RemainingDistance <= -1){
                DataCollection = DataCollection.OrderByDescending(data => data.NectarCount)
                                .ThenBy(data => data.DistanceFromHive).ToList();

                var best = DataCollection.First();
                
                Target = best.Location;
                RemainingDistance = GetDistance(Location, Target);
                RemainingDistance--;
            }

            //travelling to target location
            else if (RemainingDistance > 0){
                RemainingDistance--;
            }
            
            //destination reached - collect nectar from patch
            else if (RemainingDistance == 0){
            
                var bestPatch = area.PatchList
                                .FirstOrDefault(patch => patch.Location.Equals(Target));
                
                //deplete patch of its nectar
                if (!(bestPatch is null)){
                    while (bestPatch.NectarCount > 0 && NectarCollected < MaxCapacity){
                        bestPatch.deplete(1);
                        NectarCollected++;
                    }
                    DataCollection.First(patch => patch.Location.Equals(Target)).UpdateData(bestPatch, tick);
                }
                //set patch as empty or nectarcount is 0
                else{
                    DataCollection.First(data => data.Location.Equals(Target)).SetAsEmpty(tick);
                }

                DataCollection.OrderBy(data => data.DistanceFromHive)
                            .ThenByDescending(data => data.NectarCount); 
                    
                Location = Target;

                //continue to collect from other patches until max capacity is reached
                if (NectarCollected < MaxCapacity && !(DataCollection.FirstOrDefault(data => data.NectarCount>0) is null)){
                    Target = DataCollection.First(data => data.NectarCount>0).Location;
                    RemainingDistance = GetDistance(Location, Target);
                }

                //return home if max capacity reached or no nectar available from datacollection
                else {
                    Target = area.Hive.Location;
                    RemainingDistance = -1;
                }
            }

         } 
        public void ReturnToHive(Beehive hive) 
        { 
            if (RemainingDistance <= -1){
                Target = hive.Location;
                RemainingDistance = GetDistance(Location, hive.Location);
                RemainingDistance--;
            }
            else if (RemainingDistance > 0){
                RemainingDistance--;
            }
            else if (RemainingDistance == 0){
                RemainingDistance = -1;
                Location = hive.Location;
                Target = new Point(-1,-1);
            }
        } 

        //creates deep copy of bee's dataCollection
        public List<FlowerData> CloneDataCollection(List<FlowerData> dataCollection){
            var copy = new List<FlowerData>();
            foreach (var item in dataCollection.ToList()){
                copy.Add(new FlowerData{ 
                    Location = new Point(item.Location.X, item.Location.Y), 
                    Timestamp = item.Timestamp, 
                    NectarCount = item.NectarCount, 
                    DistanceFromHive = item.DistanceFromHive
                    });
            }
            return copy;
        }
        
        //merges data collections of two bees
        public List<FlowerData> ExhangeDataHelper(List<FlowerData> list1, List<FlowerData> list2)
        {
            if (list1 is null){
                return list2;
            }
            if (list2 is null){
                return list1;
            }
            
            //merge list without the duplicate data - prioritising by timestamp
            var combined = list1.Concat(list2).ToList();

            combined = combined.OrderByDescending(x => x.Timestamp)
                                .GroupBy(x => x.Location)
                                .Select(z => z.First())
                                .ToList();

            combined = combined.OrderByDescending(x => x.NectarCount).ThenBy(y => y.DistanceFromHive).ToList();

            return combined; 
        }
        public void ExchangeData(Field area) 
        { 
            var idle = area.BeesList.Where(x => x.Duty.Equals(State.idle)).ToList();
            
            if (idle.Count <= Constant.BeesInExchange && idle.Count > 0){
                foreach (var bee in idle.ToList()){
                    DataCollection = ExhangeDataHelper(DataCollection, bee.DataCollection);
                    bee.DataCollection = CloneDataCollection(DataCollection);
                }
            }
            else if (idle.Count > Constant.BeesInExchange){
                Random random = new Random();
                for (var i = 0; i < Constant.BeesInExchange; i++){
                    var index = random.Next(0, idle.Count);
                    DataCollection = ExhangeDataHelper(DataCollection, idle[index].DataCollection);

                    var updateBee = area.BeesList.First(data => data.Equals(idle[index]));
                    updateBee.DataCollection = CloneDataCollection(DataCollection);
                    idle.Remove(idle[index]);
                }
                return;
            }
        } 
        
        // convert nectar to honey to empty
        public void MakeHoney(Beehive hive) 
        { 
            hive.updateHoney(NectarCollected); 
            NectarCollected = 0;
        } 

        //state machine -CHECK STATE CHANGES FOR IDLE and collect
        public void StateMachine(Field area, int tick)
        {
            Random rand = new Random();
            //each state will call to function
            switch (Duty){
                case State.idle:
                    var nextMove = rand.Next(0,10);
                    if (nextMove == 1 && (DataCollection.Count>0 && DataCollection[0].NectarCount > 0)){
                        Duty = State.collect;
                    }
                    else if (nextMove == 2){
                        Duty = State.forage;
                    }
                    break;
                
                case State.forage:
                    if (Target.Equals(area.Hive.Location) && RemainingDistance == -1){
                        Duty = State.home;
                    }
                    else {
                        Forage(area, tick);    
                    }
                    break;

                case State.collect:            
                    if (RemainingDistance == -1 && !(Location.Equals(area.Hive.Location))){
                        Duty = State.home;
                    }
                    else {
                        CollectNectar(area, tick);
                    }
                    break;
                    
                case State.home:
                    if (!(Location.Equals(area.Hive.Location))){
                        ReturnToHive(area.Hive);
                    }
                    else{
                        if (NectarCollected>0){
                            Duty = State.honey;
                        }
                        else if (DataCollection.Count > 0){
                            Duty = State.exchange;
                        }
                        else { //this may not happen because forage will return with information
                            Duty = State.idle;
                        }
                    }
                    break;
                
                case State.exchange:
                    ExchangeData(area);

                    if (DataCollection.Count>0 && DataCollection[0].NectarCount > 0){
                        Duty = State.collect;
                    }
                    else {
                        nextMove = rand.Next(0,2);
                        if (nextMove == 0){
                           Duty = State.idle; 
                        } else {
                            Duty = State.forage;
                        }
                    }

                    break;
                
                case State.honey:
                    MakeHoney(area.Hive);
                    Duty = State.idle; 
                    break;
            }
            DataCollection = DataCollection.OrderBy(data => data.DistanceFromHive).ThenByDescending(data => data.NectarCount).ToList();
        }
    }
}