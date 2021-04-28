using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityStandardAssets.Vehicles.Car;
using System.Linq;


class PriorityQueue : MonoBehaviour {

    public List<DroneAI> drones; 
    private bool foundDrones = false;

    public float DroneDistanceTolerance = 15f;
    public float DirectionThreshold = 0.95f;


    private void FixedUpdate(){
        if(!foundDrones){
            drones = GameObject.FindGameObjectsWithTag("Drone").Select( gameObject => gameObject.GetComponent<DroneAI>()).ToList();
            foundDrones = true;
            setuphigherPriorityDrones();
        }
    }

    private void setuphigherPriorityDrones(){
            
            for (int i = 0; i < drones.Count; i++){
                drones[i].AllDrones = drones;
                drones[i].gameObject.name = "Drone " + i; 
            }
            
            List<DroneAI> higherPriorityDrones = new List<DroneAI>();
            for (int i = 0; i < drones.Count; i++){
                List<DroneAI> higherPriorityDronesCopy = new List<DroneAI>();
                higherPriorityDronesCopy.AddRange(higherPriorityDrones);
                drones[i].HigherPriorityDrones = higherPriorityDronesCopy;
                drones[i].DroneDistanceTolerance = DroneDistanceTolerance;
                drones[i].DirectionThreshold = DirectionThreshold;
                higherPriorityDrones.Add(drones[i]);
            }


    }

}