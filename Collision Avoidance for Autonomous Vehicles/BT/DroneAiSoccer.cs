using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;



[RequireComponent(typeof(DroneController))]
public class DroneAISoccer_blueaa : MonoBehaviour
{
    private DroneController m_Drone; // the drone controller we want to use

    public GameObject terrain_manager_game_object;
    TerrainManager terrain_manager;

    public GameObject[] friends;
    public string friend_tag;
    public GameObject[] enemies;
    public string enemy_tag;

    public GameObject own_goal;
    public GameObject other_goal;
    public GameObject ball;

    public float dist;
    public float maxKickSpeed = 40f;
    public float lastKickTime = 0f;

    private void Start()
    {
        // get the car controller
        m_Drone = GetComponent<DroneController>();
        terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();


        // note that both arrays will have holes when objects are destroyed
        // but for initial planning they should work
        friend_tag = gameObject.tag;
        if (friend_tag == "Blue")
            enemy_tag = "Red";
        else
            enemy_tag = "Blue";

        friends = GameObject.FindGameObjectsWithTag(friend_tag);
        enemies = GameObject.FindGameObjectsWithTag(enemy_tag);

        ball = GameObject.FindGameObjectWithTag("Ball");


        // Plan your path here
        // ...
    }


    public bool CanKick()
    {
        if(ball == null){
            return false;
        }

        dist = (transform.position - ball.transform.position).magnitude;
        return dist < 7f && (Time.time - lastKickTime) > 0.5f;
    }

    public void KickBall(Vector3 velocity)
    {
        // impulse to ball object in direction away from agent
        if (CanKick())
        {
            velocity.y = 0f;
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            rb.AddForce(velocity, ForceMode.VelocityChange);
            lastKickTime = Time.time;
            print("ball was kicked ");

        }

    }





    private void FixedUpdate()
    {

        Debug.DrawLine(transform.position, ball.transform.position, Color.black);
        Debug.DrawLine(transform.position, own_goal.transform.position, Color.green);
        Debug.DrawLine(transform.position, other_goal.transform.position, Color.yellow);
        Debug.DrawLine(transform.position, friends[0].transform.position, Color.cyan);
        Debug.DrawLine(transform.position, enemies[0].transform.position, Color.magenta);

        if (CanKick())
        {
            Debug.DrawLine(transform.position, ball.transform.position, Color.red);
        }

    }

    public void Move(Vector3 direction){
        m_Drone.Move_vect(direction);
    }
}

