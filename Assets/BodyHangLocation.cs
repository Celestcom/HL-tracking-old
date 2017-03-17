using UnityEngine;
using UnityEngine.UI;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NullSpace.SDK.Demos
{
	public class BodyHangLocation : MonoBehaviour
	{
		public Camera hmd;

		public bool EnableGizmos = true;

		[Range(0, .35f)]
		public float TiltAmtWithHMD = 0.0f;

		[Header("Deriving Translation Scalars")]
		[Range(-2, 2)]
		public float downTranslationAmt = .05f;
		[Range(-2, 2)]
		public float backTranslationAmt = -.4f;
		//[Header("Dev Specific Translation")]
		private bool UseDevTranslationAmt = false;
		//[Range(-2, 2)]
		private float devDownTranslationAmt = -0.15f;

		public float PureHMDPitch = 0;

		[Header("HMD Derived Information")]
		public Vector3 assumedForward = Vector3.zero;
		public Vector3 LastUpdatedPosition = Vector3.zero;

		private enum StandbyState { Waiting, Updating, Correct }
		private StandbyState RegulateState;

		private enum BodyOrientationMode { HMD_Only, ChestIMU_Only, Blend, SuperFancy }
		private BodyOrientationMode OrientationMode = BodyOrientationMode.HMD_Only;

		[Header("In-Scene References")]
		public Text tiltDisp;
		public TrackingKinematics trackingBody;
		private GameObject SimulatedIMU;
		public LayerMask hangLayermask = ~((1 << 2) | (1 << 9) | (1 << 10) | (1 << 12) | (1 << 13) | (1 << 15));

		[Header("Update Info (not important)")]
		public float updateRate = .05f;

		public float TimeSinceUpdate = .2f;
		private float UpdateDuration = .75f;
		public float UpdateCounter = .2f;
		Vector3 targetPosition;
		public Vector3 oldAssumed;
		public float SnapUpdateDist = 1.0f;
		private Vector3 LastRelativePosition;
		
		void Start()
		{
			trackingBody = FindObjectOfType<TrackingKinematics>();
			SimulatedIMU = new GameObject();
			SimulatedIMU.transform.SetParent(transform);
			SimulatedIMU.name = "Simulated Chest IMU";
			CheckDebugDisplay();
		}

		void FixedUpdate()
		{
			DerivedBodyInformation info = null;
			if (OrientationMode == BodyOrientationMode.HMD_Only)
			{
				info = DeriveBodyInfoFromHMD();

				transform.position = Vector3.Lerp(transform.position, info.IntendedPosition, updateRate);

				transform.LookAt(transform.position + info.AssumedForward * 5, Vector3.up);
			}
			else if (OrientationMode == BodyOrientationMode.ChestIMU_Only)
			{
				info = DeriveBodyInfoFromChestIMU();
			}
			else if (OrientationMode == BodyOrientationMode.Blend)
			{
				info = DeriveBodyInfoFromBlend();

				//This back/down projects based on the headset's view position
				transform.position = Vector3.Lerp(transform.position, info.IntendedPosition, updateRate);

				DerivedBodyInformation imuInfo = DeriveBodyInfoFromChestIMU();

				//This orients the body according to the chest censor.
				transform.LookAt(transform.position + imuInfo.AssumedForward * 5, Vector3.up);

				//Next change - handle the swivel rotations.

			}
		}

		//Objectives: Allow for multiple different modes of deriving the body info.
		//Create one of these for each derivation type?

		#region Deriving Body Information
		private class DerivedBodyInformation
		{
			public BodyOrientationMode DerivationType = BodyOrientationMode.HMD_Only;

			public Vector3 AssumedForward;
			public Vector3 IntendedPosition;
		}

		private DerivedBodyInformation DeriveBodyInfoFromHMD()
		{
			DerivedBodyInformation info = new DerivedBodyInformation();
			info.DerivationType = BodyOrientationMode.HMD_Only;

			Vector3 hmdNoUp = hmd.transform.forward;
			hmdNoUp.y = 0;
			Vector3 hmdUpNoY = hmd.transform.up;
			hmdUpNoY.y = 0;

			if (Vector3.Distance(hmd.transform.position, LastUpdatedPosition) > SnapUpdateDist)
			{
				ImmediateUpdate();
			}
			else
			{
				LastRelativePosition = transform.position - hmd.transform.position;
			}

			float angleFromDown = Vector3.Angle(hmd.transform.forward, Vector3.down);
			PureHMDPitch = angleFromDown;

			UpdateDebugDisplay(angleFromDown);

			//We want to use the HMD's Up to find which way we should actually look to solve the overtilting problem
			float mirrorAngleAmt = Vector3.Angle(hmd.transform.forward, Vector3.up);

			//Check if we need to do a mirror operation
			if (mirrorAngleAmt < 5 || mirrorAngleAmt > 175)
			{
				hmdNoUp = -hmdNoUp;
			}

			UpdateCounter += Time.deltaTime * updateRate;

			if (UpdateCounter >= UpdateDuration)
			{
				UpdateCounter = 0;
				LastUpdatedPosition = hmd.transform.position;
				updateRate = .05f;
			}

			float prog = UpdateDuration - UpdateCounter;
			LastUpdatedPosition = Vector3.Lerp(LastUpdatedPosition, hmd.transform.position, Mathf.Clamp(prog / UpdateDuration, 0, 1));

			Vector3 flatRight = hmd.transform.right;
			flatRight.y = 0;

			Vector3 rep = Vector3.Cross(flatRight, Vector3.up);

			assumedForward = rep.normalized;

			//Debug.DrawLine(hmd.transform.position + Vector3.up * 5.5f, hmd.transform.position + rep + Vector3.up * 5.5f, Color.grey, .08f);

			float dist = hmd.transform.position.y + .75f;
			//Debug.Log(dist + " away " + dist * beltHeightPercentage + "  \n" + hmd.transform.position);
			Vector3 hmdDown = Vector3.down * dist * (UseDevTranslationAmt ? devDownTranslationAmt : downTranslationAmt);
			targetPosition = assumedForward * backTranslationAmt + hmd.transform.position + hmdDown;

			//Debug.Log("Target Position: " + targetPosition + " \t\t" + hmd.transform.position + "\n" + hmdDown.ToString() + "\t\t" + hmdDown.magnitude + "\t\t");
			info.AssumedForward = assumedForward;
			info.IntendedPosition = targetPosition;

			return info;
		}
		private DerivedBodyInformation DeriveBodyInfoFromChestIMU()
		{
			DerivedBodyInformation info = new DerivedBodyInformation();
			info.DerivationType = BodyOrientationMode.ChestIMU_Only;

			SimulatedIMU.transform.localPosition = Vector3.zero;
			//trackingBody.calibrator.GetOrientation(Imu.Chest);
			SimulatedIMU.transform.rotation = trackingBody.calibrator.GetOrientation(Imu.Chest);

			info.AssumedForward = SimulatedIMU.transform.forward;
			//SimulatedIMU.transform.localRotation = Quaternion.Lerp(trackingBody.calibrator.GetOrientation(Imu.Chest), SimulatedIMU.transform.rotation, 0.3f);

			return info;
		}
		private DerivedBodyInformation DeriveBodyInfoFromBlend()
		{
			DerivedBodyInformation hmdInfo = DeriveBodyInfoFromHMD();
			DerivedBodyInformation chestInfo = DeriveBodyInfoFromChestIMU();

			DerivedBodyInformation info = new DerivedBodyInformation();
			info.DerivationType = BodyOrientationMode.Blend;
			info.IntendedPosition = hmdInfo.IntendedPosition;
			info.AssumedForward = SimulatedIMU.transform.forward;

			return info;
		}
		#endregion

		void ImmediateUpdate()
		{
			//Debug.Log("Immediate update\n");
			//LastUpdatedPosition = hmd.transform.position;
			transform.position = hmd.transform.position + LastRelativePosition;
		}

		void CalculateCurrentLocation()
		{

		}

		void CheckDebugDisplay()
		{
			if (tiltDisp != null)
			{
				if (EnableGizmos)
				{
					tiltDisp.transform.parent.gameObject.SetActive(true);
				}
				else
				{
					tiltDisp.transform.parent.gameObject.SetActive(false);
				}
			}
		}

		void UpdateDebugDisplay(float angleFromDown)
		{
			CheckDebugDisplay();
			if (EnableGizmos)
			{
				if (tiltDisp != null)
				{
					tiltDisp.text = "Tilt: " + ((int)angleFromDown);

					tiltDisp.color = angleFromDown < 40 ? Color.blue : angleFromDown > 140 ? Color.green : Color.white;
				}
			}
		}

		void OnGUI()
		{
			GUI.Box(new Rect(new Vector2(Screen.width - 160, 50), new Vector2(150, 40)), "Current Mode\n" + OrientationMode.ToString());

			if (GUI.Button(new Rect(new Vector2(Screen.width - 160, 100), new Vector2(150, 40)), "Derive from HMD"))
			{
				OrientationMode = BodyOrientationMode.HMD_Only;
			}
			if (GUI.Button(new Rect(new Vector2(Screen.width - 160, 150), new Vector2(150, 40)), "Derive from Orientation"))
			{
				OrientationMode = BodyOrientationMode.ChestIMU_Only;
			}
			if (GUI.Button(new Rect(new Vector2(Screen.width - 160, 200), new Vector2(150, 40)), "Blend Derivations"))
			{
				OrientationMode = BodyOrientationMode.Blend;
			}
		}

		void OnDrawGizmos()
		{
			if (EnableGizmos)
			{
				Color displayColor = new Color(0.0f, .0f, .0f, .75f);

#if UNITY_EDITOR
				if (Selection.activeGameObject == gameObject)
					displayColor = new Color(.00f, .00f, .00f, 0.0f);
#endif
				Gizmos.color = Color.red - displayColor;
				Vector3 upComp = Vector3.up * 0f;
				Gizmos.DrawRay(transform.position + upComp - transform.right * .25f, transform.right * .5f);
				upComp = Vector3.up * .25f;
				Gizmos.DrawRay(transform.position + upComp - transform.right * .25f, transform.right * .5f);
				upComp = Vector3.up * -.25f;
				Gizmos.DrawRay(transform.position + upComp - transform.right * .25f, transform.right * .5f);
				Gizmos.DrawRay(transform.position + upComp - transform.right * .25f, transform.up * .5f);
				Gizmos.DrawRay(transform.position + upComp + transform.right * .25f, transform.up * .5f);
				Gizmos.color = Color.cyan;
				Gizmos.DrawRay(transform.position + upComp, transform.up * .5f);
				Gizmos.color = new Color(1, 1, 0, .5f);

				if (SimulatedIMU != null)
				{
					Gizmos.DrawSphere(SimulatedIMU.transform.position, .33f);
					Gizmos.color = Color.yellow;
					Gizmos.DrawRay(SimulatedIMU.transform.position, SimulatedIMU.transform.forward * 1f);
				}

				Gizmos.color = new Color(0, 0, 0, .25f);
				Gizmos.DrawSphere(targetPosition, .05f);
				//DebugExtension.DrawArrow(transform.position, transform.right * .25f, Color.red - displayColor);
				if (hmd != null)
				{
					//	DebugExtension.DrawCapsule(hmd.transform.position, transform.position, Color.red - displayColor, .25f);
				}
			}
		}


		#region MyRegion
		//void FixedUpdate()
		//{
		//	Vector3 hmdNoUp = hmd.transform.forward;
		//	hmdNoUp.y = 0;
		//	Vector3 hmdUpNoY = hmd.transform.up;
		//	hmdUpNoY.y = 0;

		//	if (Vector3.Distance(hmd.transform.position, LastUpdatedPosition) > SnapUpdateDist)
		//	{
		//		ImmediateUpdate();
		//	}
		//	else
		//	{
		//		LastRelativePosition = transform.position - hmd.transform.position;
		//	}

		//	float angleFromDown = Vector3.Angle(hmd.transform.forward, Vector3.down);

		//	UpdateDebugDisplay(angleFromDown);

		//	//We want to use the HMD's Up to find which way we should actually look to solve the overtilting problem
		//	float mirrorAngleAmt = Vector3.Angle(hmd.transform.forward, Vector3.up);

		//	//Check if we need to do a mirror operation
		//	if (mirrorAngleAmt < 5 || mirrorAngleAmt > 175)
		//	{
		//		hmdNoUp = -hmdNoUp;
		//	}

		//	UpdateCounter += Time.deltaTime * updateRate;

		//	if (UpdateCounter >= UpdateDuration)
		//	{
		//		UpdateCounter = 0;
		//		LastUpdatedPosition = hmd.transform.position;
		//		updateRate = .05f;
		//	}

		//	float prog = UpdateDuration - UpdateCounter;
		//	LastUpdatedPosition = Vector3.Lerp(LastUpdatedPosition, hmd.transform.position, Mathf.Clamp(prog / UpdateDuration, 0, 1));

		//	Vector3 flatRight = hmd.transform.right;
		//	flatRight.y = 0;

		//	Vector3 rep = Vector3.Cross(flatRight, Vector3.up);

		//	assumedForward = rep.normalized;

		//	//Debug.DrawLine(hmd.transform.position + Vector3.up * 5.5f, hmd.transform.position + rep + Vector3.up * 5.5f, Color.grey, .08f);

		//	float dist = hmd.transform.position.y;
		//	//Debug.Log(dist + " away " + dist * beltHeightPercentage + "  \n" + hmd.transform.position);
		//	Vector3 hmdDown = Vector3.down * dist * (UseDevHeight ? devHeightPercentage : beltHeightPercentage);
		//	targetPosition = assumedForward * .2f + hmd.transform.position + hmdDown;

		//	transform.position = Vector3.Lerp(transform.position, targetPosition, updateRate);

		//	transform.LookAt(transform.position + assumedForward * 5, Vector3.up);
		//} 
		#endregion
	}
}