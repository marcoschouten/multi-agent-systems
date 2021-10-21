using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// solutions for problem 2 & 3

// README:
// this class has the task to navigate the graph and return a list of all the guards
// is divided into three parts:
// A) finds Guards that covers the whole space
// B) finds Minumum Set of Guards that coveres the whole space (Minumum Set Cover Problem)
// C) finds paths that connects selected Guards


// ABBREVIATED NAMES:
// POI = point of interest
// LOS = line of sight

public class MSC : MonoBehaviour {

    int numIterations = 5000; // random Search
    int numIters = 70; // adding redudant guards


    // useless
    public Graph G;
    public int root;
    TerrainManager terrain_manager;

    // MSC
    public int numV; // Number of vertices in the graph 
    public int numCovered; // Number of vertices "covered" i.e. that are in LOS with at least 1 guard
    public bool[] coveredPOI; // keep track of which node is already in the msc
    public Hashtable Guards;  // Guard idx --> List of all the guys in Line of Sight

   

    public Hashtable POI; // points of interest
    List<int> bestPermutation;
    public List<int> GoalGuards; // contains the idx of all the "final" guards which will compose goal points for our trajectory
    public System.Random random = new System.Random();


    public GameObject[] enemies;


    //_____________________________________________________ member functions _____________________________________________________
    // constructor
    public MSC(Graph g, int r, TerrainManager terrain_manager) 
    {
        this.G = g;
        this.terrain_manager = terrain_manager;
        this.root = r;

        this.numV = (g.getVertices()).Count;
        this.numCovered = 0;
        this.coveredPOI = new bool[numV];
        for (int i = 0; i < numV; i++)
            coveredPOI[i] = false;
        this.Guards = new Hashtable();
        this.POI = g.getVertices();  
        this.GoalGuards =  new List<int>();
        this.bestPermutation = new List<int>();
    }


    // main for p3
    public Hashtable p3Main()
    {

        // find closest vertex to each enemy
        enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GoalGuards = new List<int>(); // read the checkpoints.
        int nearestID = 0;
        Vector3 nearestPOS;

        foreach (GameObject obj in enemies)
        {
            (nearestPOS, nearestID) = G.Nearest(POI, obj.transform.position);
            GoalGuards.Add(nearestID);
        }






        //// 2
        Hashtable clusterPaths = TSP_2(); // INPUT: GoalGuards

        // 3
        // find path for each car // INPUT: clusterPaths;
        List<int> pathForCar1 = findPath2((List<int>)clusterPaths["Car1"]);
        List<int> pathForCar2 = findPath2((List<int>)clusterPaths["Car2"]);
        List<int> pathForCar3 = findPath2((List<int>)clusterPaths["Car3"]);

        // ZIP data
        Hashtable zipPaths = new Hashtable();
        zipPaths.Add("Car1", pathForCar1);
        zipPaths.Add("Car2", pathForCar2);
        zipPaths.Add("Car3", pathForCar3);
        return zipPaths;
    }

    // main for p2
    public Hashtable Main()
    {
        // 0
        GenerateGuards(); // find Guards (such that you can cover the whole Points of interests "POI")



        //// 1
        // find GoalGuards (minum set of guards that can cover the whole points of interest "POI")
        List<int> myGoalGuards = ComputeMSC();
        Debug.Log("AAAGuardList:" + myGoalGuards.Count);
        // print goal guards
        //int currentID = 0;
        //int precedentID = myGoalGuards[0];
        //for (int j = 1; j < myGoalGuards.Count; j++)
        //{
        //    currentID = myGoalGuards[j];
        //    Vector3 u = (Vector3)POI[precedentID];
        //    Vector3 v = (Vector3)POI[currentID];
        //    Debug.DrawLine(u, v, Color.white, 100f);
        //    precedentID = currentID;
        //}





        //// 2
        Hashtable clusterPaths = TSP_2(); // find the right Order to visit the guards 



        // 3
        // find path for each car
        List<int> pathForCar1 = findPath2((List<int>)clusterPaths["Car1"]);
        List<int> pathForCar2 = findPath2((List<int>)clusterPaths["Car2"]);
        List<int> pathForCar3 = findPath2((List<int>)clusterPaths["Car3"]);

        // ZIP data
        Hashtable zipPaths = new Hashtable();
        zipPaths.Add("Car1", pathForCar1);
        zipPaths.Add("Car2", pathForCar2);
        zipPaths.Add("Car3", pathForCar3);
        return zipPaths;
        //return null;
    }


    // ----------------------------------------------------3 new -------------------------------------------------------------------
    // input: carID
    // output: a path linking the start position of the car to all the waypoints it has to reach in the right order.
    private List<int> findPath2(List<int> road)
    {
        var checkPoint = road.ToArray();
        List<int> path = new List<int>();

        // set start position of the car by adding the goal position 
        int a = this.root;
        int b = checkPoint[0];
        int[] connecting2 = pathConnectingTwoPoints(a, b);
        // add that path connecting two points to the master Path
        for (int j = 0; j < connecting2.Length; j++)
            path.Add(connecting2[j]);


        for (int i = 1; i < checkPoint.Length; i++)
        {
            // find path connecting two points
            a = checkPoint[i - 1];
            b = checkPoint[i];
            connecting2 = pathConnectingTwoPoints(a, b);
            // add that path connecting two points to the master Path
            for (int j = 0; j < connecting2.Length; j++)
                path.Add(connecting2[j]);
        }
        return path;
    }





    //// ----------------------------------------------------3 -old -------------------------------------------------------------------
    //// input: carID
    //// output: a path linking the start position of the car to all the waypoints it has to reach in the right order.
    //private List<int> findPath(string v)
    //{
    //    int N = bestPermutation.Count; // P = 7
    //    int P = (int)N / 3; // P = 2;  NOTE that 3 is the "number of cars"
    //    int startIDX = 0;
    //    int getAmount = 0;
    //    List<int> path = new List<int>();

    //    // select Waypoints  "slicing" 
    //    // e.g. from 0 to --> 4 are the waypoints for car1, from 4--> 8 are for car2, from 8--> 12 are for car3
    //    if (v == "Car1")
    //    {
    //        startIDX = 0;
    //        getAmount = P;
    //    }
    //    if (v == "Car2")
    //    {
    //        startIDX = P;
    //        getAmount = P;
    //    }
    //    if (v == "Car3")
    //    {
    //        startIDX = P+P;
    //        getAmount = N - startIDX;
    //    }


    //    var checkPoint = bestPermutation.GetRange(startIDX, getAmount).ToArray();



    //    // set start position of the car by adding the goal position 
    //    int a = this.root;
    //    int b = checkPoint[0];
    //    int[] connecting2 = pathConnectingTwoPoints(a, b);
    //    // add that path connecting two points to the master Path
    //    for (int j = 0; j < connecting2.Length; j++)
    //        path.Add(connecting2[j]);


    //    for (int i = 1; i < checkPoint.Length; i++)
    //    {
    //        // find path connecting two points
    //        a = checkPoint[i-1];
    //        b = checkPoint[i];
    //        connecting2 = pathConnectingTwoPoints(a,b);
    //        // add that path connecting two points to the master Path
    //        for (int j = 0; j < connecting2.Length; j++)
    //            path.Add(connecting2[j]);
    //    }
    //    return path;
    //}

    // Helper:
    //  find path that connects two points using a DFS approach
    // returns sequence of Int (ID nodes) that connects the two points in the right order
    // FIXED MEM ERRO,  now is correct
    private int[] pathConnectingTwoPoints(int start, int end)
    {

        // init variables
        bool[] visited = new bool[numV]; // by default each value should be false
        int[] parent = new int[numV]; // used to backtrack to retrieve the path.
        for (int i = 0; i < parent.Length; i++) // initialise parent
        {
            parent[i] = -1;
            visited[i] = false;
        } 

        int curNode = start;
        List<int> path = new List<int>();

        //int iterN = 0;
        while (curNode != end )
        {
            //if (curNode > visited.Length)
            ////Debug.Log("visted is an array of length: " + visited.Length);
            //    Debug.Log("A-curNode= " + curNode);
            if (curNode < 0)
            {
                //Debug.Log("B-curNode= " + curNode);
                //Debug.Log("B-start= " + start);
                //Vector3 u = (Vector3)POI[78];
                //Vector3 v = (Vector3)POI[443];
                //Debug.DrawLine(u, v, Color.red, 100f);
                return null;
            }

            visited[curNode] = true;

            List<int> neighbors = G.getEdges(curNode);
            List<int> deepNeighbors = new List<int>();
            // 1) esamino le celle adiacenti, e salvo quelle Deep
            foreach(int neigh in neighbors)            {
                if (visited[neigh] == false)
                {
                    deepNeighbors.Add(neigh);
                }
            }
            // se ho celle deep, vado in profondità scelgiendo quella preferita dall'euristica, set visited la cella vistata
            if (deepNeighbors.Count > 0) // esistono neighbour deep, vai deep
            {
                // (A) sort Nodes to visit according using an informed search, prioritize nodes closer to the end goal
                int N = deepNeighbors.Count;
                int[] index = Enumerable.Range(0, N).ToArray<int>();
                Array.Sort<int>(index, (a, b) => HeuristicDistance((Vector3)POI[deepNeighbors[a]], (Vector3)POI[end]).CompareTo(HeuristicDistance((Vector3)POI[deepNeighbors[b]], (Vector3)POI[end])));
                int[] sortedNeighbors = new int[deepNeighbors.Count];
                for (int j = 0; j < deepNeighbors.Count; j++)
                {
                    sortedNeighbors[j] = deepNeighbors[index[j]];
                }
                //debug check sorting, OK the first value is higher than the second, therefore the array is correctly sorted.
                //float firstValCheck = HeuristicDistance((Vector3)POI[neighbors[0]], (Vector3)POI[end]);
                //float firstValCheck2 = HeuristicDistance((Vector3)POI[neighbors[1]], (Vector3)POI[end]);



                // (B) move to the "best" neighbour
                int moveTo = sortedNeighbors[0]; // the first element is the "best" one i.e. closer to the end goal.
                parent[moveTo] = curNode;
                curNode = moveTo;

            }
            else // backtrack towards my father.
            {
                int moveTo = parent[curNode];
                curNode = moveTo;
                if (curNode == start)
                {
                    Debug.Log("C-curNode= " + curNode);
                }
            }
        }

        // Done ! At this point we just have to walk back from the end using the parent
        // If end does not have a parent, it means that it has not been found.
        int node = end;
        while (node != -1)
        {
            path.Add(node);
            node = parent[node];

        }

        // reverse array because we added from  the end to the beginning
        int[] arrayPath = path.ToArray();
        Array.Reverse(arrayPath);

        return arrayPath;
    }

    private float HeuristicDistance(Vector3 a, Vector3 b)
    {
        return Vector3.Distance(a,b);
    }





    // -----------------------------------------------------2.1 -------------------------------------------------------------------
    public Hashtable TSP_2()
    {
        // build 3 poles
        List<int> pole1 = new List<int>();
        List<int> pole2 = new List<int>();
        List<int> pole3 = new List<int>();

        // triangolo al contrario
        Vector3 center_map = new Vector3(250.0f, 0.0f, 250.0f);
        Vector3 pole1_pos = new Vector3(center_map.x - 100.0f, 0.0f, center_map.z + 100.0f);
        Vector3 pole2_pos = new Vector3(center_map.x + 100.0f, 0.0f, center_map.z + 100.0f);
        Vector3 pole3_pos = new Vector3(center_map.x, 0.0f, center_map.z - 125.0f);
        Debug.DrawLine(pole1_pos, pole2_pos, Color.red, 100f);
        Debug.DrawLine(pole2_pos, pole3_pos, Color.red, 100f);
        Debug.DrawLine(pole3_pos, pole1_pos, Color.red, 100f);


        // distribuisco ogni guardia nel cluster giusto
        float[] distFromPole = new float[3];
        for (int i = 0; i < GoalGuards.Count; i++)
        {
            distFromPole[0] = Vector3.Distance((Vector3)POI[this.GoalGuards[i]], pole1_pos);
            distFromPole[1] = Vector3.Distance((Vector3)POI[this.GoalGuards[i]], pole2_pos);
            distFromPole[2] = Vector3.Distance((Vector3)POI[this.GoalGuards[i]], pole3_pos);
            float maxValue = distFromPole.Min();
            int maxIndex = distFromPole.ToList().IndexOf(maxValue);
            if (maxIndex == 0)
                pole1.Add(GoalGuards[i]);
            else if (maxIndex == 1)
                pole2.Add(GoalGuards[i]);
            else if (maxIndex == 2)
                pole3.Add(GoalGuards[i]);
        }


        // search for best permutation within a pole
        pole1 = FindBestPermutation(pole1);
        pole2 = FindBestPermutation(pole2);
        pole3 = FindBestPermutation(pole3);

        Hashtable clusteredGuards = new Hashtable();
        clusteredGuards.Add("Car1", pole1);
        clusteredGuards.Add("Car2", pole2);
        clusteredGuards.Add("Car3", pole3);
        return clusteredGuards;

    }

    // helper for TSP_2
    private List<int> FindBestPermutation(List<int> input)
    {
        List<int> permutation = input;
        float value = 0;
        float minValue = float.MaxValue;
        int bestIteration = -1;
        
        for (int i = 0; i < numIterations; i++)
        {
            Shuffle(permutation);
            value = ComputeValueSingle(permutation);
            if (value < minValue)
            {
                minValue = value;
                bestPermutation = permutation;
                bestIteration = i;
            }
        }
        Debug.Log("best perm #" + bestIteration);
        return permutation;

    }

    // helper for "FindBestPermutation", summs distances from i--> i+1
    private float ComputeValueSingle(List<int> permutation)
    {
        float dist = 0;
        for (int i = 1; i < permutation.Count; i++)
        {
            int idA = permutation[i-1];
            int idB = permutation[i];
            dist += Vector3.Distance((Vector3)POI[idA], (Vector3)POI[idB]);   
        }
        dist += Vector3.Distance((Vector3)POI[root], (Vector3)POI[permutation[0]]);
        return dist;
    }












    //// -----------------------------------------------------2 -------------------------------------------------------------------

    //// input: List<int> of goal position  e.g. [1, 4, 6, 2, 3]
    //// output: List<int> containing goal position + 3CarID in an optimal permutation, [-2, 1, 4, 6, -1, 2, -3, 3]
    //// NOTE: there negative numbers are filtered out, but the meaning still holds.
    //// this means that carID "-1" will need to travel to: start--> 2
    //// this means that carID "-2" will need to travel to: start--> 1 --> 4 --> 6
    //// this means that carID "-3" will need to travel to: start--> 3
    //public List<int> TravellingSalesmanProblem()
    //{
    //    // initialise - build permutation vector
    //    List<int> permutation = this.GoalGuards;
    //    int value = 0;

    //    //List<int> bestPermutation = new List<int>();
    //    int minValue = int.MaxValue;

    //    // search
    //    int bestIteration = -1;
    //    int numIterations = 2000;
    //    for (int i = 0; i < numIterations; i++)
    //    {
    //        // random permutation
    //        // var shuffledPermutation = permutation.OrderBy(a => Guid.NewGuid()).ToList(); has some bugs
    //        Shuffle(permutation);

    //        // evaluation of permutation
    //        value = ComputeValue(permutation);
    //        if (value < minValue)
    //        {
    //            minValue = value;
    //            bestPermutation = permutation;
    //            bestIteration = i;
    //        }

    //    }
    //    Debug.Log("best iteration:" + bestIteration);
    //    return bestPermutation;
    //}

    // helper for Travelling Salesman 
    //private int ComputeValue(List<int> shuffledGoals)
    //{
    //    int N = shuffledGoals.Count; // P = 7
    //    int P = (int) N / 3; // P = 2;
    //    float distance = 0.0f;

    //    // slicing
    //    int startCar1 = 0;
    //    int getAmount1 = P;

    //    int startCar2 = P;
    //    int getAmount2 = P;

    //    int startCar3 = P+P;
    //    int getAmount3 = N - startCar3;

    //    //var checkPointsCar1 = shuffledGoals.GetRange(startCar1, getAmount1).ToArray();
    //    //var checkPointsCar2 = shuffledGoals.GetRange(startCar2, getAmount2).ToArray();
    //    //var checkPointsCar3 = shuffledGoals.GetRange(startCar3, getAmount3).ToArray();

    //    float distancesCar1 = 0.0f;
    //    float distancesCar2 = 0.0f;
    //    float distancesCar3 = 0.0f;

    //    // add distance from A-->B
    //    for (int i = startCar1; i < startCar1+ getAmount1; i++)
    //    {
    //        if (i == startCar1) // switching to a new car
    //        {
    //            int idxA = this.root;
    //            int idxB = shuffledGoals[i];
    //            distancesCar1 += Vector3.Distance((Vector3)POI[idxA], (Vector3)POI[idxB]);
    //        }
    //        else  // just move towards a new point
    //        {
    //            int idxA = shuffledGoals[i - 1];
    //            int idxB = shuffledGoals[i];
    //            distancesCar1 += Vector3.Distance((Vector3)POI[idxA], (Vector3)POI[idxB]);
    //        }





    //    }// add distance from A-->B
    //    for (int i = startCar2; i < startCar2 + getAmount2; i++)
    //    {
    //        if (i == startCar2) // switching to a new car
    //        {
    //            int idxA = this.root;
    //            int idxB = shuffledGoals[i];
    //            distancesCar2 += Vector3.Distance((Vector3)POI[idxA], (Vector3)POI[idxB]);
    //        }
    //        else  // just move towards a new point
    //        {
    //            int idxA = shuffledGoals[i - 1];
    //            int idxB = shuffledGoals[i];
    //            distancesCar2 += Vector3.Distance((Vector3)POI[idxA], (Vector3)POI[idxB]);
    //        }
    //    }





    //    // add distance from A-->B
    //    for (int i = startCar3; i < startCar3 + getAmount1; i++)
    //    {
    //        if (i == startCar3) // switching to a new car
    //        {
    //            int idxA = this.root;
    //            int idxB = shuffledGoals[i];
    //            distancesCar3 += Vector3.Distance((Vector3)POI[idxA], (Vector3)POI[idxB]);
    //        }
    //        else  // just move towards a new point
    //        {
    //            int idxA = shuffledGoals[i - 1];
    //            int idxB = shuffledGoals[i];
    //            distancesCar3 += Vector3.Distance((Vector3)POI[idxA], (Vector3)POI[idxB]);
    //        }
    //    }




    //    int avgDistances = (int) ((distancesCar1 + distancesCar2 + distancesCar3) /3);
    //    return avgDistances;
    //}

    //// helper for Travelling Salesman  backup
    //private int ComputeValueOLD(List<int> shuffledGoals)
    //{
    //    int N = shuffledGoals.Count; // P = 7
    //    int P = (int)N / 3; // P = 2;
    //    float distance = 0.0f;

    //    // add distance from A-->B
    //    for (int i = 0; i < N; i++)
    //    {
    //        if (i == P * i) // switching to a new car
    //        {
    //            int idxA = this.root;
    //            int idxB = shuffledGoals[i];
    //            distance += Vector3.Distance((Vector3)POI[idxA], (Vector3)POI[idxB]);
    //        }
    //        else  // just move towards a new point
    //        {
    //            int idxA = shuffledGoals[i - 1];
    //            int idxB = shuffledGoals[i];
    //            distance += Vector3.Distance((Vector3)POI[idxA], (Vector3)POI[idxB]);

    //        }

    //    }
    //    int intDistance = (int)distance;
    //    return intDistance;
    //}





    // -----------------------------------------------------1 -------------------------------------------------------------------

    // compute Minumum Set Cover
    // return the list of guards, that one need to reach
    public List<int> ComputeMSC()
    {
        // at the beginning the "coveredPOI" array should be all set to TRUE.
        // it will add POI over every iteration until it is all every POI is covered "using the least amount of Gaurds"
        for (int i = 0; i < coveredPOI.Length; i++)
        {
            coveredPOI[i] = true;
        }

        int numPOILeft = coveredPOI.Length;
        while (numPOILeft > 0)
        {
            // find the largest Guard, one one which has the most LOS POI == true
            int largestGuardIdx = 0;
            int maxNumPOICovered = 0;
            List<int> guardList; // guardList contains the index of the POI in LOS
            // problem is that guardList is null. i need to iterate over the true keys.
            foreach (DictionaryEntry guardID in Guards)  //for (int i = 0; i < Guards.Count; i++)
            {
                int numPOICovered = 0;
                guardList = (List<int>) guardID.Value;  // gets the list of the POI in LOS with guard "i"
                for (int j = 0; j < guardList.Count; j++)   // for each Guard's List of POI, count how many new POI would it cover it this guard were to be added.
                {
                    int idxPOI = guardList[j];
                    if (coveredPOI[idxPOI] == true)   // this point of intrest is still in the game and counts!
                    {
                        numPOICovered++;
                    }
                }

                if (numPOICovered > maxNumPOICovered)
                {
                    largestGuardIdx = (int) guardID.Key;
                }
            }
            GoalGuards.Add(largestGuardIdx);  // add the Guard. this will be one of the the few points that the robot needs to reach.


            // remove from the game every POI in LOS with the selected Guard
            List<int> largestGuardList = (List<int>)Guards[largestGuardIdx]; 
            for (int j = 0; j < largestGuardList.Count; j++)   
            {
                int idxPOI = largestGuardList[j];
                coveredPOI[idxPOI] = false;
            }

            // count how many POI are still left, (we need to add a guard to cover those)
            numPOILeft = CountCovered(); // tells how many points are "true" that is: "still in the game and needs to be removed"
        }
        return GoalGuards;


    }

    // -----------------------------------------------------0 -------------------------------------------------------------------
    // this is needed only for problem 2
    // find guards such that every POI in included at least once within a Guard's List
    public void GenerateGuards()
    {
        
        // add essentials
        while (numCovered < numV)
        {

            // add a new Guard to the Guard list
            int newGuardId = -1;
            do
            {
                newGuardId = random.Next(0, numV);
            } while ((Guards.ContainsKey(newGuardId)) || (coveredPOI[newGuardId] == true));
 


            // find all of the points who are in line of sight w.r.t the added Guard.
            List<int> newGuardList = new List<int>();
            for (int i = 0; i < numV; i++)
            {
                if (CollisionFree((Vector3)POI[i], (Vector3)POI[newGuardId]))
                {
                    newGuardList.Add(i);
                    coveredPOI[i] = true; // set to true
                }
            }
            Guards.Add(newGuardId, newGuardList);



            // check if every POI is covered
            numCovered = CountCovered();


        }
        // add redundancy
        int numIters = 70;
        int iterGG = 0;
        while (iterGG < numIters)
        {
            // generate a new Guard
            int newGuardId = -1;
            do
            {
                newGuardId = random.Next(0, numV);
            } while ((Guards.ContainsKey(newGuardId)));

            // find all of the points who are in line of sight w.r.t the added Guard.
            List<int> newGuardList = new List<int>();
            for (int i = 0; i < numV; i++)
            {
                if (CollisionFree((Vector3)POI[i], (Vector3)POI[newGuardId]))
                {
                    newGuardList.Add(i);
                    coveredPOI[i] = true; // set to true
                }
            }
            Guards.Add(newGuardId, newGuardList);
            iterGG++;

        }

    }

    // HELPERS DOWN --------------------------------------------------------------------------------------------------------------
    private int CountCovered()
    {
        numCovered = 0;
        for (int i = 0; i < numV; i++)
        {
            if (coveredPOI[i] == true)
            {
                numCovered++;
            }
        }
        return numCovered;
    }

    private bool CollisionFree(Vector3 pos1, Vector3 pos2)
    {
        int testing_points = (int)Vector3.Distance(pos1, pos2) * 5;
        bool obstFree = true;

        for (int i = 1; i < testing_points; i++)
        {
            Vector3 pos_new = (pos2 - pos1) * i / (testing_points - 1) + pos1;
            if (!traversable(pos_new.x, pos_new.z))
            {
                obstFree = false;
                break;
            }
        }
        return obstFree;
    }

    private bool traversable(float x_pos, float z_pos)
    {
        int ind_i = terrain_manager.myInfo.get_i_index(x_pos);
        int ind_j = terrain_manager.myInfo.get_j_index(z_pos);
        float transversibility = terrain_manager.myInfo.traversability[ind_i, ind_j];
        if (transversibility == 0.0f)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void Shuffle<T>(IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }





}
