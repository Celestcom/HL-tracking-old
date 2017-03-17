using UnityEngine;
using System.Collections;

using NullSpace.SDK;
using NullSpace.SDK.Tracking;
using System;
using System.Collections.Generic;

public class Tracking : MonoBehaviour {

	public GameObject TrackedChest;
	public GameObject TrackedLeftUpperArm;
	public GameObject TrackedRightUpperArm;
	private Quaternion prevChest;
	private MyCalibrator calibrator;
	// Use this for initialization
	void Start () {
		calibrator = new MyCalibrator();
		NSManager.Instance.SetImuCalibrator(calibrator);
		prevChest = calibrator.GetOrientation(Imu.Chest);
	}
	
	// Update is called once per frame
	void Update () {

		var leftUpperRot = calibrator.GetOrientation(Imu.Left_Upper_Arm);
		var chest = calibrator.GetOrientation(Imu.Chest);
		var chestDif = Quaternion.Inverse(chest) * prevChest;
		//sybtract out chest rotation

		//apply dif to the 
		var newLeftUpper = chestDif * Quaternion.Inverse(leftUpperRot);
		TrackedChest.transform.rotation = calibrator.GetOrientation(Imu.Chest);
		TrackedLeftUpperArm.transform.rotation = newLeftUpper;
		TrackedRightUpperArm.transform.rotation = TrackedChest.transform.rotation * Quaternion.Inverse(calibrator.GetOrientation(Imu.Right_Upper_Arm));

		prevChest = chest;
	}

	private void OnGUI()
	{
		if (GUI.Button(new Rect(new Vector2(100, 100), new Vector2(100, 50)), "Calibrate Chest")) {
			calibrator.CalibrateChest();
		}
		if (GUI.Button(new Rect(new Vector2(100, 200), new Vector2(100, 50)), "Calibrate Left"))
		{
			calibrator.CalibrateLeftArmF();
		}
	}
	class ProcessedImu
	{
		public Quaternion rawQuat;
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
	class MyCalibrator : IImuCalibrator
	{

		IDictionary<Imu, ProcessedImu> _imuMap;
		IDictionary<Imu, Quaternion> _final;
		public MyCalibrator()
		{
			var enabledImus = new List<Imu>(){ Imu.Chest, Imu.Left_Upper_Arm, Imu.Right_Upper_Arm };
			_imuMap = new Dictionary<Imu, ProcessedImu>();

			foreach (var imu in enabledImus)
			{
				_imuMap[imu] = new ProcessedImu(Quaternion.identity, imu.ToString());
			}
		}
		public Quaternion GetOrientation(Imu imu)
		{
			if (_imuMap.ContainsKey(imu))
			{
				
				return _imuMap[imu].Get();
			} else
			{
				return Quaternion.identity;
			}
		}
		public void CalibrateChest()
		{
			Quaternion forward = Quaternion.AngleAxis(0, Vector3.forward);
		
			Func<Quaternion, Quaternion> remapper = (Quaternion q) => {
				return Quaternion.Inverse(new Quaternion(q.y, q.x, -q.z, q.w));
			};
			_imuMap[Imu.Chest].remap = remapper;
			var dif = remapper(_imuMap[Imu.Chest].rawQuat) * Quaternion.Inverse(forward);
			_imuMap[Imu.Chest].transform = (Quaternion q) => { return dif * Quaternion.Inverse(q); };
		}
		//rotate about y is correct
		public void CalibrateLeftArmA()
		{
			Quaternion left = new Quaternion(0, 0, 0, 1);
			Func<Quaternion, Quaternion> remapper = (Quaternion q) => {
				return new Quaternion(-q.x, -q.z, q.y, q.w);
			};
			_imuMap[Imu.Left_Upper_Arm].remap = remapper;
			var dif = remapper(_imuMap[Imu.Left_Upper_Arm].rawQuat) * Quaternion.Inverse(left);
			_imuMap[Imu.Left_Upper_Arm].transform = (Quaternion q) => { return dif * Quaternion.Inverse(q); };
		}
		//test: inverse and L/R mnix flip x. Wrong y inverted, other two wrong
		public void CalibrateLeftArmB()
		{
			Quaternion left = new Quaternion(0, 0, 0, 1);
			Func<Quaternion, Quaternion> remapper = (Quaternion q) => {
				return Quaternion.Inverse(new Quaternion(q.x, -q.z, q.y, q.w));
			};
			_imuMap[Imu.Left_Upper_Arm].remap = remapper;
			var dif = remapper(_imuMap[Imu.Left_Upper_Arm].rawQuat) * Quaternion.Inverse(left);
			_imuMap[Imu.Left_Upper_Arm].transform = (Quaternion q) => { return dif * Quaternion.Inverse(q); };
		}
		//test invert and lr flip Y. This switched x and z compared to b
		public void CalibrateLeftArmC()
		{
			Quaternion left = new Quaternion(0, 0, 0, 1);
			Func<Quaternion, Quaternion> remapper = (Quaternion q) => {
				return Quaternion.Inverse(new Quaternion(-q.x, -q.z, -q.y, q.w));
			};
			_imuMap[Imu.Left_Upper_Arm].remap = remapper;
			var dif = remapper(_imuMap[Imu.Left_Upper_Arm].rawQuat) * Quaternion.Inverse(left);
			_imuMap[Imu.Left_Upper_Arm].transform = (Quaternion q) => { return dif * Quaternion.Inverse(q); };
		}

		//The y is now correct, but x and z are likely swapped compared to a
		public void CalibrateLeftArmD()
		{
			Quaternion left = new Quaternion(0, 0, 0, 1);
			Func<Quaternion, Quaternion> remapper = (Quaternion q) => {
				return Quaternion.Inverse(new Quaternion(-q.x, q.z, q.y, q.w));
			};
			_imuMap[Imu.Left_Upper_Arm].remap = remapper;
			var dif = remapper(_imuMap[Imu.Left_Upper_Arm].rawQuat) * Quaternion.Inverse(left);
			_imuMap[Imu.Left_Upper_Arm].transform = (Quaternion q) => { return dif * Quaternion.Inverse(q); };
		}
		//everything wrong
		public void CalibrateLeftArmE()
		{
			Quaternion left = new Quaternion(0, 0, 0, 1);
			Func<Quaternion, Quaternion> remapper = (Quaternion q) => {
				return new Quaternion(q.x, q.y, q.z, q.w);
			};
			_imuMap[Imu.Left_Upper_Arm].remap = remapper;
			var dif = remapper(_imuMap[Imu.Left_Upper_Arm].rawQuat) * Quaternion.Inverse(left);
			_imuMap[Imu.Left_Upper_Arm].transform = (Quaternion q) => { return dif * Quaternion.Inverse(q); };
		}

		public void CalibrateLeftArmF()
		{
			Quaternion left = new Quaternion(0, 0, 0, 1);
			Func<Quaternion, Quaternion> remapper = (Quaternion q) => {
				return Quaternion.Inverse(new Quaternion(q.x, q.y, q.z, q.w));
			};
			_imuMap[Imu.Left_Upper_Arm].remap = remapper;
			var dif = remapper(_imuMap[Imu.Left_Upper_Arm].rawQuat) * Quaternion.Inverse(left);
			_imuMap[Imu.Left_Upper_Arm].transform = (Quaternion q) => { return dif * Quaternion.Inverse(q); };
		}
		public void ReceiveUpdate(TrackingUpdate update)
		{
			_imuMap[Imu.Chest].rawQuat = update.Chest;
			_imuMap[Imu.Left_Upper_Arm].rawQuat = update.LeftUpperArm;
			_imuMap[Imu.Right_Upper_Arm].rawQuat = update.RightUpperArm;
		}
	}
}
