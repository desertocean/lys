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
			public bool IsRunning { get; set; }
			public bool IsSprinting { get; set; }
			public bool IsAttackPose { get; set; }
			public bool IsAttacking { get; set; }
			public bool IsProne { get; set; }
			public bool IsCrouch { get; set; }
			public Vector3 MoveToDirection { get; set; }
			public Vector3 LookToDirection { get; set; }
			public AdvancedControlData(Vector3 MoveToDirection = new Vector3(), Vector3 LookToDirection = new Vector3(), bool IsRunning =false,bool IsSprinting=true,bool IsAttackPose =false,bool IsAttacking =false,bool IsProne =false,bool IsCrouch =false ) { 
				this.IsRunning=IsRunning;
				this.IsSprinting=IsSprinting;
				this.IsAttackPose=IsAttackPose;
				this.IsAttacking=IsAttacking;
				this.IsProne=IsProne;
				this.IsCrouch=IsCrouch;
				this.MoveToDirection = MoveToDirection;
				this.LookToDirection = LookToDirection;

            }
		}
		
 
		public GameObject Target ;
		public Collider TargetCollider { get; private set; }
 
		public AdvancedWayPointsPath WayPointsPath;
		private int CurrentIndex=0;
		private float StopDistance=0.8f;
		private AdvancedWayPointsPath.AdvancedWaypoint GetCurrentAdvancedWaypoint(Vector3 aiPosition){
			AdvancedWayPointsPath.AdvancedWaypoint AdvanceWaypoint=WayPointsPath.AdvanceWaypointsList[CurrentIndex];
			Vector3 currentWaypoint=AdvanceWaypoint.Position;
			float aiWaypointDistance = Vector3.Distance(aiPosition, currentWaypoint);
			if(aiWaypointDistance < StopDistance){
				if (CurrentIndex < WayPointsPath.AdvanceWaypointsList.Count-1)
				{   
					CurrentIndex++;
				} 
			}
			return CurrentIndex== WayPointsPath.AdvanceWaypointsList.Count?null:AdvanceWaypoint;
		}
		private AdvancedControlData GetControlData(){
			Vector3 AiPosition=transform.position;
			AdvancedWayPointsPath.AdvancedWaypoint CurrentAdvanceWaypoint= GetCurrentAdvancedWaypoint(AiPosition);
			AdvancedControlData controlData = new AdvancedControlData();
			if(CurrentAdvanceWaypoint!=null){
				if(CurrentAdvanceWaypoint.MoveType==AdvancedWayPointsPath.AWMoveTypes.Run){
					controlData.IsRunning=true;
				
				}else if(CurrentAdvanceWaypoint.MoveType==AdvancedWayPointsPath.AWMoveTypes.Sprint){
					controlData.IsRunning=true;
					controlData.IsSprinting = true;
				}
				
				if(CurrentAdvanceWaypoint.PoseType==AdvancedWayPointsPath.AWPoseTypes.Crouch){
					controlData.IsCrouch=true;
				}else if (CurrentAdvanceWaypoint.PoseType==AdvancedWayPointsPath.AWPoseTypes.Lie){
					controlData.IsProne=true;
				}

				Vector3 CurrentPosition=CurrentAdvanceWaypoint.Position;
 
				controlData.MoveToDirection = CurrentPosition-AiPosition;
				controlData.LookToDirection = new Vector3(CurrentPosition.x,1,CurrentPosition.z);
			}else{
				Character.SwitchToItem(id: 0, RightHand: true);
				controlData.IsAttacking=true;
				controlData.IsAttackPose=true;
				controlData.MoveToDirection  = Vector3.zero;	  
				Vector3 CharacterPosition=Target.transform.position;
				controlData.LookToDirection =Vector3.zero; new Vector3(CharacterPosition.x,1,CharacterPosition.z);
				
			}
		
			return controlData;
			}
	
			protected override void Update()
			{
				AdvancedControlData ControlInfo = GetControlData();
				if (Character.IsDead)
				{
					enabled = false;
					return;
				}
				UpdateCharacterControls(ControlInfo);
			}
	
	
			protected  void UpdateCharacterControls(AdvancedControlData ControlInfo)
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
				}else{
					if(Character.IsCrouched){
						Character._GetUp();
					}
					if(Character.IsProne){
						Character._GetUp();
						Character._GetUp();
					}
					
				}
			 
				Character.IsSprinting = ControlInfo.IsSprinting;
				Character.LookAtPosition=ControlInfo.LookToDirection;
				Character._Move(moveDirection.x, moveDirection.z, running);
			}
		    
		    
	    }
	}
