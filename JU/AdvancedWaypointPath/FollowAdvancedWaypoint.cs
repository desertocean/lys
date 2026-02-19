using JUTPS.AI;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
namespace JU.CharacterSystem.AI
{
    /// <summary>
    /// Follow a waypoint.
    /// </summary>
    [System.Serializable]
    public class FollowAdvancedWaypoint : JUCharacterAIBase
    {
	    public AdvancedWayPointsPath WayPointsPath;
	    private int CurrentIndex=0;
	    private int StopDistance=1;
	    private AIControlData GetControlData(){
 
		    Vector3 currentWaypoint= WayPointsPath.AdvanceWaypointsList[CurrentIndex].Position;
		    Vector3 aiPosition =  transform.position;
		    Vector3 moveDirection = currentWaypoint - aiPosition;
		    Vector3 lookDirection = moveDirection;
		    if (lookDirection.magnitude < 1f)
			    lookDirection = transform.forward;
		    float lastWaypointDistance = Vector3.Distance(aiPosition, currentWaypoint);
		    if(lastWaypointDistance < StopDistance){
			    if (CurrentIndex == WayPointsPath.AdvanceWaypointsList.Count-1)
			    { 
				    moveDirection = Vector3.zero;
			    }else{
			    	
			    	CurrentIndex++;
			    }   
		    }
		    AIControlData controlData = new AIControlData();
		    controlData.IsRunning = true;
		    controlData.IsAttackPose = true;
		    controlData.MoveToDirection = moveDirection;
		    controlData.LookToDirection = lookDirection;
	    	return controlData;
	    }

	    protected override void Update()
	    {
		    base.Update();
		    Control = GetControlData();
	    }
	 
    }
}
