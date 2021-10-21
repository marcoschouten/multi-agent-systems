using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;



public class ConnectionGraph{

    public ConnectionGraphNode[,] ConnectionGrid{
        get { return connectionGrid; }
    }
    private ConnectionGraphNode[,] connectionGrid;

    public int VisitGridXSize{
        get { return visitGridXSize; }
    }
    private int visitGridXSize;

    public int VisitGridZSize{
        get { return visitGridZSize; }
    }
    private int visitGridZSize;
    public ConnectionGraph(TerrainManager terrain_manager, int visitGridXSize, int visitGridZSize){
        this.visitGridXSize = visitGridXSize;
        this.visitGridZSize = visitGridZSize;


        float x_low = terrain_manager.myInfo.x_low;
        float x_high = terrain_manager.myInfo.x_high;
        float z_low = terrain_manager.myInfo.z_low;
        float z_high = terrain_manager.myInfo.z_high;
        float stepX = (x_high - x_low) / visitGridXSize;
        float stepZ = (z_high - z_low) / visitGridZSize;

        connectionGrid = new ConnectionGraphNode[visitGridXSize, visitGridZSize];



        for (int i = 0; i < visitGridXSize; i++) {
            float posX = x_low + (i + 0.5f)*stepX;
            for (int j = 0; j < visitGridZSize; j++) {
                float posZ = z_low + (j + 0.5f)*stepZ;
                int iTerrain = terrain_manager.myInfo.get_i_index(posX);
                int jTerrain = terrain_manager.myInfo.get_j_index(posZ);
                if(terrain_manager.myInfo.traversability[iTerrain, jTerrain] == 0f){
                    connectionGrid[i,j] = new ConnectionGraphNode(new Vector3(posX, 0, posZ));
                } 
            }
        }
    }
}

public class ConnectionGraphNode{

    public List<ConnectionGraphNode> Edges{
        get { return edges; }
    }
    private List<ConnectionGraphNode> edges;


    public Vector3 Position{
        get { return position; }
    }
    private Vector3 position;

    public bool AddedToTree;

    public ConnectionGraphNode(Vector3 position){
        this.position = position;
        edges = new List<ConnectionGraphNode>();
    }

    public void AddEdge(ConnectionGraphNode edgeNode){
        edges.Add(edgeNode);
    }

}