using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using UnityEditor;

[StructLayout(LayoutKind.Sequential)]
public struct Skel
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
    //[FieldOffset(0)]
    public float[] confidence;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
    //[FieldOffset(0)]
    public float[] x;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
    //[FieldOffset(0)]
    public float[] y;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
    //[FieldOffset(0)]
    public float[] z;
}

/*[System.Serializable]
class ModelJoints
{
    public Transform bone;
    public nuitrack.JointType jointType;
    [HideInInspector] public Quaternion baseRotOffset;
}*/

public class SetRotations : MonoBehaviour {

    // Joint body angles values
	public float neck_bending = 0, neck_twist = 0, neck_side_bending = 0;
	public float trunk_bending = 0, trunk_twist = 0, trunk_side_bending = 0;
	public float upper_right_leg_flexion = 0, upper_right_leg_abduction = 0, upper_right_leg_rotation = 0;
	public float upper_left_leg_flexion = 0, upper_left_leg_abduction = 0, upper_left_leg_rotation = 0;
	public float lower_left_leg_flexion = 0, lower_right_leg_flexion = 0;
	public float left_foot_flexion = 0, left_foot_twist = 0, left_foot_bending = 0;
	public float right_foot_flexion = 0, right_foot_twist = 0, right_foot_bending = 0;
	public float upper_right_arm_raise = 0, upper_right_arm_flexion = 0, upper_right_arm_abduction = 0, upper_right_arm_rotation = 0;
	public float upper_left_arm_raise = 0, upper_left_arm_flexion = 0, upper_left_arm_abduction = 0, upper_left_arm_rotation = 0;
	public float lower_left_arm_flexion = 0, lower_right_arm_flexion = 0;
	public float left_hand_flexion = 0, left_hand_twist = 0, left_hand_bending = 0;
	public float right_hand_flexion = 0, right_hand_twist = 0, right_hand_bending = 0;


	public GameObject skeletonPlayer;
    private GameObject neck, trunk, upper_right_leg, upper_left_leg, upper_right_arm, upper_left_arm, right_shoulder, left_shoulder, lower_left_leg, lower_right_leg, left_foot, right_foot;
    private GameObject lower_right_arm, lower_left_arm, left_hand, right_hand;
	public GameObject ground;
    public GameObject directionalLightObject;

	public Text reba_score, reba_suppl;

	private Light directionalLight;

	private GameObject maleSkeleton, femaleSkeleton;

	public Button buttonModel;
	Button btnModel;

	public Button buttonExit;
	Button btnExit;

	public AudioSource alarmSource;

	Thread thread;
	static UdpClient udp;

	static readonly object lockObject = new object();
	string returnData = "";
	bool precessData = false;

	const int max_reba = 3;

	int[] reba = {1,1,1,1};

	int [, ,] tableA = new int[,,]{ {{1, 2, 3, 4},{2, 3, 4, 5},{2, 4, 5, 6},{3, 5, 6, 7},{4, 6, 7, 8}} ,
	{{1, 2, 3, 4},{3, 4, 5, 6},{4, 5, 6, 7},{5, 6, 7, 8},{6, 7, 8, 9}} , {{3, 3, 5, 6},{4, 5, 6, 7},{5, 6, 7, 8},{6, 7, 8, 9},{7, 7, 8, 9}} };

	int [, ,] tableB = new int[,,]{ {{1, 2, 2},{1, 2, 3},{3, 4, 5},{4, 5, 5},{6, 7, 8},{7,8,8}} ,
	{{1, 2, 3},{2, 3, 4},{4, 5, 5},{5, 6, 7},{7, 8, 8},{8, 9, 9}} };

	int [,] tableC = new int [,] {{1, 1, 1, 2, 3, 3, 4, 5, 6, 7, 7, 7},{1, 2, 2, 3, 4, 4, 5, 6, 6, 7, 7, 8},{2, 3, 3, 3, 4, 5, 6, 7, 7, 8, 8, 8},
	{3, 4, 4, 4, 5, 6, 7, 8, 8, 9, 9, 9},{4, 4, 4, 5, 6, 7, 8, 8, 9, 9, 9, 9},{6, 6, 6, 7, 8, 8, 9, 9, 10, 10, 10, 10},{7, 7, 7, 8, 9, 9, 9, 10, 10, 11, 11, 11},
	{8, 8, 8, 9, 10, 10, 10, 10, 10, 11, 11, 11},{9, 9, 9, 10, 10, 10, 11, 11, 11, 12, 12, 12},{10, 10, 10, 11, 11, 11, 11, 12, 12, 12, 12, 12},
	{11, 11, 11, 11, 12,12, 12, 12, 12, 12, 12, 12},{12, 12, 12, 12, 12,12, 12, 12, 12, 12, 12, 12}};

	String message;

	private Skel skel;

	public nuitrack.JointType[] typeJoint;
	GameObject[] CreatedJoint;
	public GameObject PrefabJoint;

    private float[,] init_pos = new float [3,3];
    private bool first_time;
    private Vector3 unity_hand_pos;
    private Vector3 unity_elbow_pos;
    private Vector3 unity_shoulder_pos;

    public Material highlight;

    private new SkinnedMeshRenderer renderer;

    [Header("Rigged model")]
    [SerializeField]
    ModelJoint[] modelJoints;

    Dictionary<nuitrack.JointType, ModelJoint> jointsRigged = new Dictionary<nuitrack.JointType, ModelJoint>();

	// Use this for initialization
	void Start () {

		directionalLight = directionalLightObject.GetComponent<Light>();

		maleSkeleton = GameObject.FindWithTag("mesh_obj");
		femaleSkeleton = GameObject.FindWithTag("meshes");

		maleSkeleton.SetActive(false);

		btnModel = buttonModel.GetComponent<Button>();
		btnExit = buttonExit.GetComponent<Button>();

		btnModel.onClick.AddListener(ChangeModel);
		btnExit.onClick.AddListener(doExit);

		alarmSource = GetComponent<AudioSource> ();

	    //string path = "Assets/Resources/angles.txt";
		string path = Application.streamingAssetsPath + "/angles.txt";
		string line;
		float[] angles = new float[36];
	    //Read the text from directly from the test.txt file
	    StreamReader reader = new StreamReader(path);
		int i = 0, k = 0;
		while ((line = reader.ReadLine()) != null)
		{
			if (i%2 != 0)
			{
				angles[k] = float.Parse(line);
				k++;
			}
			i++;
		}
	    reader.Close();

		neck_bending = angles[0];
		neck_twist = angles[1];
		neck_side_bending = angles[2];
		trunk_bending = angles[3];
		trunk_twist = angles[4];
		trunk_side_bending = angles[5];
		upper_right_leg_flexion = angles[6];
		upper_right_leg_abduction = angles[7];
		upper_right_leg_rotation = angles[8];
		upper_left_leg_flexion = angles[9];
		upper_left_leg_abduction = angles[10];
		upper_left_leg_rotation = angles[11];
		lower_left_leg_flexion = angles[12];
		lower_right_leg_flexion = angles[13];
		left_foot_flexion = angles[14];
		left_foot_twist = angles[15];
		left_foot_bending = angles[16];
		right_foot_flexion = angles[17];
		right_foot_twist = angles[18];
		right_foot_bending = angles[19];
		upper_right_arm_raise = angles[20];
		upper_right_arm_flexion = angles[21];
		upper_right_arm_abduction = angles[22];
		upper_right_arm_rotation = angles[23];
		upper_left_arm_raise = angles[24];
		upper_left_arm_flexion = angles[25];
		upper_left_arm_abduction = angles[26];
		upper_left_arm_rotation = angles[27];
		lower_left_arm_flexion = angles[28];
		lower_right_arm_flexion = angles[29];
		left_hand_flexion = angles[30];
		left_hand_twist = angles[31];
		left_hand_bending = angles[32];
		right_hand_flexion = angles[33];
		right_hand_twist = angles[34];
		right_hand_bending = angles[35];

		neck = GameObject.FindWithTag("Neck");
		trunk = GameObject.FindWithTag("Trunk");
		upper_right_leg = GameObject.FindWithTag("Upper_Right_Leg");
		upper_left_leg = GameObject.FindWithTag("Upper_Left_Leg");
		lower_right_leg = GameObject.FindWithTag("Lower_Right_Leg");
		lower_left_leg = GameObject.FindWithTag("Lower_Left_Leg");
		left_foot = GameObject.FindWithTag("Left_Foot");
		right_foot = GameObject.FindWithTag("Right_Foot");
		right_shoulder = GameObject.FindWithTag("Right_Shoulder");
		left_shoulder = GameObject.FindWithTag("Left_Shoulder");
		upper_right_arm = GameObject.FindWithTag("Upper_Right_Arm");
		upper_left_arm = GameObject.FindWithTag("Upper_Left_Arm");
		lower_right_arm = GameObject.FindWithTag("Lower_Right_Arm");
		lower_left_arm = GameObject.FindWithTag("Lower_Left_Arm");
		left_hand = GameObject.FindWithTag("Left_Hand");
		right_hand = GameObject.FindWithTag("Right_Hand");

		neck.transform.Rotate(neck_bending, neck_twist, neck_side_bending, Space.Self);
		trunk.transform.Rotate(trunk_bending, trunk_twist, trunk_side_bending, Space.Self);
		upper_right_leg.transform.Rotate(-upper_right_leg_flexion,upper_right_leg_rotation,upper_right_leg_abduction, Space.Self);
		upper_left_leg.transform.Rotate(-upper_left_leg_flexion,-upper_left_leg_rotation,-upper_left_leg_abduction, Space.Self);
		upper_right_arm.transform.Rotate(-upper_right_arm_rotation,-upper_right_arm_flexion,upper_right_arm_abduction,Space.Self);
		upper_left_arm.transform.Rotate(-upper_left_arm_rotation,upper_left_arm_flexion,-upper_left_arm_abduction, Space.Self);
		lower_right_leg.transform.Rotate(lower_right_leg_flexion, 0.0f, 0.0f, Space.Self);
		lower_left_leg.transform.Rotate(lower_left_leg_flexion, 0.0f, 0.0f, Space.Self);
		left_foot.transform.Rotate(left_foot_flexion, -left_foot_twist, -left_foot_bending, Space.Self);
		right_foot.transform.Rotate(right_foot_flexion, right_foot_twist, right_foot_bending, Space.Self);

		if (upper_right_arm_raise > 0.05)
			upper_right_arm_raise = 0.05f;
		if (upper_right_arm_raise < -0.02)
			upper_right_arm_raise = -0.02f;
		if (upper_left_arm_raise > 0.05)
			upper_left_arm_raise = 0.05f;
		if (upper_left_arm_raise < -0.02)
			upper_left_arm_raise = -0.02f;

		right_shoulder.transform.Translate(0.0f, upper_right_arm_raise, 0.0f, Space.Self);
		left_shoulder.transform.Translate(0.0f, upper_left_arm_raise, 0.0f, Space.Self);
		lower_right_arm.transform.Rotate(0.0f, -lower_right_arm_flexion, 0.0f, Space.Self);
		lower_left_arm.transform.Rotate(0.0f, lower_left_arm_flexion, 0.0f, Space.Self);
		left_hand.transform.Rotate(-left_hand_twist, -left_hand_bending, left_hand_flexion, Space.Self);
		right_hand.transform.Rotate(right_hand_twist, right_hand_bending, -right_hand_flexion, Space.Self);

		reba = find_reba();

		updateEnvironment();

		thread = new Thread(new ThreadStart(ThreadMethod));
		thread.Start();

		message = "";

        first_time = true;

        unity_hand_pos = right_hand.transform.position;
        unity_elbow_pos = lower_right_arm.transform.position;
        unity_shoulder_pos = right_shoulder.transform.position;

        renderer = GameObject.FindWithTag("beta_joints_mesh").GetComponent<SkinnedMeshRenderer>();

        for (int j = 0; j < modelJoints.Length; j++)
        {
            modelJoints[j].baseRotOffset = modelJoints[j].bone.rotation;
            jointsRigged.Add(modelJoints[j].jointType.TryGetMirrored(), modelJoints[j]);
        }
	}

	// Update is called once per frame
	void Update () {
        /*Material[] mats = renderer.materials;
        mats[0] = highlight;
        renderer.materials = mats;*/
		if (precessData)
		{
			/*lock object to make sure there data is
			 *not being accessed from multiple threads at thesame time*/
			lock (lockObject)
			{
				precessData = false;

				//Process received data
				//Debug.Log("Received: " + returnData);

				//Debug.Log(skel.x[15]);
				float[,] pos = new float [,] {{skel.x[14], skel.y[14], skel.z[14]},{skel.x[13], skel.y[13], skel.z[13]},{skel.x[12], skel.y[12], skel.z[12]}};
				right_hand.transform.position = new Vector3(unity_hand_pos.x+(pos[0,0]-init_pos[0,0])*0.001f,unity_hand_pos.y+(pos[0,1]-init_pos[0,1])*0.001f,unity_hand_pos.z+(pos[0,2]-init_pos[0,2])*0.001f);
                lower_right_arm.transform.position = new Vector3(unity_elbow_pos.x+(pos[1,0]-init_pos[1,0])*0.001f,unity_elbow_pos.y+(pos[1,1]-init_pos[1,1])*0.001f,unity_elbow_pos.z+(pos[1,2]-init_pos[1,2])*0.001f);
                right_shoulder.transform.position = new Vector3(unity_shoulder_pos.x+(pos[2,0]-init_pos[2,0])*0.001f,unity_shoulder_pos.y+(pos[2,1]-init_pos[2,1])*0.001f,unity_shoulder_pos.z+(pos[2,2]-init_pos[2,2])*0.001f);

				//Reset it for next read(OPTIONAL)
				//returnData = "";
			}
		}

        if (CurrentUserTracker.CurrentSkeleton != null) ProcessSkeleton(CurrentUserTracker.CurrentSkeleton);
		/*if (CurrentUserTracker.CurrentUser != 0)
		{
				nuitrack.Skeleton skeleton = CurrentUserTracker.CurrentSkeleton;
				message = "Skeleton found";

				for (int q = 0; q < typeJoint.Length; q++)using System.Collections.Generic;
				{
						nuitrack.Joint joint = skeleton.GetJoint(typeJoint[q]);
						Vector3 newPosition = 0.001f * joint.ToVector3();
                        if(q == 15)
                            right_hand.transform.localPosition = new Vector3(unity_hand_pos.z+(newPosition.z - init_pos[0,2]),unity_hand_pos.y+(newPosition.y - init_pos[0,1]),-unity_hand_pos.x-(newPosition.x - init_pos[0,0]));
				}
		}
		else
		{
				message = "Skeleton not found";
		}*/

	}

    void ProcessSkeleton(nuitrack.Skeleton skeleton)
    {
        //Vector3 torsoPos = Quaternion.Euler(0f, 180f, 0f) * (0.001f * skeleton.GetJoint(nuitrack.JointType.Torso).ToVector3());
        //transform.position = new Vector3(torsoPos.x,torsoPos.y,-torsoPos.z);

        foreach (var riggedJoint in jointsRigged)
        {
            nuitrack.Joint joint = skeleton.GetJoint(riggedJoint.Key);
            ModelJoint modelJoint = riggedJoint.Value;
            Quaternion jointOrient = Quaternion.Inverse(CalibrationInfo.SensorOrientation) * (joint.ToQuaternion()) *modelJoint.baseRotOffset;
            modelJoint.bone.rotation = jointOrient;
        }
    }

	// Display the message on the screen
	void OnGUI()
	{
			GUI.color = Color.red;
			GUI.skin.label.fontSize = 50;
			GUILayout.Label(message);
	}

	private int[] find_reba(){
		int [] score = {1,1,1,1};
		int reba_value = 1;
		int scoreA = 1, scoreB_r = 1, scoreB_l = 1, scoreB = 1, scoreC = 1;

		int n = 1;
		if(neck_bending > 20 || neck_bending < -10)
			n = 2;
		if(neck_side_bending > 30 || neck_side_bending < -30 || neck_twist > 30 || neck_twist < -30)
			n += 1;

		int t = 1;
		if(trunk_bending < -10 || (trunk_bending > 5 && trunk_bending <= 20))
			t = 2;
		if(trunk_bending > 20 && trunk_bending <= 60)
			t = 3;
		if(trunk_bending > 60)
			t = 4;

		int l = 1;

		scoreA = tableA[n-1,t-1,l-1];

		int ua_r = 1;
		if(upper_right_arm_flexion > 20 && upper_right_arm_flexion <= 45)
			ua_r = 2;
		if(upper_right_arm_flexion > 45 && upper_right_arm_flexion <= 90)
			ua_r = 3;
		if(upper_right_arm_flexion > 90)
			ua_r = 4;
		if(upper_right_arm_abduction > 30)
			ua_r += 1;
		if(upper_right_arm_raise > 0.02)
			ua_r += 1;

		int la_r = 1;
		if(lower_right_arm_flexion <= 60 || lower_right_arm_flexion >= 100)
			la_r = 2;

		int w_r = 1;
		if(right_hand_flexion > 15 || right_hand_flexion < -15)
			w_r = 2;
		if(right_hand_twist > 30 || right_hand_twist < -30 || right_hand_bending > 30 || right_hand_bending < -30)
			w_r += 1;

		scoreB_r = tableB[la_r-1,ua_r-1,w_r-1];

		int ua_l = 1;
		if(upper_left_arm_flexion > 20 && upper_left_arm_flexion <= 45)
			ua_l = 2;
		if(upper_left_arm_flexion > 45 && upper_left_arm_flexion <= 90)
			ua_l = 3;
		if(upper_left_arm_flexion > 90)
			ua_l = 4;
		if(upper_left_arm_abduction > 30)
			ua_l += 1;
		if(upper_left_arm_raise > 0.02)
			ua_l += 1;

		int la_l = 1;
		if(lower_left_arm_flexion <= 60 || lower_left_arm_flexion >= 100)
			la_l = 2;

		int w_l = 1;
		if(left_hand_flexion > 15 || left_hand_flexion < -15)
			w_l = 2;
		if(left_hand_twist > 30 || left_hand_twist < -30 || left_hand_bending > 30 || left_hand_bending < -30)
			w_l += 1;

		scoreB_l = tableB[la_l-1,ua_l-1,w_l-1];

		scoreB = Mathf.Max(scoreB_l,scoreB_r);

		scoreC = tableC[scoreA-1,scoreB-1];

		reba_value = scoreC;

		score[0] = reba_value;
		score[1] = scoreA;
		score[2] = scoreB;
		score[3] = scoreC;

		return score;
	}

	private void changeLight(Color color)
	{
		directionalLight.color = color;
	}

	private void changeText()
	{
		reba_score.text = "Reba score: " + reba[0].ToString();
		reba_suppl.text = "Score A = " + reba[1].ToString() + "\n" + "Score B = " + reba[2].ToString() + "\n" + "Score C = " + reba[3].ToString() ;
	}

	private void ChangeModel()
  {
	  maleSkeleton.SetActive(!maleSkeleton.active);
      femaleSkeleton.SetActive(!femaleSkeleton.active);

		Text bModeltext = btnModel.GetComponentInChildren<Text>();
		if(maleSkeleton.active == true)
			bModeltext.text = "Male";
		else
			bModeltext.text = "Female";
  }

	private void doExit() {
        Application.Quit();
    }

	private void doBeep()
	{
		alarmSource.Play();
		//EditorApplication.Beep();
	}

	private void changeTextColor(Color color)
	{
		reba_score.color = color;
		reba_suppl.color = color;
	}

	private void updateEnvironment()
	{
		changeText();
		if(reba[0] >= max_reba)
		{
			changeLight(Color.red);
			changeTextColor(Color.red);
			doBeep();
		}
		else
		{
			changeLight(Color.white);
			changeTextColor(Color.white);
		}
	}

	private void ThreadMethod()
	{
	    udp = new UdpClient(11311);
	    while (true)
	    {
	        IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

			try
			{
				if (udp.Available > 0) // Only read if we have some data
				{
			        byte[] receiveBytes = udp.Receive(ref RemoteIpEndPoint);

			        /*lock object to make sure there data is
			        *not being accessed from multiple threads at thesame time*/
			        lock (lockObject)
			        {
								  GCHandle handle = GCHandle.Alloc(receiveBytes, GCHandleType.Pinned);
									try
							    {
							        skel = (Skel)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(Skel));
											precessData = true;

                      if(first_time == true)
                      {

                        nuitrack.Skeleton skeleton = CurrentUserTracker.CurrentSkeleton;
        				nuitrack.Joint joint = skeleton.GetJoint(typeJoint[15]);
        				Vector3 newPosition = 0.001f * joint.ToVector3();

                        init_pos[0,0] = newPosition.x;
                        init_pos[0,1] = newPosition.y;
                        init_pos[0,2] = newPosition.z;

                        first_time = false;
                      }
							    }
							    finally
							    {
							        handle.Free();
							    }

									//Debug.Log("right hand pos x = " + skel.x[15] + ", right hand pos y = " + skel.y[15] + ", right hand pos z = " + skel.z[15]);
									//right_hand.transform.Translate(skel.x[15]+200, 0.0f, 0.0f, Space.Self);
								  //int BufferSize = Marshal.SizeOf(typeof(Skeleton));
								  //NewStuff stuff = (NewStuff)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(NewStuff));
			            //returnData = Encoding.ASCII.GetString(receiveBytes);

			            /*if (returnData == "1\n")
			            {
			                //Done, notify the Update function
			                precessData = true;
			            }*/
			        }
				}
			}
			catch(Exception e)
			{

			}
	    }
	}

	void OnApplicationQuit()
	{
		udp.Close();
		thread.Abort();
	}

}
