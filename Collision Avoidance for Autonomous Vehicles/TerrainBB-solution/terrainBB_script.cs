using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

        

public static class TerrainBB_script {

    // shared global variables
    public static List<GameObject> drones;
    public static float thresholdPriorityRadius = float.MaxValue;
    public static bool dronesFound = false;

    public static List<GameObject> getDrones() {
        return drones;
    }

    public static void FindDrones() {

        if (dronesFound == false) {
            drones = GameObject.FindGameObjectsWithTag("Drone").ToList();
            for (int i = 0; i < drones.Count; i++) {
                drones[i].name = "" + i;
            }
            dronesFound = true;

        }
        
        //if (!foundDrones) {
        //    // find drone objects
        //    drones = GameObject.FindGameObjectsWithTag("Drone").ToList();
        //    foundDrones = true;
        //}
    }


    public static List<GameObject> GetCloseDrones (GameObject drone) {
        List<GameObject> closeBuddies = new List<GameObject>();

        foreach (GameObject d in drones) {
            float distance = Vector3.Distance(drone.transform.position, d.transform.position);
            if (distance < thresholdPriorityRadius) {
                closeBuddies.Add(d);
            }
        }
        return closeBuddies;

    }



    // returns true if some Higher|lower is in range
    public static  bool LowerDronesInRange(GameObject drone) {
        Vector3 currentDronePos = drone.transform.position;
        List<Vector3> lowerDronePos = GetLowerPOS(drone);

        // exists lower priority Drones
        if (lowerDronePos != null) {
            foreach (Vector3 pos in lowerDronePos) {
                float distance = Vector3.Distance(currentDronePos, pos);
                if (distance < thresholdPriorityRadius) {
                    return true;

                }
            }
            return false;
        }

        // do not exists higher priority Drones
        else {
            return false;
        }
    }
    public static bool HigherDronesInRange(GameObject drone) {
        Vector3 currentDronePos = drone.transform.position;
        List<Vector3> higherPriorityPOS = GetHigherPOS(drone);
        // exists higher priority Drones
        if (higherPriorityPOS != null) {
            foreach (Vector3 pos in higherPriorityPOS) {
                float distance = Vector3.Distance(currentDronePos, pos);
                if (distance < thresholdPriorityRadius) {
                    return true;

                }
            }
            return false;
        }

        // do not exists higher priority Drones
        else {
            return false;
        }


    }

    // GET 
    public static List<GameObject> GetLowerID(GameObject drone) {
        int index = drones.IndexOf(drone);
        return drones.GetRange(0, index).ToList();
    }
    public static List<Vector3> GetLowerPOS(GameObject drone) {
        int index = drones.IndexOf(drone);
        return drones.GetRange(0, index).Select(gameObject => gameObject.transform.position).ToList();
    }
    private static List<GameObject> GetHigherID(GameObject drone) {
        int index = drones.IndexOf(drone);
        //Debug.Log("drones length :" + drones.Count);
        //Debug.Log("drones index :" + index);
        int howMany = drones.Count - index -1;
        return drones.GetRange(index + 1, howMany).ToList();
    }

    private static List<Vector3> GetHigherPOS(GameObject drone) {
        int index = drones.IndexOf(drone);
        //Debug.Log("drones length :" + drones.Count);
        //Debug.Log("drones index :" + index);
        int howMany = drones.Count - index - 1;
        return drones.GetRange(index + 1, howMany).Select(gameObject => gameObject.transform.position).ToList();
    }
}
