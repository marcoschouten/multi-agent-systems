using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace UnityStandardAssets.Vehicles.Car
{
    [RequireComponent(typeof(CarController))]
    public class CarAI3 : MonoBehaviour
    {
        private CarController m_Car; // the car controller we want to use

        public GameObject terrain_manager_game_object;
        TerrainManager terrain_manager;

        public GameObject[] friends;
        public GameObject[] enemies;

        public GameObject firstCar;

        public Hashtable sharedResults;

        [SerializeField] string carIDString;

        Vector3 my_target;
        public Vector3 target_velocity;
        Vector3 old_target_pos;
        Vector3 desired_velocity;

        public float k_p = 2f;
        public float k_d = 0.9f;

        Rigidbody my_rigidbody;

        Vector3 stuckPosition;
        int t_being_stuck_treshold = 10;
        int counter_being_stuck;
        int stuck_timer = 0;
        Vector3 saved;
        Vector3 NEWsaved;


        // marco
        public List<Vector3> path;
        public int currentNode;
        // marco end


        // // stuck and reverse parameters
        float stuck_time;
        public int stuck = 0;
        int stuck_counter = 0;
        int reverse_threshold = 1;  // reverse after being stuck for this times
        int reverse_time = 4;     // reverse duration (second)

        private void Start()
        {
            //-------------------------------------------------- initialise ----------------
            // get the car controller
            m_Car = GetComponent<CarController>();
            terrain_manager = terrain_manager_game_object.GetComponent<TerrainManager>();
            my_rigidbody = GetComponent<Rigidbody>();
            enemies = GameObject.FindGameObjectsWithTag("Enemy");
            friends = GameObject.FindGameObjectsWithTag("Player");

            // note that both arrays will have holes when objects are destroyed
            // but for initial planning they should work
            friends = GameObject.FindGameObjectsWithTag("Player");
            // Note that you are not allowed to check the positions of the turrets in this problem

            int num_cars = 3;
            float firingRange = 10.0f;
            float car_length = 4.22f;

            Graph myGraph = new Graph(terrain_manager, firingRange, car_length, num_cars, transform.position);
            Hashtable vertices = myGraph.getVertices();
            (_, int root) = myGraph.getRootVertex();





            //-------------------------------------------------- build map ----------------

            this.sharedResults = p3_solution.getResult(terrain_manager, myGraph, root);



            //-------------------------------------------------- read path ----------------
            List<int> pathCar1 = (List<int>)sharedResults[carIDString];

            Color myC = Color.white;
            if (carIDString == "Car1")
            {
                myC = Color.green;
            }
            if (carIDString == "Car2")
            {
                myC = Color.blue;
            }
            if (carIDString == "Car3")
            {
                myC = Color.yellow;
            }


            // print path "Debug"
            int currentID = 0;
            int precedentID = pathCar1[0];
            for (int j = 1; j < pathCar1.Count; j++)
            {
                currentID = pathCar1[j];
                Vector3 u = (Vector3)vertices[precedentID];
                Vector3 v = (Vector3)vertices[currentID];
                Debug.DrawLine(u, v, myC, 100f);
                precedentID = currentID;
            }

            // convert ID-->Vector3
            Vector3 rootV3 = (Vector3)vertices[root];
            path.Add(rootV3);
            if (carIDString == "Car2")
            {
                path.Add(new Vector3(rootV3.x, 0.0f, rootV3.z + 5.0f));
            }
            if (carIDString == "Car3")
            {
                path.Add(new Vector3(rootV3.x, 0.0f, rootV3.z + 10.0f));
            }
            for (int j = 0; j < pathCar1.Count; j++)
            {
                currentID = pathCar1[j];
                Vector3 v = (Vector3)vertices[currentID];
                path.Add(v);
            }
            currentNode = 0;
            old_target_pos = transform.position; // init with car position



        }


        private void FixedUpdate()
        {
            if (HavePrecedence(carIDString) == true)
            {
                UpdateTarget();
                // keep track of target position and velocity
                // Vector3 target_position = my_target.transform.position;
                Vector3 target_position = my_target;
                target_velocity = (target_position - old_target_pos) / (Time.fixedDeltaTime * 1000);
                //old_target_pos = target_position; // marco commented - - -

                // a PD-controller to get desired velocity
                Vector3 position_error = target_position - transform.position;
                Vector3 velocity_error = target_velocity - my_rigidbody.velocity;
                Vector3 desired_acceleration = k_p * position_error + k_d * velocity_error;

                float steering = Vector3.Dot(desired_acceleration, transform.right);
                float acceleration = Vector3.Dot(desired_acceleration, transform.forward);

                Debug.DrawLine(target_position, target_position + target_velocity, Color.red);
                Debug.DrawLine(transform.position, transform.position + my_rigidbody.velocity, Color.blue);
                Debug.DrawLine(transform.position, transform.position + desired_acceleration, Color.black);

                // this is how you control the car
                //Debug.Log("Steering:" + steering + " Acceleration:" + acceleration);
                m_Car.Move(steering, acceleration, acceleration, 0f);
            }
            else // wait
            {
                float steering = 0.0f;
                float acceleration = 0.0f;
                m_Car.Move(steering, acceleration, acceleration, 0f);
            }
           
        }

        private bool HavePrecedence(string carIDString)
        {
            // car1 > car2 > car 3
            if (carIDString == "Car1")
            {
                return true;
            }
            if (carIDString == "Car2") 
            {
                if (Vector3.Distance(friends[0].transform.position, friends[1].transform.position) < 5.0f)
                {
                    return false;
                }
                return true;

            }
            if (carIDString == "Car3")
            {
                if (Vector3.Distance(friends[0].transform.position, friends[2].transform.position) < 5.0f)
                {
                    return false;
                }
                if (Vector3.Distance(friends[1].transform.position, friends[2].transform.position) < 5.0f)
                {
                    return false;
                }
                return true;


            }
            return true;
        }



        // marco - - -
        // ctrl -k -c to comment
        // ctrl - k,  ctrl-d to clean and beautify
        private void UpdateTarget()
        {
            // return true if changes current node
            if (Vector3.Distance(transform.position, path[currentNode]) < 6.0f)
            { // compare current car position with the current target Node
                old_target_pos = path[currentNode]; // if I am  close enough to a goal node, then I progress to the next
                if (currentNode == (path.Count - 1))
                {
                    // do nothing
                }
                else
                {
                    currentNode++;
                }
            }
            my_target = path[currentNode];
        }
        // marco -end - - -
    }
}
