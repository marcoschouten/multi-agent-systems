using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[RequireComponent(typeof(DroneController))]
public class DroneAIPP : MonoBehaviour
{

    private DroneController m_Drone; // the car controller we want to use

    public GameObject my_goal_object;
    public GameObject terrain_manager_game_object;
    public List<DroneAI> HigherPriorityDrones = new List<DroneAI>();
    public List<DroneAI> AllDrones = new List<DroneAI>();


    TerrainManager terrain_manager;

    public List<Vector3> Path;


    public int step;

    public float WayPointTolerance = 15f;
    public float DroneDistanceTolerance = 15f;
    public float k_p = 10f;
    public float k_d = 0.5f;
    public float DirectionThreshold = 0.95f;

    private float VelocityFactor = 10f;
    private Vector3 stopPosition = Vector3.zero;
    private float stopTime = 0f;
    private bool isStopped = false;

    public bool PlotPath = true;

    public Vector3 target_velocity;
    Vector3 old_target_pos;
    Vector3 desired_velocity;

    public bool canSeeOtherDrone = false;
    public bool closeToHigherPrio = false;


    public ConnectionGraph connectionGraph;

    private void Start()
    {
        m_Drone = GetComponent<DroneController>();
        terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();        
    }


    public Color GetColor(){
        return GetComponentInChildren<Renderer>().material.GetColor("_Color");
    }


   private void FixedUpdate(){
        if(Path == null || Path.Count == 0){
            return;
        }


        Vector3 movement_direction = m_Drone.GetComponent<Rigidbody>().velocity.normalized;
        if(step > 0){
            movement_direction = (Path[step] - Path[step-1]).normalized;
        }


        canSeeOtherDrone = false;
        foreach(DroneAI otherDrone in AllDrones){            
            if(otherDrone != this){
                Vector3 distance = otherDrone.transform.position - transform.position;
                bool canSee = distance.magnitude < DroneDistanceTolerance && Vector3.Dot(distance.normalized, movement_direction) > DirectionThreshold;
                if(canSee){
                }

            }
        }

        float VehicleWidth = 2.5f;

        Vector3 velocityVector =  VelocityFactor * GetComponent<Rigidbody>().velocity;
        Vector3 movingDirection = velocityVector.normalized;
        Vector3 position = transform.position;


        closeToHigherPrio = false;
        foreach (DroneAI higherPriorityDrone in HigherPriorityDrones)
        {
            if(higherPriorityDrone != this){
                Vector3 otherPosition = higherPriorityDrone.transform.position;
                Vector3 otherVelocity = VelocityFactor*higherPriorityDrone.GetComponent<Rigidbody>().velocity;

                bool canCollide = CanVehiclesCollide(
                    position, 
                    velocityVector, 
                    otherPosition,
                    otherVelocity 
                    );
                if(canCollide){
                    closeToHigherPrio = true;
                    break;   
                }
            }
        }

        float waitingTime = 2.5f;

        Vector3 target_position = Path[step];
        old_target_pos = target_position;            
        target_velocity = (target_position - old_target_pos) / Time.fixedDeltaTime;            
        if(canSeeOtherDrone || closeToHigherPrio || (Time.time < stopTime + waitingTime)){
            if(!isStopped){
                isStopped = true;
                stopPosition = transform.position;
                stopTime = Time.time;
            }
            target_position = stopPosition;
            target_velocity = Vector3.zero;
        } else {
            isStopped = false;
        }



        Vector3 threshold_distance = Path[step] - transform.position;        



        // a PD-controller to get desired velocity            
        Vector3 position_error = target_position - transform.position;            
        Vector3 velocity_error = target_velocity - m_Drone.GetComponent<Rigidbody>().velocity;           
        Vector3 desired_acceleration = k_p * position_error + k_d * velocity_error;

        m_Drone.Move_vect(desired_acceleration);

        if(step < Path.Count - 1){
            float droneHeight = 5f;
            Vector3 droneCenter = transform.position + (droneHeight/2)*transform.up -(droneHeight/2)*movement_direction;

            Vector3 floatingPathStep = Path[step+1];
            floatingPathStep.y = droneCenter.y;


            if (threshold_distance.magnitude < WayPointTolerance){
                if(!isCollisionRayCast(droneCenter, floatingPathStep)){
                    step += 1;
                }

                //Debug.Log(name +  "Step increased to: " + step);
            }
        }
        //Debug.DrawLine(transform.position, Path[step], Color.red, 0.5f);
        Debug.DrawLine(transform.position, transform.position + velocityVector, GetColor(), 0.5f);


    }

        void OnDrawGizmos(){
            if(PlotPath && Path != null && Path.Count > 1){
                Gizmos.color = GetColor();
                Gizmos.DrawSphere(Path[step], 1f);
                Vector3 oldWaypoint = Path[0];
                for (int i = 1; i < Path.Count; i++)
                {
                    Vector3 waypoint = Path[i];
                    Gizmos.DrawLine(oldWaypoint, waypoint);
                    oldWaypoint = waypoint;                    
                }
            }
    }

    private bool isCollisionRayCast(Vector3 myPos, Vector3 nextPos){
        RaycastHit[] hits;
        Vector3 distance = nextPos - myPos;
        string wallName = "Cube";


        hits = Physics.RaycastAll(myPos, distance.normalized , distance.magnitude);
        if(hits.Length > 0){
            foreach (RaycastHit hit in hits){
                if(hit.transform.name == wallName){
                    return true;
                }
            }
        }
        return false;
    }    

    public bool CanVehiclesCollide(Vector3 position, Vector3 velocity, Vector3 positionOther, Vector3 velocityOther){
        Vector2 l1_p1 = new Vector2(position.x, position.z);
        Vector2 l1_p2 = new Vector2(position.x + velocity.x, position.z + velocity.z);
        Vector2 l2_p1 = new Vector2(positionOther.x, positionOther.z);
        Vector2 l2_p2 = new Vector2(positionOther.x + velocityOther.x, positionOther.z + velocityOther.z);
        return AreLinesIntersecting(l1_p1, l1_p2, l2_p1, l2_p2, true);

    }

        


    public bool AreLinesIntersecting(Vector2 l1_p1, Vector2 l1_p2, Vector2 l2_p1, Vector2 l2_p2, bool shouldIncludeEndPoints)
    {


        //To avoid floating point precision issues we can add a small value
        float epsilon = 0.00001f;

        bool isIntersecting = false;

        float denominator = (l2_p2.y - l2_p1.y) * (l1_p2.x - l1_p1.x) - (l2_p2.x - l2_p1.x) * (l1_p2.y - l1_p1.y);

        //Make sure the denominator is > 0, if not the lines are parallel
        if (denominator != 0f)
        {
            float u_a = ((l2_p2.x - l2_p1.x) * (l1_p1.y - l2_p1.y) - (l2_p2.y - l2_p1.y) * (l1_p1.x - l2_p1.x)) / denominator;
            float u_b = ((l1_p2.x - l1_p1.x) * (l1_p1.y - l2_p1.y) - (l1_p2.y - l1_p1.y) * (l1_p1.x - l2_p1.x)) / denominator;

            //Are the line segments intersecting if the end points are the same
            if (shouldIncludeEndPoints)
            {
                //Is intersecting if u_a and u_b are between 0 and 1 or exactly 0 or 1
                if (u_a >= 0f + epsilon && u_a <= 1f - epsilon && u_b >= 0f + epsilon && u_b <= 1f - epsilon)
                {
                    isIntersecting = true;
                }
            }
            else
            {
                //Is intersecting if u_a and u_b are between 0 and 1
                if (u_a > 0f + epsilon && u_a < 1f - epsilon && u_b > 0f + epsilon && u_b < 1f - epsilon)
                {
                    isIntersecting = true;
                }
            }
        }

        return isIntersecting;
    }

}
