using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Panda;


public enum PlayerRole { Goalie, Playmaker, Attacker, Defender };
public enum Strategy { Unset, Defensive, Neutral, Offensive };


[RequireComponent(typeof(DroneController))]
public class DroneAISoccer_blue : MonoBehaviour
{

    public static bool dynamicChanging = true;

    //Ensure that only one of the drones is a manager
    public static bool ManagerSet = false;

    public bool isManager = false;

    private Strategy currentStrategy = Strategy.Unset;

    private DroneController m_Drone; // the drone controller we want to use

    public GameObject terrain_manager_game_object;
    private float centerX = 150f;

    public GameObject[] friends;

    public List<DroneAISoccer_blue> friendDrones;
    public string friend_tag;
    public GameObject[] enemies;
    public string enemy_tag;

    public GameObject own_goal;
    public GameObject other_goal;
    public GameObject ball;

    private Rigidbody rigidbody;
    private Rigidbody ballRigidbody;

    public float dist;
    public float maxKickSpeed = 40f;
    public float lastKickTime = 0f;

    public float isCloseToOwnGoalThreshold = 20f;
    public float isCloseToEnemyGoalThreshold = 35f;

    public float HasBallThreshold = 15f;

    public float enemiesPressingThreshold = 15f;

    public float reachedGoalThreshold = 20f;
    public float reachedBallThreshold = 4f;






    public float k_p = 1f;
    public float k_d = 0.2f;


    public PlayerRole PlayerRole = PlayerRole.Attacker;

    private PandaBehaviour panda;


    [Task]
    bool IsRole(PlayerRole role)
    {
        return PlayerRole == role;
    }



    private void Start()
    {
        if (!ManagerSet)
        {
            ManagerSet = true;
            isManager = true;
        }

        // get the car controller
        m_Drone = GetComponent<DroneController>();
        panda = GetComponent<PandaBehaviour>();
        rigidbody = GetComponent<Rigidbody>();

        // note that both arrays will have holes when objects are destroyed
        // but for initial planning they should work
        friend_tag = gameObject.tag;
        if (friend_tag == "Blue")
            enemy_tag = "Red";
        else
            enemy_tag = "Blue";

        friends = GameObject.FindGameObjectsWithTag(friend_tag);
        friendDrones = friends.Select(
                gameObject => gameObject.GetComponent<DroneAISoccer_blue>()
            ).ToList();
        enemies = GameObject.FindGameObjectsWithTag(enemy_tag);

        ball = GameObject.FindGameObjectWithTag("Ball");
        ballRigidbody = ball.GetComponent<Rigidbody>();


        // Plan your path here
        // ...
    }


    [Task]
    public bool CanKick()
    {
        if (ball == null)
        {
            return false;
        }

        dist = (transform.position - ball.transform.position).magnitude;
        return dist < 7f && (Time.time - lastKickTime) > 0.5f;
    }


    [Task]
    void KickToScoreCopyPaste()
    {
        // find closest enemy to goal
        GameObject enemyGK = null;
        float minDist = float.MaxValue;
        foreach (GameObject enemy in enemies)
        {
            // drone
            Vector3 enemyPos = enemy.transform.position;
            float dist = Vector3.Distance(enemyPos, other_goal.transform.position);
            if (minDist > dist)
            {
                minDist = dist;
                enemyGK = enemy;
            }
        }

        //shoot up
        float aim;
        if (enemyGK.transform.position.z > other_goal.transform.position.z)
        {
            aim = -10.0f;
        }
        else
        {
            aim = +10.0f;
        }



        // vectors
        Vector3 O = ball.transform.position;
        Vector3 CO = other_goal.transform.position - O;
        CO.z += aim;
        Vector3 AO = ball.GetComponent<Rigidbody>().velocity;

        // magnitudes
        float B = maxKickSpeed;
        AO.y = 0; float A = AO.magnitude;


        // (1)
        float cosTheta = (Vector3.Dot(AO, CO)) / (AO.magnitude * CO.magnitude);
        // 

        // (2)
        float theta = Mathf.Acos(cosTheta);

        //float theta = Vector3.Angle(AO, CO); // degrees
        //theta = Mathf.Deg2Rad(theta);

        // (3)
        float temp = A * Mathf.Sin(theta);
        float fi = Mathf.Asin(temp / B);

        // (4)
        float x1 = B * Mathf.Cos(fi); // parallel component
        float x2 = B * Mathf.Sin(fi); // perpendicular component

        // (5)
        // find alpha, angle between CO and X axis
        Vector3 xAxis = Vector3.right;
        float cosAlpha = (Vector3.Dot(xAxis, CO)) / (CO.magnitude * xAxis.magnitude);
        float alpha = Mathf.Acos(cosAlpha);

        //float alpha = Vector3.Angle(xAxis, CO); // degrees


        // Vector3.Angle(xAxis, CO);    = X
        // Vector3.Angle(CO, xAxis);    = 180 - X

        // project to cardinal axis.
        float x = x1 * Mathf.Cos(alpha) - x2 * Mathf.Sin(alpha);
        float z = x2 * Mathf.Cos(alpha) + x1 * Mathf.Sin(alpha);




        // kick the ball
        Vector3 inputKick = new Vector3(x, 0.0f, z);

        Debug.DrawLine(O, O + inputKick, Color.red, 1f); // input kick
        Debug.DrawLine(O, O + AO, Color.blue, 1f); // velocity    
        Debug.DrawLine(O, O + CO, Color.green, 1f); // resultant


        KickBall(inputKick);




        if (!CanKick())
        {
            Task.current.Succeed();
        }
    }




    [Task]
    void KickToScore()
    {
        // find closest enemy to goal
        GameObject enemyGK = null;
        float minDist = float.MaxValue;
        foreach (GameObject enemy in enemies)
        {
            // drone
            Vector3 enemyPos = enemy.transform.position;
            float dist = Vector3.Distance(enemyPos, other_goal.transform.position);
            if (minDist > dist)
            {
                minDist = dist;
                enemyGK = enemy;
            }
        }

        //shoot up
        float aim;
        if (enemyGK.transform.position.z > other_goal.transform.position.z)
        {
            aim = -10.0f;
        }
        else
        {
            aim = +10.0f;
        }

        Vector3 targetPosition = other_goal.transform.position;
        targetPosition.z += aim;

        KickToPosition(targetPosition);

        if (!CanKick())
        {
            Task.current.Succeed();
        }
    }

    private void KickToPosition(Vector3 targetPosition)
    {
        // vectors
        Vector3 O = ball.transform.position;
        Vector3 CO = targetPosition - O;
        Vector3 AO = ball.GetComponent<Rigidbody>().velocity;


        // magnitudes
        O.y = 0;
        CO.y = 0;
        AO.y = 0;
        float B = maxKickSpeed;
        float A = AO.magnitude;

        if (AO.magnitude < 10)
        {
            Vector3 inputKick = CO.normalized * B;
            KickBall(inputKick);
        }
        else
        {
            float thetaDeg = Vector3.SignedAngle(CO, AO, Vector3.up);
            float alphaDeg = Vector3.SignedAngle(CO, Vector3.right, Vector3.up);
            Debug.Log("theta deg: " + thetaDeg);

            // (2) get fi
            float thetaRad = thetaDeg * Mathf.Deg2Rad; // rad
            float fiRad = Mathf.Asin((A * Mathf.Sin(thetaRad)) / B); // rad
            float fiDeg = fiRad * Mathf.Rad2Deg;  // deg
            Debug.Log("fi deg: " + fiDeg);
            Debug.Log("fi rad: " + fiRad);

            // (3) get Vector with CO as basis
            float x1 = B * Mathf.Cos(fiRad); // parallel component
            float x2 = B * Mathf.Sin(fiRad); // perpendicular component

            // (4) ALPHA    CO--Alpha
            Vector3 xAxis = Vector3.right;

            Debug.Log("alpha deg: " + alphaDeg);
            // project to cardinal axis.
            float alphaRad = alphaDeg * Mathf.Deg2Rad; // rad
            float x = x1 * Mathf.Cos(alphaRad) - x2 * Mathf.Sin(alphaRad);
            float z = x2 * Mathf.Cos(alphaRad) + x1 * Mathf.Sin(alphaRad);

            // kick the ball
            Vector3 BO = new Vector3(x, 0.0f, z);
            KickBall(BO);

            // debug
            Debug.DrawLine(O, O + xAxis, Color.grey, 1f); // input kick
            Debug.DrawLine(O, O + BO, Color.red, 1f); // input kick
            Debug.DrawLine(O, O + AO, Color.blue, 1f); // velocity    
            Debug.DrawLine(O, O + AO + BO, Color.black, 1f); // resultant        
            Debug.DrawLine(O, O + CO, Color.green, 1f); // resultant
            float inBetween = Vector3.Angle(CO, BO);
            Debug.Log("inBetween: " + inBetween);

        }
    }

    float angle360(Vector3 from, Vector3 to, Vector3 right)
    {
        float angle = Vector3.Angle(from, to);
        return (Vector3.Angle(right, to) > 90f) ? 360f - angle : angle;
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
            print("ball was kicked with power " + velocity.magnitude);

        }
    }


    [Task]
    void GoToBall()
    {
        Vector3 ballPosition = ball.transform.position;
        Vector3 ballVelocity = ballRigidbody.velocity;

        MovePID(ballPosition, ballVelocity);


        if (CanKick())
        {
            Task.current.Succeed();
        }
    }


    private void MovePID(Vector3 targetPosition, Vector3 targetVelocity)
    {
        Vector3 dronePosition = transform.position;
        Vector3 positionError = targetPosition - dronePosition;
        positionError.y = 0;

        Vector3 droneVelocity = rigidbody.velocity;
        Vector3 velocityError = targetVelocity - droneVelocity;
        velocityError.y = 0;

        Move(k_p * positionError + k_d * velocityError);
    }




    public void Move(Vector3 direction)
    {
        m_Drone.Move_vect(direction);
    }


    public void MoveTo(Vector3 position)
    {
        m_Drone.Move_vect(position - transform.position);
    }

    [Task]
    public bool IsCloseToOwnGoal()
    {
        float goalDistance = (own_goal.transform.position - transform.position).magnitude;
        return goalDistance <= isCloseToOwnGoalThreshold;
    }

    [Task]
    void MoveToOwnGoal()
    {
        MoveTo(own_goal.transform.position);
        if (IsCloseToOwnGoal())
        {
            Task.current.Succeed();
        }
    }

    [Task]
    void GuardGoal()
    {
        float goalDistance = 15f;
        Vector3 dronePosition = ball.transform.position;
        Vector3 goalPosition = own_goal.transform.position;
        Vector3 goalToDrone = dronePosition - goalPosition;
        Vector3 targetPosition = goalPosition + goalDistance * goalToDrone.normalized;

        Move((targetPosition - transform.position).normalized);
        Task.current.Succeed();
    }

    [Task]
    public bool IsCloseToEnemyGoal()
    {
        float goalDistance = (other_goal.transform.position - transform.position).magnitude;
        return goalDistance <= isCloseToEnemyGoalThreshold;
    }

    [Task]
    public void PassClosest()
    {
        GameObject closestFriend = getClosestFriend();
        KickBall(maxKickSpeed * (closestFriend.transform.position - transform.position).normalized);

        if (!CanKick())
        {
            Task.current.Succeed();
        }
    }


    [Task]
    public void PassClosestToEnemyGoal()
    {
        GameObject friend = getFriendClosestToEnemyGoal();
        if (friend != null)
        {
            KickToPosition(friend.transform.position);
            //KickBall(maxKickSpeed * (friend.transform.position - transform.position).normalized);
        }
        if (!CanKick())
        {
            Task.current.Succeed();
        }
    }

    //Works the same as AllyCloserToGoal
    [Task]
    public bool HasFriendCloserToEnemyGoal()
    {
        return getFriendClosestToEnemyGoal() != null;
    }

    public GameObject getClosestFriend()
    {
        return getFriendClosestToPosition(transform.position, false);
    }



    private GameObject getFriendClosestToEnemyGoal()
    {
        return getFriendClosestToPosition(other_goal.transform.position, true);
    }

    private GameObject getFriendClosestToBall(bool requireCloserThanMe)
    {
        return getFriendClosestToPosition(ball.transform.position, requireCloserThanMe);
    }

    private GameObject getFriendClosestToPosition(Vector3 position, bool requireCloserThanMe)
    {
        if (friends == null || friends.Length <= 1)
        {
            return null;
        }

        float minDistance = float.MaxValue;
        if (requireCloserThanMe)
        {
            minDistance = (transform.position - position).magnitude;
        }
        GameObject closestFriend = null;
        foreach (GameObject friend in friends)
        {
            if (friend != this)
            {
                float distance = (friend.transform.position - position).magnitude;
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestFriend = friend;
                }
            }
        }
        return closestFriend;
    }


    [Task]
    public bool IsFriendsCloserToBallThanEnemies(bool requireCloserThanMe)
    {
        GameObject closestFriend = getFriendClosestToBall(requireCloserThanMe);
        if (closestFriend == null)
        {
            return false;
        }

        float friendDistanceToBall = (closestFriend.transform.position - ball.transform.position).magnitude;
        foreach (GameObject enemy in enemies)
        {
            float enemyDistanceToBall = (closestFriend.transform.position - ball.transform.position).magnitude;
            if (enemyDistanceToBall < friendDistanceToBall)
            {
                return false;
            }
        }
        return true;
    }

    [Task]
    public bool IsLastGuard()
    {
        Vector3 myPosition = transform.position;
        foreach (GameObject enemy in enemies)
        {
            // emeny drone
            Vector3 dronePosition = enemy.transform.position;
            // condition: my X < enemies.X
            if (myPosition.x > dronePosition.x)
            {
                return false;
            }
        }
        return true;
    }


    [Task]
    bool HasBall()
    {
        return DroneHasBall(transform.position, rigidbody.velocity);
    }

    //Same as AllyHasBall
    [Task]
    bool FriendHasBall()
    {
        foreach (GameObject friend in friends)
        {
            // drone
            Vector3 friendPosition = friend.transform.position;
            Rigidbody friendRB = GetComponent<Rigidbody>();
            Vector3 friendVelocity = friendRB.velocity;

            if (DroneHasBall(friendPosition, friendVelocity))
            {
                return true;
            }
        }
        return false;
    }

    [Task]
    bool EnemyHasBall()
    {
        foreach (GameObject enemy in enemies)
        {
            // drone
            Vector3 enemyPosition = enemy.transform.position;
            Rigidbody enemyRB = GetComponent<Rigidbody>();
            Vector3 enemyVelocity = enemyRB.velocity;

            if (DroneHasBall(enemyPosition, enemyVelocity))
            {
                return true;
            }
        }
        return false;
    }

    private bool DroneHasBall(Vector3 dronePosition, Vector3 droneVelocity)
    {

        Vector3 ballPosition = ball.transform.position;
        Vector3 ballVelocity = ballRigidbody.velocity;

        // condition: being close to the ball, and going toward the same direction
        bool inRange = Vector3.Distance(dronePosition, ballPosition) < HasBallThreshold;
        bool inSameDirection = Vector3.Dot(droneVelocity, ballVelocity) > 0.0f;
        return inRange && inSameDirection;

    }



    [Task]
    bool CanPassClosestSafely()
    {
        GameObject closestFriend = getClosestFriend();
        if (closestFriend == null)
        {
            return false;
        }

        Vector3 receiverPos = closestFriend.transform.position;

        Vector3 myPosition = transform.position;
        foreach (GameObject friend in friends)
        {
            // friend drone
            Vector3 dronePosition = friend.transform.position;

            // condition: friend must not be able to intercept the ball
            Vector3 receiverRel = receiverPos - myPosition;
            Vector3 enemyRel = dronePosition - myPosition;

            if (Vector3.Cross(receiverRel, enemyRel).normalized.magnitude < 0.5f)
            {
                return false;
            }
        }
        return true;
    }

    [Task]
    // will move backwards if there is no Last Guard
    void PositionAsLastGuard()
    {
        Vector3 direction = (own_goal.transform.position - transform.position).normalized;
        Move(direction);
        if (IsLastGuard())
        {
            Task.current.Succeed();
        }
    }



    [Task]
    // ADD Z LENGTH (HARDCODED- should be 45!)
    void KickToSide()
    {
        // if you are closer to top border, kick towards top
        // if you are closer to bottom border, kick towards bottom
        float kickPower = maxKickSpeed;
        Vector3 kickDirection;
        Vector3 pos = transform.position;
        float x = other_goal.transform.position.x - own_goal.transform.position.x;
        if (pos.z > 50f)
        {
            kickDirection = new Vector3(transform.position.x + x, 0.0f, transform.position.z + 40.0f).normalized;
        }
        else
        {
            kickDirection = new Vector3(transform.position.x + x, 0.0f, transform.position.z - 40.0f).normalized;
        }

        KickBall(kickPower * kickDirection);
        if (!CanKick())
        {
            Task.current.Succeed();
        }
    }

    [Task]
    // give velocity s.t. finds collision (CONES)
    void Intercept()
    {
        // ball
        Vector3 ballPosition = ball.transform.position;
        Vector3 ballVelocity = ballRigidbody.velocity;

        MovePID(ballPosition, ballVelocity);

        // kick it away if possibile
        if (CanKick())
        {
            float kickPower = maxKickSpeed;
            Vector3 kickDirection = (other_goal.transform.position - transform.position).normalized;
            KickBall(kickPower * kickDirection);
        }
    }



    [Task]
    void Block()
    {
        GameObject closestEnemy = GetClosestEnemy();
        if (closestEnemy != null)
        {
            Vector3 enemyPosition = closestEnemy.transform.position;
            Rigidbody enemyRB = closestEnemy.GetComponent<Rigidbody>();
            Vector3 enemyVelocity = enemyRB.velocity;

            MovePID(enemyPosition, enemyVelocity);
        }
    }

    private GameObject GetClosestEnemy()
    {
        Vector3 myPos = transform.position;
        float minDistance = float.MaxValue;
        GameObject closestEnemy = null;

        // find closest Enemy
        foreach (GameObject enemy in enemies)
        {
            // enemy
            Vector3 enemyPos = enemy.transform.position;

            float distance = Vector3.Distance(myPos, enemyPos);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestEnemy = enemy;
            }
        }
        return closestEnemy;
    }

    [Task]
    bool EnemiesPressing()
    {
        foreach (GameObject enemy in enemies)
        {
            float enemyDistance = Vector3.Distance(enemy.transform.position, transform.position);
            if (enemyDistance < enemiesPressingThreshold)
            {
                return true;
            }
        }
        return false;
    }


    [Task]
    // todo: make it more smart by taking into account the enemy position and angles.
    // FIXME
    void Dribble()
    {
        // ball
        Vector3 ballPosition = ball.transform.position;
        Vector3 ballVelocity = ballRigidbody.velocity;

        // evaluate kick direction X
        float x = other_goal.transform.position.x - own_goal.transform.position.x;

        // evaluate kick direction Z
        if (transform.position.z > 50f)
        {
            float z = transform.position.z + 40.0f;
        }
        else
        {
            float z = transform.position.z - 40.0f;
        }


        // kick
        if (CanKick())
        {
            float kickPower = maxKickSpeed;
            Vector3 kickDirection = (other_goal.transform.position - transform.position).normalized;
            KickBall(kickPower * kickDirection);
        }
    }


    [Task]
    // todo: make it more smart by taking into account the enemy position and angles.
    void ReceiveBall()
    {

        List<Vector3> idealReceivePositions = new List<Vector3>();
        // check if Blue side or Red Side
        float side = other_goal.transform.position.x - own_goal.transform.position.x;
        if (side > 0) // we are blue side
        {
            idealReceivePositions.Add(new Vector3(200, 0, 100));
            idealReceivePositions.Add(new Vector3(215, 0, 120));
            idealReceivePositions.Add(new Vector3(215, 0, 80));

        }
        else // we are red side
        {
            idealReceivePositions.Add(new Vector3(100, 0, 100));
            idealReceivePositions.Add(new Vector3(85, 0, 120));
            idealReceivePositions.Add(new Vector3(85, 0, 80));
        }



        // look for best POSITION


        float maxDistPoint = 0;
        Vector3 maxDistPos = Vector3.zero;

        foreach (Vector3 pos in idealReceivePositions)
        {
            float pointDist = 0.0f;
            foreach (GameObject enemy in enemies)
            {
                float enemyDist = Vector3.Distance(transform.position, pos);
                pointDist += enemyDist;
            }


            if (pointDist > maxDistPoint)
            {
                maxDistPoint = pointDist;
                maxDistPos = pos;
            }
        }
        // move to Best Position

        MoveTo(maxDistPos);



    }



    [Task]
    public void WalkForwardWithBall()
    {

        Vector3 dronePosition = transform.position;
        Vector3 droneVelocity = rigidbody.velocity;

        Vector3 ballPosition = ball.transform.position;
        Vector3 ballVelocity = ballRigidbody.velocity;

        Vector3 targetPosition = other_goal.transform.position;
        Vector3 targetDirection = (targetPosition - ballPosition).normalized;
        Vector3 aroundPosition = ballPosition - 5f * targetDirection;
        Vector3 positionError = aroundPosition - dronePosition;
        positionError.y = 0;


        if (positionError.magnitude >= reachedBallThreshold)
        {
            MovePID(aroundPosition, ballVelocity);
            return;
        }

        Vector3 kickVelocity = Mathf.Min(0.01f, .6f * Vector3.Dot(droneVelocity, targetDirection)) * targetDirection - ballVelocity;

        if (kickVelocity.magnitude >= maxKickSpeed)
        {
            kickVelocity = maxKickSpeed * kickVelocity.normalized;
        }

        KickBall(kickVelocity);

        Move(0.1f * (targetPosition - dronePosition));

        if ((ballPosition - targetPosition).magnitude <= reachedGoalThreshold)
        {
            Task.current.Succeed();
        }
    }

    [Task]
    bool WillReachBallBeforeEnemies(){
        float distanceToOwnGoalX = (ball.transform.position - own_goal.transform.position).x;
        distanceToOwnGoalX = Mathf.Abs(distanceToOwnGoalX);

        if(distanceToOwnGoalX <= 15f || distanceToOwnGoalX >= 35f){
            return false;
        }

        //This does not consider the velocity of the ball yet!
        float goalAccelerationFactor = 0.6f;
        float enemyAccelerationFactor = 0.9f;


        Vector3 dronePosition = transform.position;
        Vector3 ballPosition = ball.transform.position;
        Vector3 ballDirection = ballPosition - dronePosition;
        ballDirection.y = 0f;
        float ballDistance = ballDirection.magnitude;

        Vector3 droneVelocity = GetComponent<Rigidbody>().velocity - ballRigidbody.velocity ;
        droneVelocity.y = 0f;
        float droneVelocityR = Vector3.Dot(droneVelocity, ballDirection.normalized);


        float maxAcceleration = m_Drone.max_acceleration;
        float goalieAcceleration = goalAccelerationFactor * maxAcceleration;
        float enemyAcceleration = enemyAccelerationFactor * maxAcceleration;

        float timeToReachBall = (Mathf.Sqrt(2*ballDistance*goalieAcceleration + droneVelocityR*droneVelocityR)-droneVelocityR)/goalieAcceleration;

        foreach(GameObject enemy in enemies){
            Vector3 enemyPosition = enemy.transform.position;
            Vector3 enemyBallDirection = ballPosition - enemyPosition;
            enemyBallDirection.y = 0f;
            float enemyBallDistance = enemyBallDirection.magnitude;
            float enemyVelocityR = 0f;

            Rigidbody enemyRB = enemy.GetComponent<Rigidbody>();
            if(enemyRB != null){
                Vector3 enemyVelocity = enemyRB.velocity  - ballRigidbody.velocity;
                enemyVelocity.y = 0f;
                enemyVelocityR = Vector3.Dot(enemyVelocity, enemyBallDirection.normalized);
            }
            float enemyTimeToReachBall = (Mathf.Sqrt(2*enemyBallDistance*enemyAcceleration + enemyVelocityR*enemyVelocityR)-enemyVelocityR)/enemyAcceleration;
            if(enemyTimeToReachBall <= timeToReachBall){
                return false;
            }
        }
        return true;
    }

    [Task]
    bool WillReachBallBeforeOthers()
    {
        //This does not consider the velocity of the ball yet!
        float goalAccelerationFactor = 0.6f;
        float enemyAccelerationFactor = 0.9f;
        float friendAccelerationFactor = 0.7f;


        Vector3 dronePosition = transform.position;
        Vector3 ballPosition = ball.transform.position;
        Vector3 ballDirection = ballPosition - dronePosition;
        float ballDistance = ballDirection.magnitude;

        Vector3 droneVelocity = rigidbody.velocity;
        droneVelocity.y = 0;
        float droneVelocityR = Vector3.Dot(droneVelocity, ballDirection.normalized);


        float maxAcceleration = m_Drone.max_acceleration;
        float goalieAcceleration = goalAccelerationFactor * maxAcceleration;
        float enemyAcceleration = enemyAccelerationFactor * maxAcceleration;
        float friendAcceleration = friendAccelerationFactor * maxAcceleration;


        float timeToReachBall = (Mathf.Sqrt(2 * ballDistance * goalieAcceleration + droneVelocityR * droneVelocityR) - droneVelocityR) / goalieAcceleration;

        foreach (GameObject enemy in enemies)
        {
            Vector3 enemyPosition = enemy.transform.position;
            Vector3 enemyBallDirection = ballPosition - enemyPosition;
            float enemyBallDistance = enemyBallDirection.magnitude;

            float enemyVelocityR = 0f;
            Rigidbody enemyRB = enemy.GetComponent<Rigidbody>();
            if (enemyRB != null)
            {
                Vector3 enemyVelocity = enemyRB.velocity;
                enemyVelocity.y = 0;
                enemyVelocityR = Vector3.Dot(enemyVelocity, enemyBallDirection.normalized);
            }
            float enemyTimeToReachBall = (Mathf.Sqrt(2 * enemyBallDistance * enemyAcceleration + enemyVelocityR * enemyVelocityR) - enemyVelocityR) / enemyAcceleration;
            if (enemyTimeToReachBall <= timeToReachBall)
            {
                return false;
            }
        }
        foreach(GameObject friend in friends){
            Vector3 friendPosition = friend.transform.position;
            Vector3 friendBallDirection = ballPosition - friendPosition;
            float friendBallDistance = friendBallDirection.magnitude;

            float friendVelocityR = 0f;
            Rigidbody friendRB = friend.GetComponent<Rigidbody>();
            if (friendRB != null)
            {
                Vector3 friendVelocity = friendRB.velocity;
                friendVelocity.y = 0f;
                friendVelocityR = Vector3.Dot(friendVelocity, friendBallDirection.normalized);
            }
            float friendTimeToReachBall = (Mathf.Sqrt(2 * friendBallDistance * friendAcceleration + friendVelocityR * friendVelocityR) - friendVelocityR) / friendAcceleration;
            if (friendTimeToReachBall <= timeToReachBall)
            {
                return false;
            }
        }
        return true;
    }

    void Update()
    {
        panda.Reset();
        panda.Tick();
    }


    [Task]
    bool IsBallOnEnemySide()
    {

        float ballX = ball.transform.position.x;
        float goalX = own_goal.transform.position.x;
        return (goalX > centerX && ballX < centerX) || (goalX < centerX && ballX >= centerX);
    }

    [Task]
    void SetDefensiveRoles()
    {
        if (dynamicChanging || currentStrategy != Strategy.Defensive)
        {
            currentStrategy = Strategy.Defensive;
            List<DroneAISoccer_blue> dronesSortedByDistanceToOwnGoal = friendDrones.ToList()
                .OrderBy(drone => (drone.transform.position - own_goal.transform.position).magnitude)
                .ToList();
            dronesSortedByDistanceToOwnGoal[0].PlayerRole = PlayerRole.Goalie;
            dronesSortedByDistanceToOwnGoal[1].PlayerRole = PlayerRole.Defender;
            dronesSortedByDistanceToOwnGoal[2].PlayerRole = PlayerRole.Defender;
            dronesSortedByDistanceToOwnGoal[2].PlayerRole = PlayerRole.Defender;
            dronesSortedByDistanceToOwnGoal[2].PlayerRole = PlayerRole.Defender;
            dronesSortedByDistanceToOwnGoal[2].PlayerRole = PlayerRole.Defender;
            dronesSortedByDistanceToOwnGoal[2].PlayerRole = PlayerRole.Defender;
        }
    }


    [Task]
    void SetNeutralRoles()
    {

        if (dynamicChanging || currentStrategy != Strategy.Neutral)
        {
            currentStrategy = Strategy.Neutral;
            List<DroneAISoccer_blue> dronesSortedByDistanceToOwnGoal = friendDrones
                .OrderBy(drone => (drone.transform.position - own_goal.transform.position).magnitude)
                .ToList();
            dronesSortedByDistanceToOwnGoal[0].PlayerRole = PlayerRole.Goalie;
            dronesSortedByDistanceToOwnGoal[1].PlayerRole = PlayerRole.Defender;
            dronesSortedByDistanceToOwnGoal[2].PlayerRole = PlayerRole.Attacker;
        }

    }

    [Task]
    void SetOffensiveRoles()
    {
        if (dynamicChanging || currentStrategy != Strategy.Offensive)
        {
            currentStrategy = Strategy.Offensive;
            List<DroneAISoccer_blue> dronesSortedByDistanceToOtherGoal = friendDrones
                .OrderBy(drone => (drone.transform.position - other_goal.transform.position).magnitude)
                .ToList();
            dronesSortedByDistanceToOtherGoal[0].PlayerRole = PlayerRole.Attacker;
            dronesSortedByDistanceToOtherGoal[1].PlayerRole = PlayerRole.Attacker;
            dronesSortedByDistanceToOtherGoal[2].PlayerRole = PlayerRole.Goalie;
        }
    }

    [Task]
    bool IsManager()
    {
        return isManager;
    }

    [Task]
    public bool IsBallCloseToOwnGoal()
    {
        float goalDistance = (own_goal.transform.position - ball.transform.position).magnitude;
        return goalDistance <= isCloseToOwnGoalThreshold;
    }
    
    


    private void FixedUpdate()
    {
        //Debug.DrawLine(ball.transform.position, ball.transform.position + ballRigidbody.velocity, Color.green);
        /*
        Debug.DrawLine(transform.position, ball.transform.position, Color.black);
        Debug.DrawLine(transform.position, own_goal.transform.position, Color.green);
        Debug.DrawLine(transform.position, other_goal.transform.position, Color.yellow);
        Debug.DrawLine(transform.position, friends[0].transform.position, Color.cyan);
        Debug.DrawLine(transform.position, enemies[0].transform.position, Color.magenta);
        */
        if (CanKick())
        {
            Debug.DrawLine(transform.position, ball.transform.position, Color.red);
        }

    }
}

