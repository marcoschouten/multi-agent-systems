using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(DroneController))]
public class DroneAI : MonoBehaviour {

    private DroneController m_Drone; // the car controller we want to use
    public GameObject my_goal_object; // stores a Sphere. it's "transform" contains the Vec3 coordinates of the goal
    public GameObject terrain_manager_game_object;
    TerrainManager terrain_manager;


    // shared global variables --Marco
    public List<GameObject> drones;
    int counter = 0;
    public Vector3 toGo = new Vector3(0.0f, 0.0f, 0.0f);


    public GameObject minDrone;

  

    private void Start() {
        // get the drone controller
        m_Drone = GetComponent<DroneController>();
        terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
        TerrainBB_script.FindDrones();

        minDrone = new GameObject();

        //// draw circle
        //line = gameObject.GetComponent<LineRenderer>();
        //line.SetVertexCount(segments + 1);
        //line.useWorldSpace = false;
        //CreatePoints();
    }



    // called 100 times every second, (once every 0.01 seconds)
    //"ideally" updates the acceleration every 0.5 seconds.
    private void FixedUpdate() {        
        counter++;
        if (counter > 10) {// 0.0f,  0.010 secons we update the acceleration
            // normalize the step:
            Vector3 relVect = my_goal_object.transform.position - transform.position;
            toGo = ComputeNextMovementALWAYSDODGE(relVect);
            
            // option 1: no obstacles -> return relVect
            // option 2: you are a low priority drone: you want to wait your turn --> make it stop and wait. hold its position
            // 
            counter = 0;
        }

        //Debug.Log("toGo magnitude: " + toGo.magnitude);
        Rigidbody my_rb = GetComponent<Rigidbody>();
        Vector3 myVelocity = my_rb.velocity;
        //

        if (myVelocity.magnitude > 10.0f) {
            m_Drone.Move_vect(-myVelocity);

        } else {
            m_Drone.Move_vect(toGo);
        }

        Debug.DrawLine(transform.position, transform.position + toGo, Color.black, 0.5f); // closest is Green



    }

    //private Vector3 ComputeNextMovementNOPRIOR(Vector3 relVect) {
    //    // option B find closest
    //    GameObject minDrone = new GameObject();
    //    float minDist = float.MaxValue;
    //    List<GameObject> pippo = TerrainBB_script.GetCloseDrones(gameObject);

    //    if (pippo != null) {
    //        foreach (GameObject d in pippo) {
    //            float dist = Vector3.Distance(gameObject.transform.position, d.transform.position);
    //            if (dist < minDist) {
    //                minDist = dist;
    //                minDrone = d;
    //            }
    //        }
    //        Vector3 turnVect = ComputeTurningAcc(minDrone);

    //        return turnVect;

    //    }
    //    else {
    //        return relVect;
    //    }
    //}

    private Vector3 ComputeNextMovementALWAYSDODGE(Vector3 relVect) {

        // find the cloest Low priority Drone (expect itself)
        List<GameObject> allDrones = TerrainBB_script.getDrones();
        float minDist = float.MaxValue;
        foreach (GameObject d in allDrones) {
            if (gameObject != d) {
                //Debug.Log("AAA");


                float dist = (gameObject.transform.position - d.transform.position).magnitude;
                if (dist < minDist) {
                    minDist = dist;
                    minDrone = d;
                }
            }
        }


        // check range, if < collisionRange, radial-Acc
        if (minDist < 10.0f) {
            // rotate
            return new Vector3(relVect.z, 0.0f, - relVect.x);

        }
        else {
            // straight
            return relVect;
        }

        


        //// compute Acceleration= turning + moving towards the goal
        //Vector3 turnVect = ComputeTurningAcc(minDrone);
        //Debug.Log("turnVect= " + turnVect);

        //Debug.DrawLine(transform.position, minDrone.transform.position, Color.green, 0.5f); // closest is Green
        //return turnVect + relVect*0.001f;
    }




        private Vector3 ComputeNextMovement(Vector3 relVect) {

        

        //if (TerrainBB_script.HigherDronesInRange(gameObject)) {
        //    Debug.Log("wait");
        //    // wait
        //    // make it freeze
        //    // return -relVect.normalized * 0.1f;
        //    return -relVect * 0.001f;  

        //    //Rigidbody my_rb = GetComponent<Rigidbody>();
        //    //Vector3 myVelocity = my_rb.velocity;

        //    // return - myVelocity;
        //}

        if (TerrainBB_script.LowerDronesInRange(gameObject)) {
            // dodge
            Debug.Log("dodge");
            List<GameObject> lowerDrones = TerrainBB_script.GetLowerID(gameObject);

            //// option A weighted sum 
            //Vector3 turnVect = new Vector3(0.0f, 0.0f, 0.0f);
            //foreach (GameObject d in lowerDrones) {
            //    turnVect += ComputeTurningAcc(d); // turnVect it's orthogoal to relVect
            //}
            //turnVect = turnVect / lowerDrones.Count;

            // option B find closest
           
            float minDist = float.MaxValue;
            foreach (GameObject d in lowerDrones) {
                float dist = (gameObject.transform.position -  d.transform.position).magnitude;
                if (dist < minDist) {
                    minDist = dist;
                    minDrone = d;
                }     
            }


            //float minVelocity = 2.0f;
            Vector3 turnVect = ComputeTurningAcc(minDrone) ;
            //turnVect = Mathf.Max(minVelocity, turnVect.magnitude) * turnVect.normalized;

            Debug.DrawLine(transform.position, minDrone.transform.position, Color.green, 0.5f); // closest is Green

            return (turnVect) + (relVect * 0.1f);
        }

        else {
            Debug.Log("nobody");
            // move straight
            return relVect * 0.1f;
        }
    }

    // https://en.wikipedia.org/wiki/Proportional_navigation
    private Vector3 ComputeTurningAcc(GameObject closestDrone) {
        // get target Data
        GameObject targetDrone = closestDrone;
        Rigidbody t_rb = targetDrone.GetComponent<Rigidbody>();
        Vector3 targetVelocity = t_rb.velocity;
        Vector3 targetPosition = targetDrone.transform.position;

        // get current Data
        // GameObject myDrone = this.drone;
        Rigidbody my_rb = GetComponent<Rigidbody>();
        Vector3 myVelocity = my_rb.velocity;
        Vector3 myPosition = transform.position;


        // formula
        Vector3 Vr = targetVelocity - myVelocity;
        Vector3 R = targetPosition - myPosition;

        Vector3 temp1  = Vector3.Cross(R, Vr);
        float temp2 = Vector3.Dot(R, R);
        Vector3 Omega = temp1 / temp2;

        // constant N
        float N = 1.0f;
        Vector3 acc = N * Vector3.Cross(Vr, Omega);

        //if (Vector3.Dot(acc, myVelocity) < 0.1f) {
        //    Debug.Log("orthogonal");
        //}

        Debug.DrawLine(transform.position, transform.position + acc, Color.white, 0.5f);  // white = new acc
        Debug.DrawLine(transform.position, transform.position + myVelocity, Color.red, 0.5f); // red = current Velocity


        return acc ;
    }


    
   

}
