using UnityEngine;
using System.Collections;

using Panda;
public class Player : MonoBehaviour
{

    PandaBehaviour panda;
    DroneAISoccer_blue droneAi;

    void Start(){
        panda = GetComponent<PandaBehaviour>();
        droneAi = GetComponent<DroneAISoccer_blue>();
    }


    [Task]
    void CanKick()
    {
        bool canKick = droneAi.CanKick();
        if(canKick){
            Task.current.Succeed();
        } else {
            Task.current.Fail();
        }
    }

    [Task]
    void KickBall()
    {
        float kickPower = 10f;

        Vector3 kickDirection = (droneAi.other_goal.transform.position - transform.position).normalized;

        droneAi.KickBall(kickPower * kickDirection);
        
        if(!droneAi.CanKick()){
            Task.current.Succeed();
        }
    }

    [Task]
    void GoToBall(){
        if(droneAi.ball == null){
            Task.current.Fail();
            return;
        }

        Vector3 ballDirection = (droneAi.ball.transform.position - transform.position).normalized;

        droneAi.Move(ballDirection);

        if(droneAi.CanKick()){
            Task.current.Succeed();
        }
    }

    void Update(){
        panda.Reset();
        panda.Tick();
    }


}
