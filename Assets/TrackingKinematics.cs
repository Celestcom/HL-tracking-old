using UnityEngine;
using System.Collections;

using NullSpace.SDK;
using NullSpace.SDK.Tracking;
using System;
using System.Collections.Generic;

public class TrackingKinematics : MonoBehaviour
{

	public GameObject TrackedChest;
	public GameObject TrackedHead;
	public GameObject TrackedLeftUpperArm;
	public GameObject TrackedRightUpperArm;
	public GameObject TrackedLeftHand;
	private Quaternion _lastLeftArm = Quaternion.identity;
	public MyCalibrator calibrator;
	public Vector3 Offset;
	public Vector3 Offset2;
	public float HorizontalShoulderOffset;
	public float VerticalShoulderOffset;
	public Vector3 ArmOffset;
	[Range(0.1f, 0.7f)]
	public float ShoulderLength = 0.0f;
	private GameObject _forearm;
	private GameObject _elbowJoint;
	private GameObject _upperarm;
	private float forearmLength;
	private float upperarmLength;
	public float NeckOffset = 0.1f;
	public GameObject ArmMeasureObject;
	public Material transparent;

	void Start()
	{
		Offset = new Vector3(0, -1.11f, 0);
		Offset2 = new Vector3(0, 0.67f, 0);

		ArmOffset = new Vector3(-0.33f, -0.69f, 0.34f);
		calibrator = new MyCalibrator(TrackedHead, TrackedChest.transform.localRotation, TrackedLeftUpperArm.transform.localRotation, TrackedRightUpperArm.transform.localRotation);
		NSManager.Instance.SetImuCalibrator(calibrator);


		_elbowJoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
		_elbowJoint.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
		_elbowJoint.name = "ElbowJoint";


		_forearm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
		_forearm.transform.localScale = new Vector3(.2f, 0.2f, 0.2f);

		_forearm.name = "ForearmSegment";

		_upperarm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
		_upperarm.name = "UpperArmSegment";

		upperarmLength = 0.40f;
		//ArmMeasureObject = _upperarm;
		forearmLength = 0.42f;

		ForceTransparent(_elbowJoint);
		ForceTransparent(_forearm);
		ForceTransparent(_upperarm);

		//vertshoulder = -0.21
		//horizshoulder = 0.15

	}

	private void ForceTransparent(GameObject obj)
	{
		if (obj == null)
			return;

		MeshRenderer mr = obj.GetComponent<MeshRenderer>();
		if (mr != null && transparent != null)
		{
			mr.material = transparent;
		}
	}

	#region Late Updates (Basic, Chest, Arms)
	void LateUpdate()
	{
		Vector3 leftOffset = new Vector3(0, ShoulderLength, 0);
		Vector3 rightOffset = new Vector3(0, ShoulderLength, 0);
		LateUpdateChest();

		var chestPos = TrackedChest.transform.position;

		LateUpdateLeftArm();
		LateUpdateRightArm();
	}
	void LateUpdateChest()
	{
		TrackedChest.transform.position = TrackedHead.transform.position;
		TrackedChest.transform.position += new Vector3(0, 0.02f, 0);
		TrackedChest.transform.position += new Vector3(0, 0, NeckOffset);
	}
	void LateUpdateLeftArm()
	{
		if (TrackedLeftUpperArm != null)
		{
			TrackedLeftUpperArm.transform.SetParent(TrackedChest.transform);
			TrackedLeftUpperArm.transform.position = TrackedChest.transform.position;
			TrackedLeftUpperArm.transform.position += -TrackedChest.transform.right * HorizontalShoulderOffset;
			TrackedLeftUpperArm.transform.position += TrackedChest.transform.up * VerticalShoulderOffset;

			TrackedLeftUpperArm.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

			_upperarm.transform.position = TrackedLeftUpperArm.transform.position;
			_upperarm.transform.up = TrackedLeftUpperArm.transform.up;
			//_upperarm.transform.forward = TrackedLeftUpperArm.transform.forward;
			//	_upperarm.transform.eulerAngles += new Vector3(0,90,0);
			_upperarm.transform.position += -TrackedLeftUpperArm.transform.up * (upperarmLength / 2f + 0.1f);
			_upperarm.transform.localScale = new Vector3(0.18f, upperarmLength, 0.18f);

			_elbowJoint.transform.SetParent(TrackedLeftUpperArm.transform);

			_elbowJoint.transform.position = TrackedLeftUpperArm.transform.position;
			_elbowJoint.transform.position += -TrackedLeftUpperArm.transform.up.normalized * upperarmLength * 1.5f;

			Vector3 leftForearmDir = TrackedLeftHand.transform.position - _elbowJoint.transform.position;
			_elbowJoint.transform.LookAt(TrackedLeftHand.transform.position);

			_forearm.transform.position = _elbowJoint.transform.position;
			_forearm.transform.forward = _elbowJoint.transform.forward;
			_forearm.transform.eulerAngles += new Vector3(90, 0, 0);
			_forearm.transform.position += _elbowJoint.transform.forward * (forearmLength / 2f + 0.1f);
			_forearm.transform.localScale = new Vector3(0.14f, forearmLength, 0.14f);
		}
	}
	void LateUpdateRightArm()
	{
		if (TrackedRightUpperArm != null)
		{
			TrackedRightUpperArm.transform.SetParent(TrackedChest.transform);
			TrackedRightUpperArm.transform.position = TrackedChest.transform.position;
			TrackedRightUpperArm.transform.position += -TrackedChest.transform.right * HorizontalShoulderOffset;
			TrackedRightUpperArm.transform.position += TrackedChest.transform.up * VerticalShoulderOffset;

			TrackedRightUpperArm.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
		}
	}
	#endregion

	void Update()
	{
		TrackedChest.transform.localRotation = Quaternion.Lerp(calibrator.GetOrientation(Imu.Chest), TrackedChest.transform.rotation, 0.3f);
		//TrackedChest.transform.localRotation = calibrator.GetOrientation(Imu.Chest);
		//TrackedLeftUpperArm.transform.rotation = calibrator.GetOrientation(Imu.Left_Upper_Arm);
		//TrackedRightUpperArm.transform.rotation = calibrator.GetOrientation(Imu.Right_Upper_Arm);

		TrackedLeftUpperArm.transform.rotation = Quaternion.Lerp(calibrator.GetOrientation(Imu.Left_Upper_Arm), TrackedLeftUpperArm.transform.rotation, 0.3f);
		//Debug.Log(calibrator.GetOrientation(Imu.Left_Upper_Arm) + "\n");
		TrackedRightUpperArm.transform.rotation = Quaternion.Lerp(calibrator.GetOrientation(Imu.Right_Upper_Arm), TrackedRightUpperArm.transform.rotation, 0.3f);

		//TrackedLeftUpperArm.transform.
		//	TrackedLeftUpperArm.transform.Translate(new Vector3(0, 0,1) * 2);
		//TrackedLeftUpperArm.transform.rotation = TrackedChest.transform.rotation;

		if (Input.GetKeyDown(KeyCode.Space))
		{
			calibrator.CalibrateAll();
		}
		if (Input.GetKeyDown(KeyCode.C))
		{
			CalibrateLengths();
		}
	}

	void CalibrateLengths()
	{
		if (ArmMeasureObject != null)
		{
			//measure dist from hand to head

			float a = Vector3.Distance(TrackedLeftHand.transform.position, ArmMeasureObject.transform.position);
			float b = 0.1f; //neck length
			float c = Mathf.Sqrt(a * a + b * b);

			float forearmLength = a / 2.0f - 0.1f;
			float upperarmLength = a / 2.0f - 0.2f;

			this.forearmLength = forearmLength;
			this.upperarmLength = upperarmLength;
			Debug.Log("Length is " + upperarmLength);
		}
	}

	private void OnGUI()
	{
		if (GUI.Button(new Rect(new Vector2(100, 100), new Vector2(100, 50)), "Calibrate Chest"))
		{
			calibrator.CalibrateAll();
		}
	}
	class ProcessedImu
	{
		public Quaternion rawQuat;
		public Quaternion originalGameOrientation;
		public string name;
		public Func<Quaternion, Quaternion> transform;
		public Func<Quaternion, Quaternion> remap;
		public ProcessedImu(Quaternion q, string name)
		{
			rawQuat = q;
			transform = (Quaternion qq) => qq;
			remap = (Quaternion qq) => qq;
			this.name = name;
		}
		public Quaternion Get()
		{
			var newQu = transform(remap(rawQuat));
			//if (name == "Chest")
			//	{
			//		Debug.Log("Rotation was" + rawQuat);

			//	Debug.Log("New is" + newQu);
			//	}
			return newQu;
		}
		public override string ToString()
		{
			return name;
		}
	}
	public class MyCalibrator : IImuCalibrator
	{
		GameObject head;
		IDictionary<Imu, ProcessedImu> _imuMap;
		IDictionary<Imu, Quaternion> _final;
		public MyCalibrator(GameObject head, Quaternion chestRef, Quaternion lArmRef, Quaternion rArmRef)
		{
			this.head = head;
			var enabledImus = new List<Imu>() { Imu.Chest, Imu.Left_Upper_Arm, Imu.Right_Upper_Arm };
			_imuMap = new Dictionary<Imu, ProcessedImu>();

			foreach (var imu in enabledImus)
			{
				_imuMap[imu] = new ProcessedImu(Quaternion.identity, imu.ToString());

			}
			_imuMap[Imu.Chest].originalGameOrientation = chestRef.Clone();
			_imuMap[Imu.Left_Upper_Arm].originalGameOrientation = lArmRef.Clone();
			_imuMap[Imu.Right_Upper_Arm].originalGameOrientation = rArmRef.Clone();


		}
		public Quaternion GetOrientation(Imu imu)
		{
			if (_imuMap.ContainsKey(imu))
			{

				return _imuMap[imu].Get();
			}
			else
			{
				return Quaternion.identity;
			}
		}
		#region Calibrate
		public void CalibrateAll()
		{
			CalibrateChest();
			CalibrateLeft();
			CalibrateRight();
		}
		public void CalibrateChest()
		{
			//imu's Y is unit's Y
			//imu's X is unity's X
			//imus' Z is unity'Z
			//var currentWorldOrientation = _imuMap[Imu.Chest].rawQuat;
			//var originalGameRotation = _imuMap[Imu.Chest].originalGameOrientation;
			//Quaternion forward = Quaternion.AngleAxis(0, Vector3.forward);

			//_imuMap[Imu.Chest].remap = (Quaternion q) =>
			//{

			// return Quaternion.Inverse(new Quaternion(q.y, q.x, -q.z, q.w));
			//};
			//var dif = _imuMap[Imu.Chest].remap(_imuMap[Imu.Chest].rawQuat) * Quaternion.Inverse(forward);
			////var dif = Quaternion.Inverse(_imuMap[Imu.Chest].remap(currentWorldOrientation)) * originalGameRotation;

			//_imuMap[Imu.Chest].transform = (Quaternion q) =>
			//{
			//	return q * dif;
			//};


			Quaternion forward = Quaternion.LookRotation(Vector3.forward, Vector3.up);
			Func<Quaternion, Quaternion> remapper = (Quaternion q) =>
			{
				return Quaternion.Inverse(new Quaternion(q.y, q.x, -q.z, q.w));
			};
			_imuMap[Imu.Chest].remap = remapper;
			var dif = remapper(_imuMap[Imu.Chest].rawQuat) * Quaternion.Inverse(forward);
			_imuMap[Imu.Chest].transform = (Quaternion q) => { return dif * Quaternion.Inverse(q); };

		}

		public void CalibrateLeft()
		{
			Debug.Log("Calibrating Left\n");

			var currentWorldOrientation = _imuMap[Imu.Left_Upper_Arm].rawQuat;

			var originalGameRotation = _imuMap[Imu.Left_Upper_Arm].originalGameOrientation;

			_imuMap[Imu.Left_Upper_Arm].remap = (Quaternion q) =>
			{


				//imu's x is unity's y
				//imu's y is unity's z
				//imu's z is unity's x

				return new Quaternion(q.y, -q.z, -q.x, q.w);
				//(a,-b,-d,-c) 
				//This creates the correct behavior but results in Gimbal Lock
				//Quaternion quat = new Quaternion(-q.z, q.y, -q.x, q.w);
				//Vector3 newEuler = quat.eulerAngles;
				////Debug.Log(newEuler.ToString() + "\n");
				//newEuler = new Vector3(-newEuler.x, newEuler.y, newEuler.z);
				//quat.eulerAngles = newEuler;

				//None of these work.
				//return Quaternion.Inverse(new Quaternion(-q.z, q.y, -q.x, q.w));
				//return Quaternion.Inverse(new Quaternion(q.z, -q.y, q.x, -q.w));
				//return new Quaternion(-q.z, -q.y, -q.x, q.w);
				//return new Quaternion(-q.z, q.y, -q.x, -q.w);
				//return new Quaternion(-q.z, q.y, q.x, q.w);
				//return new Quaternion(-q.z, q.y, -q.x, q.w);
				//return new Quaternion(q.z, q.y, -q.x, q.w);
				//return q;
			};

			var dif = Quaternion.Inverse(_imuMap[Imu.Left_Upper_Arm].remap(currentWorldOrientation)) * originalGameRotation;

			var newDif = dif.Clone();
			_imuMap[Imu.Left_Upper_Arm].transform = (Quaternion q) =>
			{
				return q * newDif;
			};
		}
		public void CalibrateRight()
		{

			var currentWorldOrientation = _imuMap[Imu.Right_Upper_Arm].rawQuat;
			var originalGameRotation = _imuMap[Imu.Right_Upper_Arm].originalGameOrientation;


			_imuMap[Imu.Right_Upper_Arm].remap = (Quaternion q) =>
			{


				//imu's x is unity's y
				//imu's y is unity's z
				//imu's z is unity's x

				return new Quaternion(q.y, -q.z, -q.x, q.w);
				//(a,-b,-d,-c) 
				//This creates the correct behavior but results in Gimbal Lock
				//Quaternion quat = new Quaternion(-q.z, q.y, -q.x, q.w);
				//Vector3 newEuler = quat.eulerAngles;
				////Debug.Log(newEuler.ToString() + "\n");
				//newEuler = new Vector3(-newEuler.x, newEuler.y, newEuler.z);
				//quat.eulerAngles = newEuler;

				//None of these work.
				//return Quaternion.Inverse(new Quaternion(-q.z, q.y, -q.x, q.w));
				//return Quaternion.Inverse(new Quaternion(q.z, -q.y, q.x, -q.w));
				//return new Quaternion(-q.z, -q.y, -q.x, q.w);
				//return new Quaternion(-q.z, q.y, -q.x, -q.w);
				//return new Quaternion(-q.z, q.y, q.x, q.w);
				//return new Quaternion(-q.z, q.y, -q.x, q.w);
				//return new Quaternion(q.z, q.y, -q.x, q.w);
				//return q;
			};

			var dif = Quaternion.Inverse(_imuMap[Imu.Right_Upper_Arm].remap(currentWorldOrientation)) * originalGameRotation;

			var newDif = dif.Clone();
			_imuMap[Imu.Right_Upper_Arm].transform = (Quaternion q) =>
			{
				return q * newDif;
			};



		}
		#endregion
		public void ReceiveUpdate(TrackingUpdate update)
		{
			_imuMap[Imu.Chest].rawQuat = update.Chest;
			_imuMap[Imu.Left_Upper_Arm].rawQuat = update.LeftUpperArm;

			_imuMap[Imu.Right_Upper_Arm].rawQuat = update.RightUpperArm;


		}
	}
}
