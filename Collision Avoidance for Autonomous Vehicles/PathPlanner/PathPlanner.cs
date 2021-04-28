using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityStandardAssets.Vehicles.Car;
using System.Linq;


class PathPlanner : MonoBehaviour {

    public bool PlotVisitGrid;
    public bool PlotConnectionGraph;
    public bool PlotPaths;


    public TerrainManager terrainManager;
    public List<GameObject> drones; 

    public List<GameObject> goals;

    private bool[,] visitGrid;
    private ConnectionGraph connectionGraph;

    private int nDrones;
    private bool foundDrones = false;


    public int VisitGridXSize;
    public int VisitGridZSize;

    public int PlaybackSpeed = 1;

    private int hamiltonianGridXSize;
    private int hamiltonianGridZSize;

    public float WayPointTolerance = 15f;

    public float CloseToGoalThreshold = 3f;

    public float k_p = 10f;
    public float k_d = 0.5f;

    private void Start(){
        Time.timeScale = PlaybackSpeed;

        initializeVisitGrid();

    }


    private void initializeVisitGrid(){
        visitGrid = new bool[VisitGridXSize, VisitGridZSize];
    }

    private void initializeConnectionGraph(){
        connectionGraph = new ConnectionGraph(terrainManager, VisitGridXSize, VisitGridZSize);
        for (int i = 0; i < VisitGridXSize; i++) {
            for (int j = 0; j < VisitGridZSize; j++) {
                if(connectionGraph.ConnectionGrid[i,j] != null && isNodeCloseToGoal(connectionGraph.ConnectionGrid[i,j].Position)){
                    connectionGraph.ConnectionGrid[i,j] = null;
                }
            }
        }


        for (int i = 0; i < VisitGridXSize; i++) {
            for (int j = 0; j < VisitGridZSize; j++) {
                if(connectionGraph.ConnectionGrid[i,j] != null){
                    if(i < VisitGridXSize-1){
                        if(connectionGraph.ConnectionGrid[i+1,j] != null){
                            bool isColl = isCollisionRayCast(
                                connectionGraph.ConnectionGrid[i,j].Position, 
                                connectionGraph.ConnectionGrid[i+1,j].Position
                            );
                            bool edgeCloseToGoal = isEdgeCloseToGoal(
                                connectionGraph.ConnectionGrid[i,j].Position, 
                                connectionGraph.ConnectionGrid[i+1,j].Position
                            );
                            if(!isColl && !edgeCloseToGoal){
                                connectionGraph.ConnectionGrid[i,j].AddEdge( connectionGraph.ConnectionGrid[i+1,j] );
                                connectionGraph.ConnectionGrid[i+1,j].AddEdge( connectionGraph.ConnectionGrid[i,j] );
                            }
                        } 
                    }
                    if(j < VisitGridZSize - 1){
                        if(connectionGraph.ConnectionGrid[i,j+1] != null){
                            bool isColl = isCollisionRayCast(
                                connectionGraph.ConnectionGrid[i,j].Position, 
                                connectionGraph.ConnectionGrid[i,j+1].Position
                            );
                            bool edgeCloseToGoal = isEdgeCloseToGoal(
                                connectionGraph.ConnectionGrid[i,j].Position, 
                                connectionGraph.ConnectionGrid[i,j+1].Position
                            );
                            if(!isColl && !edgeCloseToGoal){
                                connectionGraph.ConnectionGrid[i,j].AddEdge( connectionGraph.ConnectionGrid[i,j+1] );
                                connectionGraph.ConnectionGrid[i,j+1].AddEdge( connectionGraph.ConnectionGrid[i,j] );
                            }
                        } 
                    }
                }
            }
        }
    }

    

    private bool isCollisionRayCast(Vector3 myPos, Vector3 nextPos){
        int layer_mask = LayerMask.GetMask("Player", "Enemy");
        RaycastHit hit;
        Vector3 distance = nextPos - myPos;

        bool isHit = Physics.Raycast(myPos, distance.normalized , out hit, distance.magnitude, layer_mask);
        return isHit;
    }    


    void OnDrawGizmos()
    {
        if(terrainManager == null){
            return;
        }

        float x_low = terrainManager.myInfo.x_low;
        float x_high = terrainManager.myInfo.x_high;
        float z_low = terrainManager.myInfo.z_low;
        float z_high = terrainManager.myInfo.z_high;

        if(PlotVisitGrid && visitGrid != null){
            float stepX = (x_high - x_low) / VisitGridXSize;
            float stepZ = (z_high - z_low) / VisitGridZSize;


            Vector3 cubeWidth = new Vector3(stepX, 0.1f, stepZ);

            for (int i = 0; i < VisitGridXSize; i++) {
                float posX = x_low + (i + 0.5f)*stepX;
                for (int j = 0; j < VisitGridZSize; j++) {
                    float posZ = z_low + (j + 0.5f)*stepZ;
                    if(visitGrid[i,j]){
                        Gizmos.color = Color.blue;
                    } else {
                        Gizmos.color = Color.red;
                    }
                    Gizmos.DrawWireCube(new Vector3(posX, 0, posZ), cubeWidth);
                }
            }
        }

        
        if(PlotConnectionGraph && connectionGraph != null){
            for (int i = 0; i < VisitGridXSize; i++) {
                for (int j = 0; j < VisitGridZSize; j++) {
                    Gizmos.color = Color.magenta;
                    if(connectionGraph.ConnectionGrid[i, j] != null){
                        Gizmos.DrawSphere(connectionGraph.ConnectionGrid[i, j].Position, 1.5f);
                        foreach (ConnectionGraphNode edge in connectionGraph.ConnectionGrid[i,j].Edges){
                            Gizmos.DrawLine(connectionGraph.ConnectionGrid[i, j].Position, edge.Position);
                        }

                    }
                }
            }
        }
    }

    private void FixedUpdate(){
        if(!foundDrones){
            drones = GameObject.FindGameObjectsWithTag("Drone").ToList();
            findGoals();
            foundDrones = true;
            initializeConnectionGraph();
            calculateDronePaths();
        }
    }

    private void findGoals(){
        goals = Resources
            .FindObjectsOfTypeAll<GameObject>()
            .Where(obj => obj.name == "Sphere" && obj.transform.parent == null)
            .ToList();
    }

    private void calculateDronePaths(){
        foreach (GameObject drone in drones){
            DroneAI droneAI = drone.GetComponent<DroneAI>();
            if(droneAI != null){
                List<Vector3> path = calculateDronePath(droneAI);
                List<Vector3> pathShiftedRight = shiftPathRight(path);
                if(path != null && path.Count > 0){
                    droneAI.Path = pathShiftedRight;
                    droneAI.WayPointTolerance = WayPointTolerance;
                    droneAI.k_d = k_d;
                    droneAI.k_p = k_p;
                    droneAI.PlotPath = PlotPaths;
                }
            }

            
        }
    }

    private List<Vector3> calculateDronePath(DroneAI droneAI){
        Vector3 startPos = droneAI.transform.position;
        Vector3 endPos = droneAI.my_goal_object.transform.position;

        ConnectionGraphNode startNode = findClosestNode(startPos);
        ConnectionGraphNode endNode = findClosestNode(endPos);
        
        List<ConnectionGraphNode> visitedPoints = new List<ConnectionGraphNode>(); 

        Queue<List<ConnectionGraphNode>> queue = new Queue<List<ConnectionGraphNode>>();

        List<ConnectionGraphNode> rootPath = new List<ConnectionGraphNode>();
        rootPath.Add(startNode);

        queue.Enqueue(rootPath);    

        while(queue.Count > 0){
            List<ConnectionGraphNode> currentPath = queue.Dequeue();
            ConnectionGraphNode currentGraphNode = currentPath.Last();

            if(currentGraphNode == endNode){
                List<Vector3> path = new List<Vector3>();
                path.Add(startPos);
                path.AddRange(currentPath.Select(node => node.Position));
                path.Add(endPos);
                return path;
            }

            foreach (ConnectionGraphNode edge in currentGraphNode.Edges){
                if(!visitedPoints.Contains(edge)){
                    List<ConnectionGraphNode> edgePath = new List<ConnectionGraphNode>();
                    edgePath.AddRange(currentPath);
                    edgePath.Add(edge);
                    visitedPoints.Add(edge);
                    queue.Enqueue(edgePath);
                }
            }
        }
        return null;
    }

    private List<Vector3> shiftPathRight(List<Vector3> path){
        if(path != null && path.Count > 1){
            List<Vector3> shiftedPath = new List<Vector3>();
            shiftedPath.Add(path[0]);
            for (int i = 1; i < path.Count - 1; i++)
            {
                Vector3 direction = (path[i+1] - path[i-1]).normalized;
                Vector3 right = new Vector3(direction.z, 0f, -direction.x);
                shiftedPath.Add(path[i] +  0.9f*VisitGridXSize/4 * right);
            }
            shiftedPath.Add(path[path.Count-1]);
            return shiftedPath;

        } else {
            return path;
        }
    }        


    private ConnectionGraphNode findClosestNode(Vector3 startPos){
        ConnectionGraphNode closestNode = null;
        float closestDistance = 0f;
        for (int i = 0; i < connectionGraph.VisitGridXSize; i++) {
            for (int j = 0; j < connectionGraph.VisitGridZSize; j++) {
                if(connectionGraph.ConnectionGrid[i,j] != null){
                    float currentDistance = (connectionGraph.ConnectionGrid[i,j].Position - startPos).magnitude;
                    if(closestNode == null || currentDistance < closestDistance){
                        closestNode = connectionGraph.ConnectionGrid[i,j];
                        closestDistance = currentDistance;
                    }
                }
            }
        }
        return closestNode;            
    }

    private bool isEdgeCloseToGoal(Vector3 nodePosition, Vector3 nodePosition2){
        return false;
        Vector3 edgeCenter = 0.5f*(nodePosition + nodePosition2);
        foreach(GameObject goal in goals){
            if((goal.transform.position - edgeCenter).magnitude < 0.5f*CloseToGoalThreshold){
                return true;
            }
        }
        return false;
    }

    private bool isNodeCloseToGoal(Vector3 nodePosition){
        return false;
        foreach(GameObject goal in goals){
            if((goal.transform.position - nodePosition).magnitude < CloseToGoalThreshold){
                return true;
            }
        }
        return false;
    }




}