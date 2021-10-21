using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class p3_solution
{
    // Start is called before the first frame update
    public static Hashtable map;
    static TerrainManager terrain_manager;
    public static bool haveRunnedOnce = false;
    public static Graph myGraph;
    public static int root;


    //// constructor
    //public p2_solution(TerrainManager tm, Graph g, int r)
    //{
    //    
    //}

    public static Hashtable getResult(TerrainManager tm, Graph g, int r)
    {
        terrain_manager = tm;
        myGraph = g;
        root = r;

        if (haveRunnedOnce == true)
        {
            return map;

        }
        else
        {
            toRun();
            return map;
        }
    }

    // main. sets the map.
    public static void toRun()
    {
        Debug.Log("RUN P3 SOL ONCE");
        MSC myMSC = new MSC(myGraph, root, terrain_manager);

        map = myMSC.p3Main();


        haveRunnedOnce = true;





    }
}
