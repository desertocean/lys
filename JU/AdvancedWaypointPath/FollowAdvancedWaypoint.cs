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
    public class FollowAdvancedWaypoint : JUProtectedCharacterAIBase
	{
		[System.Serializable]
		public struct AdvancedControlData
		{
			public bool IsRunning;
			public bool IsAttackPose;
			public bool IsAttacking;
			public bool IsProne;
			public bool IsCrouch;
			public Vector3 MoveToDirection;
			public Vector3 LookToDirection;
		}
		private AdvancedControlData ControlInfo { get; set; }
	    public AdvancedWayPointsPath WayPointsPath;
	    private int CurrentIndex=0;
		private float StopDistance=0.8f;
	    private AdvancedControlData GetControlData(){
 
		    Vector3 currentWaypoint= WayPointsPath.AdvanceWaypointsList[CurrentIndex].Position;
		    Vector3 aiPosition =  transform.position;
		    Vector3 moveDirection = currentWaypoint - aiPosition;
		    Vector3 lookDirection = moveDirection;
		    if (lookDirection.magnitude < 1f)
			    lookDirection = transform.forward;
		    float lastWaypointDistance = Vector3.Distance(aiPosition, currentWaypoint);
		    //Debug.Log(CurrentIndex+" "+lastWaypointDistance+" "+ (WayPointsPath.AdvanceWaypointsList.Count-1)+" "+lastWaypointDistance+" "+(lastWaypointDistance < StopDistance));
		    AdvancedControlData controlData = new AdvancedControlData();
		    controlData.IsCrouch = false;
		    if(lastWaypointDistance < StopDistance){
			    if (CurrentIndex >= WayPointsPath.AdvanceWaypointsList.Count-2)
			    {   
				    controlData.IsCrouch = true;
			    } 
			    if (CurrentIndex == WayPointsPath.AdvanceWaypointsList.Count-1)
			    {   
				    moveDirection = Vector3.zero;
				    lookDirection = Vector3.zero;
			    }else{
			    	
			    	CurrentIndex++;
			    }   
		    }
		    
		    controlData.IsRunning = true;
		    controlData.IsAttackPose = false;
			controlData.IsProne= false;
		    controlData.MoveToDirection = moveDirection;
		    controlData.LookToDirection = lookDirection;
	    	return controlData;
	    }

	    protected override void Update()
	    {
		    
		    ControlInfo = GetControlData();
		    if (Character.IsDead)
		    {
			    enabled = false;
			    return;
		    }

		    UpdateCharacterControls();
	    }
	    
	    
	    protected override void UpdateCharacterControls()
	    {
		    bool attackPose = ControlInfo.IsAttackPose;
		    bool attacking = ControlInfo.IsAttacking;
		    bool running = ControlInfo.IsRunning;
		    Vector3 moveDirection = ControlInfo.MoveToDirection;

		    bool isMoving = moveDirection.magnitude > 0.1f;
		    if (isMoving)
		    {
			    moveDirection = Vector3.ProjectOnPlane(moveDirection, Vector3.up);
			    moveDirection /= moveDirection.magnitude;
		    }

		    // Force look to the the direction if is not in fire mode because the normal way to look to the direction
		    // works only if is on fire mode.
		    if (!Control.IsAttackPose && !isMoving && Control.LookToDirection.magnitude > 0)
			    Character.DoLookAt(transform.position + (ControlInfo.LookToDirection * 10));

		    Character.FiringModeIK = attackPose && Character.RightHandWeapon;
		    Character.FiringMode = attackPose && Character.RightHandWeapon;
		    Character.DefaultUseOfAllItems(attacking, attacking, attacking, true, attacking, attacking, attacking && !Character.RightHandWeapon);

		    if (!MoveEnabled)
			    moveDirection = Vector3.zero;
			
		    if(ControlInfo.IsProne){
		    	if(!Character.IsProne){
			    	Character._Prone();	
		    	}
		    	
		    }else if(ControlInfo.IsCrouch){
		    	if(!Character.IsCrouched){
			    	Character._Crouch();	
		    	}
		    }
			    
		    Character._Move(moveDirection.x, moveDirection.z, running);
	    }
    }
}
