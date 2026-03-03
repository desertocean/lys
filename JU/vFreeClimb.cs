using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS;
namespace Invector.vCharacterController.vActions
{
	public class vFreeClimb : vMonoBehaviour
	{
        #region Public variables

 
		[Tooltip("Tags of climb surfaces")]
		public vTagMask climbSurfaceTags = new List<string>() { "FreeClimb" };
		[Tooltip("Layer of climb surfaces")]
		public LayerMask climbSurfaceLayers = 0;
		[Tooltip("Layer to check obstacles when movement")]
		public LayerMask obstacleLayers;

		private JUCharacterController TPSCharacter;
		private CapsuleCollider TPSCollider=null;
		public LayerMask groundLayer = 1 << 0;
		
		public string cameraState = "Default";

 
		public string animatorStateHierarchy = "Base Layer.Actions.FreeClimb";

 
		public GenericInput climbEdgeInput = new GenericInput("E", "A", "A");
		public GenericInput enterExitInput = new GenericInput("Space", "X", "X");
		public GenericInput climbJumpInput = new GenericInput("Space", "X", "X");

 
		public bool moveUsingRootMotion = true;
		public float climbSpeed = 1f;
		public bool autoClimbEdge = true;

 
		public float climbStaminaCost = 0f;
		public float jumpClimbStaminaCost = 0f;
		public float staminaRecoveryDelay = 1.5f;
 

		[Range(0, 180)]
		public float minSurfaceAngle = 30, maxSurfaceAngle = 160;
		[Tooltip("Empty GameObject Child of Character\nPosition this object on the \"hand target position\"")]
		public Transform handTarget;
		[Tooltip("Offset to Hand IK")]
		public Vector3 offsetHandPositionL, offsetHandPositionR;
		public float climbEnterSpeed = 5f;
		public float climbEnterMaxDistance = 1f;
		[Tooltip("Use this to check if can go to horizontal position")]
		public float lastPointDistanceH = 0.4f;
		[Tooltip("Use this to check if can go to vertical position")]
		public float lastPointDistanceVUp = 0.2f;
		public float lastPointDistanceVDown = 1.25f;
		[Tooltip("Start Point of RayCast to check if Can GO")]
		public float offsetHandTarget = -0.2f;
		[Tooltip("Start Point of RayCast to check Base Rotation")]
		public float offsetBase = 0.35f;


 

		[Tooltip("Min/Max Distance to ClimbJump")]
		public float climbJumpMinDistance = 0.2f;
		public float climbJumpMaxDistance = 2f;
		public float climbJumpDepth = 2f;

 

		[Tooltip("Min Wall thickness to climbUp")]
		public float climbUpMinThickness = 0.3f;
		[Tooltip("Min space  to climbUp with obstruction")]
		public float climbUpMinSpace = 0.5f;
		public float climbUpHeight = 2f;
 
		public string climbUpAnimatorState = "ClimbUpWall";
		public float endExitTimeAnimation = 0.8f;
		[Range(0, 1)]
 
		public float startNormalizeTime = 0.15f;
		[Range(0, 1)]
 
		public float targetNormalizeTime = 0.4f;
 
		public AvatarTarget avatarTarget = AvatarTarget.LeftHand;
		[Range(0, 1)]
		public float matchTargetX = 0f;
		[Range(0, 1)]
		public float matchTargetY = 1f;
		[Range(0, 1)]
		public float matchTargetZ = 0f;
		[Range(0, 1)]
		public float matchRotation = 0f;

 
		public bool debugRays;
 
		public bool debugClimbMovement = true;
 
		public bool debugClimbUp;
 
		public bool debugClimbJump;
 
		public bool debugBaseRotation;
 
		public bool debugHandIK;

 

		public UnityEngine.Events.UnityEvent onEnterClimb, onExitClimb;

        #endregion

        #region Protected variables

		protected vDragInfo dragInfo;
		protected vDragInfo jumDragInfo;
 
		protected float horizontal, vertical;
		protected float oldInput = 0.1f;
		protected float ikWeight;
		protected float posTransition;
		protected float rotationTransition;
		protected float enterTransition;

		protected bool canMoveClimb;
		protected bool inClimbUp;
		protected bool inClimbJump;
		protected bool inAlingClimb;
		protected bool inClimbEnter;
		protected bool climbEnterGrounded, climbEnterAir;
		protected bool inJumpExit;

		protected Vector3 upPoint;
		protected Vector3 jumpPoint;
		protected Vector3 input;
		protected RaycastHit hit;
		protected Quaternion jumpRotation;

		Vector3 lHandPos;
		Vector3 rHandPos;
		Vector3 targetPositionL;
		Vector3 targetPositionR;
		Vector3 lastInput;

		protected Vector3 handTargetPosition
		{
			get
			{
				return transform.TransformPoint(handTarget.localPosition.x, handTarget.localPosition.y, 0);
			}
		}

        #endregion


		protected virtual void Start()
		{
			dragInfo = new vDragInfo();
			jumDragInfo = new vDragInfo();
			TPSCharacter = GetComponent<JUCharacterController>();
			TPSCollider=(CapsuleCollider)TPSCharacter.coll;
			
			
			
		}

		protected virtual void Update()
		{
			
			input = new Vector3(TPSCharacter.Inputs.MoveAxis.x, 0, TPSCharacter.Inputs.MoveAxis.y);
			ClimbHandle();
			ClimbUpHandle();
          
		}



		protected virtual void ClimbUpHandle()
		{
			if (inClimbJump || TPSCharacter.enabled || !TPSCharacter.anim || !dragInfo.inDrag) return;

			if (inClimbUp && !inAlingClimb)
			{
				if (TPSCharacter.anim.GetCurrentAnimatorStateInfo(0).IsName(animatorStateHierarchy + ".ClimbUpWall"))
				{
					if (!TPSCharacter.anim.IsInTransition(0))
						TPSCharacter.anim.MatchTarget(upPoint + Vector3.up * 0.1f, Quaternion.Euler(0, transform.eulerAngles.y, 0), avatarTarget, new MatchTargetWeightMask(new Vector3(matchTargetX, matchTargetY, matchTargetZ), matchRotation), startNormalizeTime, targetNormalizeTime);
					//if (TPSCharacter.anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= endExitTimeAnimation) ExitClimb();

					//TP_Input.cc.StopCharacter();
					//TP_Input.cc.ResetInputAnimatorParameters();
				}
				return;
			}
			CheckClimbUp();
		}
 
 
		private void CheckClimbUp(bool ignoreInput = false)
		{
			var climbUpConditions = autoClimbEdge ? vertical > 0f : climbEdgeInput.GetButtonDown();

			if (!canMoveClimb && !inClimbUp && (climbUpConditions || ignoreInput))
			{
				var dir = transform.forward;

				var startPoint = dragInfo.position + transform.forward * -0.1f;
				var endPoint = startPoint + Vector3.up * (TPSCollider.height * 0.25f);
				var obstructionPoint = endPoint + dir.normalized * (climbUpMinSpace + 0.1f);
				var thicknessPoint = endPoint + dir.normalized * (climbUpMinThickness + 0.1f);
				var climbPoint = thicknessPoint + -transform.up * (TPSCollider.height * 0.5f);

				if (!Physics.Linecast(startPoint, endPoint, obstacleLayers))
				{
					if (debugRays && debugClimbUp) Debug.DrawLine(startPoint, endPoint, Color.green, 2f);
					if (!Physics.Linecast(endPoint, obstructionPoint, obstacleLayers))
					{
						if (debugRays && debugClimbUp) Debug.DrawLine(endPoint, obstructionPoint, Color.green, 2f);
						if (Physics.Linecast(thicknessPoint, climbPoint, out hit, groundLayer))
						{
							if (debugRays && debugClimbUp) Debug.DrawLine(thicknessPoint, climbPoint, Color.green, 2f);
							var angle = Vector3.Angle(Vector3.up, hit.normal);
							var localUpPoint = transform.InverseTransformPoint(hit.point + (angle > 25 ? Vector3.up * TPSCollider.radius : Vector3.zero) + dir * -(climbUpMinThickness * 0.5f));
							localUpPoint.z = TPSCollider.radius;
							upPoint = transform.TransformPoint(localUpPoint);
							if (Physics.Raycast(hit.point + Vector3.up * -0.05f, Vector3.up, out hit, TPSCollider.height, obstacleLayers))
							{
								if (hit.distance > TPSCollider.height * 0.5f)
								{
									if (hit.distance < TPSCollider.height)
									{
										TPSCharacter.IsCrouched = true;
										TPSCharacter.anim.SetBool("Crouched", true);
									}
									ClimbUp();
								}
								else
								{
									if (debugRays && debugClimbUp) Debug.DrawLine(upPoint, hit.point, Color.red, 2f);
								}
							}
							else ClimbUp();
						}
						else if (debugRays && debugClimbUp) Debug.DrawLine(thicknessPoint, climbPoint, Color.red, 2f);
					}
					else if (debugRays && debugClimbUp) Debug.DrawLine(endPoint, obstructionPoint, Color.red, 2f);
				}
				else if (debugRays && debugClimbUp) Debug.DrawLine(startPoint, endPoint, Color.red, 2f);
			}
		}
		protected virtual void ClimbUp()
		{
			StartCoroutine(AlignClimb());
			inClimbUp = true;
		}
		
		
		IEnumerator AlignClimb()
		{
			inAlingClimb = true;
			var transition = 0f;
			var dir = transform.forward;
			dir.y = 0;
			var angle = Vector3.Angle(Vector3.up, transform.forward);

			var targetRotation = Quaternion.LookRotation(-dragInfo.normal);
			var targetPosition = ((dragInfo.position + dir * -TPSCollider.radius + Vector3.up * 0.1f) - transform.rotation * handTarget.localPosition);

			TPSCharacter.anim.SetFloat(TPSCharacter.AnimatorParameters.ClimbVerticalInput, 1f);
			while (transition < 1 && Vector3.Distance(targetRotation.eulerAngles, transform.rotation.eulerAngles) > 0.2f && angle < 60)
			{
				TPSCharacter.anim.SetFloat(TPSCharacter.AnimatorParameters.ClimbVerticalInput, 1f);
				transition += Time.deltaTime * 0.5f;
				targetPosition = ((dragInfo.position + dir * -TPSCollider.radius) - transform.rotation * handTarget.localPosition);
				transform.position = Vector3.Slerp(transform.position, targetPosition, transition);
				transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, transition);
				yield return null;
			}
			TPSCharacter.anim.CrossFadeInFixedTime("ClimbUpWall", 0.1f);
			inAlingClimb = false;
		}
		
		protected virtual void ClimbHandle()
		{
			
			if (!TPSCharacter.anim) return;
			
			if (!dragInfo.inDrag)
			{   
				//Debug.Log(handTargetPosition+" "+climbEnterMaxDistance+" "+climbSurfaceLayers);
				if (Physics.Raycast(handTargetPosition, transform.forward, out hit, climbEnterMaxDistance, climbSurfaceLayers))
				{  
					if (IsValidPoint(hit.normal, hit.collider.transform.gameObject.tag))
					{ 
						if (debugRays) Debug.DrawRay(handTargetPosition, transform.forward * climbEnterMaxDistance, Color.green);
						dragInfo.canGo = true;
						dragInfo.normal = hit.normal;
						dragInfo.collider = hit.collider;
						dragInfo.position = hit.point;
					}
				}
				else
				{
					if (debugRays) Debug.DrawRay(handTargetPosition, transform.forward * climbEnterMaxDistance, Color.red);
					dragInfo.canGo = false;
				}
			}
			if (dragInfo.canGo && !inClimbEnter && Physics.SphereCast(handTargetPosition + transform.forward * -TPSCollider.radius * 0.5f, TPSCollider.radius * 0.5f, transform.forward, out hit, climbEnterMaxDistance, climbSurfaceLayers))
			{
				dragInfo.collider = hit.collider;
				var hitPointLocal = transform.InverseTransformPoint(hit.point);
				hitPointLocal.y = handTarget.localPosition.y;
				hitPointLocal.x = handTarget.localPosition.x;

				dragInfo.position = transform.TransformPoint(hitPointLocal);
				
				if (dragInfo.canGo &&  TPSCharacter.Inputs.MoveAxis.y > 0.1f && !dragInfo.inDrag && Time.time > (oldInput + 2f)){
					
					EnterClimb();
					dragInfo.inDrag=true;
				}
					
			}
			ClimbMovement();
		}
 

		protected virtual void ClimbMovement()
		{
			horizontal = input.x;
			vertical = input.z;
 
			canMoveClimb=CheckCanMoveClimb();
			if (canMoveClimb)
			{
 
				TPSCharacter.anim.SetFloat(TPSCharacter.AnimatorParameters.ClimbHorizontalInput, horizontal, 0.2f, Time.deltaTime);
				TPSCharacter.anim.SetFloat(TPSCharacter.AnimatorParameters.ClimbVerticalInput, vertical, 0.2f, Time.deltaTime);
			}
			else if (!inAlingClimb && !inClimbJump)
			{ 
				TPSCharacter.anim.SetFloat(TPSCharacter.AnimatorParameters.ClimbHorizontalInput, 0, 0.2f, Time.deltaTime);
				TPSCharacter.anim.SetFloat(TPSCharacter.AnimatorParameters.ClimbVerticalInput, 0, 0.2f, Time.deltaTime);
			}
			//if(dragInfo.inDrag)
			//	ApplyClimbMovement();
			//if ((input.z < 0 || (inClimbJump && Mathf.Abs(input.x) < 0.1f && input.z == 0)) && Physics.Raycast(transform.position + Vector3.up * 0.1f, -Vector3.up, 0.4f, groundLayer))
			//{
			//	ExitClimb(true);
			//}
		}
		protected virtual void OnAnimatorMove()
		{
			if (TPSCharacter.enabled) return;

			climbEnterGrounded = (TPSCharacter.anim.GetCurrentAnimatorStateInfo(0).IsName(animatorStateHierarchy + ".EnterClimbGrounded"));
			climbEnterAir = (TPSCharacter.anim.GetCurrentAnimatorStateInfo(0).IsName(animatorStateHierarchy + ".EnterClimbAir"));

			if (dragInfo.inDrag && (canMoveClimb) && !inClimbUp && !inClimbJump && !climbEnterGrounded)
			{
				ApplyClimbMovement();
			}

			else if (inClimbUp || climbEnterGrounded || climbEnterAir)
			{
				if (!inClimbUp)
					CheckClimbUp(true);

				ApplyRootMotion();
			}

		}
		protected virtual void ApplyRootMotion()
		{
			transform.position = TPSCharacter.anim.rootPosition;
			transform.rotation = TPSCharacter.anim.rootRotation;
			posTransition = 0;
		}
		protected virtual void ApplyClimbMovement()
		{
			///Apply Rotation
			CalculateMovementRotation();
			///Apply Position
			posTransition = Mathf.Lerp(posTransition, 1f, 5 * Time.deltaTime);
			var root = Vector3.zero;
			if (moveUsingRootMotion)
			{
				root = transform.InverseTransformPoint(TPSCharacter.anim.rootPosition) * climbSpeed * input.magnitude;
			}
			else
			{
				root = new Vector3(input.x, input.z, 0) * climbSpeed * input.magnitude * Time.deltaTime;
			}
			var position = (dragInfo.position - transform.rotation * handTarget.localPosition) + (transform.right * root.x + transform.up * root.y);
			Debug.DrawLine(transform.position, dragInfo.position);
			if (input.magnitude > 0.1f)
				transform.position = Vector3.Lerp(transform.position, position, posTransition);
		}
		
		protected virtual void CalculateMovementRotation(bool ignoreLerp = false)
		{
			var h = lastInput.x;
			var v = lastInput.z;
			var characterBase = transform.position + transform.up * (TPSCollider.radius + offsetBase);
			var directionPoint = characterBase + transform.right * (h * lastPointDistanceH) + transform.up * (v * lastPointDistanceVUp);

			RaycastHit rotationHit;
			vLine centerLine = new vLine(characterBase, directionPoint);
			centerLine.Draw(Color.cyan, draw: debugRays && debugBaseRotation);
			var hasBasePoint = CheckBasePoint(out rotationHit);

			var basePoint = rotationHit.point;
			if (Physics.Linecast(centerLine.p1, centerLine.p2, out rotationHit, climbSurfaceLayers) && climbSurfaceTags.Contains(rotationHit.collider.gameObject.tag))
			{
				RotateTo(-rotationHit.normal, hasBasePoint ? basePoint : rotationHit.point, ignoreLerp);
				return;
			}

			centerLine.p1 = centerLine.p2;
			centerLine.p2 += transform.forward * (climbEnterMaxDistance);
			centerLine.Draw(Color.yellow, draw: debugRays && debugBaseRotation);

			if (Physics.Linecast(centerLine.p1, centerLine.p2, out rotationHit, climbSurfaceLayers) && climbSurfaceTags.Contains(rotationHit.collider.gameObject.tag))
			{
				RotateTo(-rotationHit.normal, hasBasePoint ? basePoint : rotationHit.point, ignoreLerp);
				return;
			}
			centerLine.p1 += transform.forward * TPSCollider.radius * 0.5f;
			centerLine.p2 += (transform.right * ((TPSCollider.radius + lastPointDistanceH) * -input.x)) + (transform.up * lastPointDistanceVUp * -v) + transform.forward * TPSCollider.radius;
			centerLine.Draw(Color.red, draw: debugRays && debugBaseRotation);

			if (Physics.Linecast(centerLine.p1, centerLine.p2, out rotationHit, climbSurfaceLayers) && climbSurfaceTags.Contains(rotationHit.collider.gameObject.tag))
			{
				RotateTo(-rotationHit.normal, hasBasePoint ? basePoint : rotationHit.point, ignoreLerp);
				return;
			}
		}
 
		bool CheckBasePoint(out RaycastHit baseHit)
		{
			var forward = new Vector3(transform.forward.x, 0, transform.forward.z);
			var characterBase = transform.position + transform.up * (TPSCollider.radius + offsetBase) - forward * (TPSCollider.radius * 2);

			var targetPoint = transform.position + forward * (1 + TPSCollider.radius);
			vLine baseLine = new vLine(characterBase, targetPoint);

			if (Physics.Linecast(baseLine.p1, baseLine.p2, out baseHit, climbSurfaceLayers) && climbSurfaceTags.Contains(baseHit.collider.gameObject.tag))
			{
				baseLine.Draw(Color.blue, draw: debugRays && debugBaseRotation);
				return true;
			}
			baseLine.Draw(Color.magenta, draw: debugRays);
			baseLine.p1 = baseLine.p2;
			baseLine.p2 = baseLine.p1 + forward + Vector3.up;

			if (Physics.Linecast(baseLine.p1, baseLine.p2, out baseHit, climbSurfaceLayers) && climbSurfaceTags.Contains(baseHit.collider.gameObject.tag))
			{
				baseLine.Draw(Color.blue, draw: debugRays && debugBaseRotation);
				return true;
			}
			baseLine.Draw(Color.magenta, draw: debugRays);
			baseLine.p2 = baseLine.p1 + forward + Vector3.down;

			if (Physics.Linecast(baseLine.p1, baseLine.p2, out baseHit, climbSurfaceLayers) && climbSurfaceTags.Contains(baseHit.collider.gameObject.tag))
			{
				baseLine.Draw(Color.blue, draw: debugRays && debugBaseRotation);
				return true;
			}
			baseLine.Draw(Color.magenta, draw: debugRays && debugBaseRotation);
			return false;
		}
		protected virtual void RotateTo(Vector3 direction, Vector3 point, bool ignoreLerp = false)
		{
			if (input.magnitude < 0.1f) return;
			var referenceDirection = point - dragInfo.position;
			if (debugRays && debugBaseRotation) Debug.DrawLine(point, dragInfo.position, Color.blue, .1f);
			var resultDirection = Quaternion.AngleAxis(-90, transform.right) * referenceDirection;
			var eulerX = Quaternion.LookRotation(resultDirection).eulerAngles.x;
			var baseRotation = Quaternion.LookRotation(direction);
			var resultRotation = Quaternion.Euler(eulerX, baseRotation.eulerAngles.y, transform.eulerAngles.z);
			//var eulerResult = resultRotation.eulerAngles - transform.rotation.eulerAngles;
			transform.rotation = Quaternion.Lerp(transform.rotation, resultRotation, (TPSCharacter.anim.GetCurrentAnimatorStateInfo(0).normalizedTime % 1) * 0.2f);
		}
		protected virtual bool CheckCanMoveClimb()
		{
			if (input.magnitude > 0.001f)
			{
				lastInput = input;
			}
			return true;
			var h = lastInput.x > 0 ? 1 * lastPointDistanceH : lastInput.x < 0 ? -1 * lastPointDistanceH : 0;
			var v = lastInput.z > 0 ? 1 * lastPointDistanceVUp : lastInput.z < 0 ? -1 * lastPointDistanceVDown : 0;
			var centerCharacter = handTargetPosition + transform.up * offsetHandTarget;
			var targetPosNormalized = centerCharacter + (transform.right * h) + (transform.up * v);
			var targetPos = centerCharacter + (transform.right * lastInput.x) + (transform.up * lastInput.z);
			var castDir = (targetPosNormalized - handTargetPosition + (transform.forward * -0.5f)).normalized;
			var castDirCapsule = transform.TransformDirection(new Vector3(h, v, 0));

			if (TPSCollider.CheckCapsule(castDirCapsule, out hit, TPSCollider.radius * 0.5f, TPSCollider.radius, obstacleLayers, debugRays && debugClimbMovement))
			{
				return false;
			}

			if (inClimbJump || inClimbUp) return false;
			vLine climbLine = new vLine(centerCharacter, targetPosNormalized);
			climbLine.Draw(Color.green, draw: debugRays && debugClimbMovement);
			if (Physics.Linecast(climbLine.p1, climbLine.p2, out hit, climbSurfaceLayers))
			{
				if (IsValidPoint(hit.normal, hit.collider.transform.gameObject.tag))
				{
					dragInfo.collider = hit.collider;
					dragInfo.normal = hit.normal;
					return true;
				}
			}

			climbLine.p1 = climbLine.p2;
			climbLine.p2 = climbLine.p1 + transform.forward * TPSCollider.radius * 2f;
			climbLine.Draw(Color.yellow, draw: debugRays && debugClimbMovement);
			if (Physics.Linecast(climbLine.p1, climbLine.p2, out hit, climbSurfaceLayers))
			{
				if (IsValidPoint(hit.normal, hit.collider.transform.gameObject.tag))
				{
					dragInfo.collider = hit.collider;
					dragInfo.normal = hit.normal;
					return true;
				}
			}

			climbLine.p1 += transform.forward * TPSCollider.radius * 0.5f;
			climbLine.p2 += (transform.right * (TPSCollider.radius + lastPointDistanceH) * -input.x) + (transform.up * -v) + transform.forward * TPSCollider.radius;
			climbLine.Draw(Color.red, draw: debugRays && debugClimbMovement);
			if (Physics.Linecast(climbLine.p1, climbLine.p2, out hit, climbSurfaceLayers))
			{
				if (IsValidPoint(hit.normal, hit.collider.transform.gameObject.tag))
				{
					dragInfo.normal = hit.normal;
					dragInfo.collider = hit.collider;
					return true;
				}
			}
			return false;
		}




		protected virtual void EnterClimb()
		{
			oldInput = Time.time;
			//TP_Input.cc.enabled = false;
			TPSCharacter.enabled = false;

			//TP_Input.cc.animatorStateInfos.RegisterListener();
			//TP_Input.cc.ResetCapsule();
			TPSCharacter.rb.isKinematic = true;
			RaycastHit hit;
			var dragPosition = new Vector3(dragInfo.position.x, transform.position.y, dragInfo.position.z) + transform.forward * -TPSCollider.radius;
			var castObstacleUp = Physics.Raycast(dragPosition + transform.up * TPSCollider.height, transform.up, TPSCollider.height * 0.5f, obstacleLayers);
			var castDragableWallForward = Physics.Raycast(dragPosition + transform.up * (TPSCollider.height * climbUpHeight), transform.forward, out hit, 1f, climbSurfaceLayers) && climbSurfaceTags.Contains(hit.collider.gameObject.tag);
			var climbUpConditions = TPSCharacter.IsGrounded && !castObstacleUp && castDragableWallForward;

			TPSCharacter.anim.SetBool("Climbing",true);
			TPSCharacter.anim.CrossFadeInFixedTime(climbUpConditions ? "EnterClimbGrounded" : "EnterClimbAir", 0.0f);
			if (dragInfo.collider && dragInfo.collider.transform.parent && transform.parent != dragInfo.collider.transform.parent && !dragInfo.collider.transform.parent.gameObject.isStatic)
				transform.parent = dragInfo.collider.transform.parent;
			TPSCharacter.rb.useGravity=false;
			StartCoroutine(EnterClimbAlignment(climbUpConditions));
			//onEnterClimb.Invoke();
			//TP_Input.cc.onActionStay.AddListener(OnTriggerStayEvent);
		}
		
		
		IEnumerator EnterClimbAlignment(bool enterGrounded = false)
		{
			inClimbEnter = true;
			dragInfo.inDrag = true;

			WaitForFixedUpdate fixedUpdate = new WaitForFixedUpdate();

			enterTransition = 0f;
			Debug.DrawLine(handTargetPosition, dragInfo.position, Color.red, 10f);
			Debug.DrawLine(transform.position, dragInfo.position, Color.red, 10f);
			if (enterGrounded) yield return new WaitForSeconds(0.4f);
			var _position = transform.position;
			var _rotation = transform.rotation;
			var _targetRotation = Quaternion.LookRotation(-dragInfo.normal);
			var _targetPosition = (dragInfo.position - transform.rotation * handTarget.localPosition);
			while (enterTransition <= 1f)
			{

				if (enterGrounded && Physics.Raycast(handTargetPosition, transform.forward, out hit, climbEnterMaxDistance, climbSurfaceLayers))
				{
					if (IsValidPoint(hit.normal, hit.collider.transform.gameObject.tag))
					{
						_position = transform.position;
						if (debugRays) Debug.DrawRay(handTargetPosition, transform.forward * climbEnterMaxDistance, Color.green);
						dragInfo.normal = hit.normal;
						dragInfo.collider = hit.collider;
						dragInfo.position = hit.point;
					}
				}
				enterTransition += Time.deltaTime * climbEnterSpeed;

				transform.rotation = Quaternion.Lerp(_rotation, _targetRotation, enterTransition);
				_targetPosition = (dragInfo.position - transform.rotation * handTarget.localPosition);
				transform.position = Vector3.Lerp(_position, _targetPosition, enterTransition);

				yield return fixedUpdate;
			}
			_targetPosition = (dragInfo.position - transform.rotation * handTarget.localPosition);
			transform.position = _targetPosition;
			inClimbEnter = false;
		}
		
		public class vDragInfo
		{
			public bool canGo;
			public bool inDrag;

			public Vector3 position
			{
				get
				{
					if (canGo && inDrag && collider) return collider.transform.TransformPoint(localPosition);

					return worldPosition;
				}
				set
				{
					worldPosition = value;
					if (collider)
						localPosition = collider.transform.InverseTransformPoint(value);
				}
			}
			public Vector3 normal;
			public Vector3 localPosition;
			public Vector3 worldPosition;
			public Collider collider
			{
				get
				{
					return _collider;
				}
				set
				{
					if (value != _collider && value != null)
					{
						localPosition = value.transform.InverseTransformPoint(worldPosition);
						_collider = value;
					}
				}
			}
			private Collider _collider;
		}
		protected virtual bool IsValidPoint(Vector3 normal, string tag)
		{
			if (!climbSurfaceTags.Contains(tag)) return false;

			var angle = Vector3.Angle(Vector3.up, normal);

			if (angle >= minSurfaceAngle && angle <= maxSurfaceAngle)
				return true;
			return false;
		}
	}
}