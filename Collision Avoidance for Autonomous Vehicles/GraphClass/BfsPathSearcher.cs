using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BfsPathSearcher : MonoBehaviour{

    public Graph G;
    public TerrainManager terrainManager;

    public float firingRange = 5f;

    public float carLength = 2.5f;

    public int num_cars = 3;



	public BfsPathSearcher(){
        Vector3 init_car_pos = terrainManager.myInfo.start_pos;
        Graph G = new Graph(terrainManager, firingRange, carLength, num_cars, init_car_pos);
    }
        



    // input: list of IDs of the vertices that the car needs to travel through
    // output: a path linking the start position of the car to all the waypoints it has to reach in the right order.
    private List<int> findPath2(List<int> road)
    {
        var checkPoint = road.ToArray();
        List<int> path = new List<int>();

        // set start position of the car by adding the goal position 
        int a = 0;
        int b = checkPoint[0];
        int[] connecting2 = pathConnectingTwoPointsBFS(a, b);
        // add that path connecting two points to the master Path
        for (int j = 0; j < connecting2.Length; j++)
            path.Add(connecting2[j]);


        for (int i = 1; i < checkPoint.Length; i++)
        {
            // find path connecting two points
            a = checkPoint[i - 1];
            b = checkPoint[i];
            connecting2 = pathConnectingTwoPointsBFS(a, b);
            // add that path connecting two points to the master Path
            for (int j = 0; j < connecting2.Length; j++)
                path.Add(connecting2[j]);
        }
        return path;
    }


    // BFS 
    private int[] pathConnectingTwoPointsBFS(int start, int end)
    {
        // init variables
        int numV = G.getVertices().Count;


        Queue<int> myQ = new Queue<int>();
        bool[] visited = new bool[numV];
        int[] parent = new int[numV];
        for (int i = 0; i < parent.Length; i++)
        {
            parent[i] = -1;
            visited[i] = false;
        }
        visited[start] = true;
        myQ.Enqueue(start);
        while (myQ.Count > 0)
        {
            int v = myQ.Dequeue();
            if (v == end)
            {
                return BacktraceM(parent, start, end);
            }
            List<int> adjEdges = G.getEdges(v);
            foreach (int w in adjEdges)
            {
                if (visited[w] == false)
                {
                    visited[w] = true;
                    parent[w] = v;   // v --> w
                    myQ.Enqueue(w);
                }
            }

        }
        return null;
    }

    private int[] BacktraceM(int[] parent, int start, int end)
    {
        // build path by reading the "parent" array
        // List<int> path = new List<int>();
        Stack<int> path = new Stack<int>();

        int myNode = end;
        int pa = parent[myNode];
        if ((pa != -1) || (myNode == start))
        {
            while (myNode != -1)
            {
                path.Push(myNode);
                myNode = parent[myNode];
            }
        }
        int[] arrayPath = path.ToArray();
        return arrayPath;
    }

}
