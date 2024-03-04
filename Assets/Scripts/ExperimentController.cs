using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;
using Oculus.Interaction.Locomotion;
using System;

public class ExperimentController : MonoBehaviour
{
    [Header("User Info")]
    public int participantID = 0;


    [Header("Trial Info")]
    public GameObject virtualCylinder;
    public GameObject physicalCylinder;
    public Transform targetTransform;
    public GameObject arrow;
    public GameObject rotationCue;
    public GameObject outOfViewCue;
    public GameObject ansCylinder;
    public Material ansMaterial;
    public GameObject response;
    private float ansAlpha;
    public int trialNum = 0; // 0-3, 4 trials: 2 targets times 2 self-rotations
    public int blockNum = 0; // 0-7, 8 blocks
    public float currentTime = 0;
    public enum Conditions {virtualStatic = 0, virtualRotate = 1, physicalStatic = 2, physicalRotate = 3};
    public Conditions currentCondition;
    public List<int> conditionArray;
    public enum SelfRotation {none = 0, rotate = 1};
    public SelfRotation currentRotation;
    public enum Targets {virtualTarget = 0, physicalTarget = 1};
    public Targets currentTarget;
    public GameObject StartTrialPanel;

    [Header("Pointing")]
    public GameObject controller; 
    public LayerMask pointingMask;
    public GameObject rectify;
    public GameObject circleRectify;
    public GameObject laser;
    private Transform laserTransform;
    public Vector3 hitPoint;
     
    // public GameObject uiPanel;
    // public GameObject startButton;
    // public Logger logger;
    [Header("Environment and UI")]
    public FadeEffect fadeEffect;
    public OVRPassthroughLayer passthroughLayer;
    // private variables
    public static bool isStartTrialPanelTriggered;
    public TMP_Text startTrialPanelText;
    public GameObject instruction;
    public TMP_Text instructionText;
    bool isTrialRunning;
    bool isBaselineMeasure;
    bool isTestingMeasure;
    float beginTimeStamp;
    float endTimeStamp;
    Ray ray;
    RaycastHit hit;
    float rad = 55.0f * Mathf.Deg2Rad; // Degrees-to-radians conversion constant (Read Only). This is equal to (PI * 2) / 360.
    bool fadeInRW;
    bool fadeInVR;
    float lerpTimeElapsed = 0;
    float lerpDuration = 2;
    float restingTime = 0;
    float restingInterval = 5;
    
    // for creating virtual cube positions
    List<int> signs;
    List<int> signsForPhysicalTargets;
    private int virtualCylinderCount = 0;
    private StreamWriter erfStudyWriter;
    
    private List<TrialData> dataList;
    private bool isDataSaved;
    public static float selfRotationDuration;
    private Vector3 responseVec;
    private AudioSource pointingIndicator;

    void Start()
    {
        fadeEffect = this.GetComponent<FadeEffect>();
        fadeEffect.fadeInEffect();

        // data
        string questionnairePath = GetDataPath();
        erfStudyWriter = new StreamWriter(questionnairePath, true);
        dataList = new List<TrialData>();

        conditionArray = new List<int> {0, 1, 2, 3};
        signs = new List<int> {-1, 1};

        signsForPhysicalTargets = new List<int> {-1, 1};
        Helpers.Shuffle(signsForPhysicalTargets);

        laserTransform = laser.transform;
        restingTime = 5.1f;
        // startTrialPanelText.text = "Start Trial";
        instructionText.text = "Use your right controller to touch the panel and begin.";
        pointingIndicator = GetComponent<AudioSource>();

        // initialize the first block
        Helpers.Shuffle(conditionArray);
        Helpers.Shuffle(signs);
        PrepareTrial();

        // passthroughLayer.enabled = false;
        virtualCylinder.SetActive(false);

        rectify.SetActive(false);
        circleRectify.SetActive(false);
        laser.SetActive(false);
        rotationCue.SetActive(false);
        ansCylinder.SetActive(false);
        response.SetActive(false);
        ansAlpha = 0.5f;
        ansMaterial.color = new Color(0, 1, 1, ansAlpha);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            InitializePhysicalCylinderOnArc();
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            fadeEffect.fadeInEffect();    
        }
        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            // For PC debugging
            isTrialRunning = true;
            StartCoroutine(ShowTargetAndRetention());
        }

        // core study
        FadeBehavior();

        // initial one trial
        if (isStartTrialPanelTriggered && Alignment.isCalibrated)
        {
            isTrialRunning = true;
            StartCoroutine(ShowTargetAndRetention());
            isStartTrialPanelTriggered = false;
        }

        if (rotationCue.activeSelf && RotationCueControl.isCueComplete)
        {
            rotationCue.SetActive(false);
        }


        if (blockNum < 10)
        {
            if (trialNum < 4)
            {
                if (isTrialRunning)
                {
                    currentTime += Time.deltaTime; // keep tracking time for each trial

                    if (isBaselineMeasure)
                    {
                        if (Physics.Raycast(controller.transform.position, controller.transform.forward, out hit, 200, pointingMask))
                        {
                            hitPoint = hit.point;
                            UpdateLaser(hitPoint);          
                        
                            if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch))
                            {
                                endTimeStamp = currentTime;
                                responseVec = hitPoint;
                                Debug.LogWarning(responseVec.ToString("F3"));
                                AddData();

                                if (blockNum < 2)
                                {
                                    response.SetActive(true);
                                    response.transform.position = responseVec;
                                    response.transform.LookAt(Vector3.zero);

                                    ansCylinder.SetActive(true);
                                    ansAlpha = 0.5f;
                                    ansMaterial.color = new Color(0, 1, 1, ansAlpha);
                                    ansCylinder.transform.position = targetTransform.position;
                                    ansCylinder.transform.LookAt(Vector3.zero);
                                    StartCoroutine(ShowAns(2f));
                                }
                                // reset variables
                                isBaselineMeasure = false;
                                DisableLaser();                                
                                // show rotation cue.
                                if (blockNum < 2) instructionText.text = "Look at the white circle,\nwalking in place and put your arm down."; // for rotation of static
                                StartCoroutine(ShowRotateOrientation(endTimeStamp + 5.0f));
                            }
                        }
                        else
                        {
                            DisableLaser();
                        }
                    }

                    // this is a bit random LOL. I want to keep track if there's time for rotation cue animation.
                    if (!RotationCueControl.isCueComplete) selfRotationDuration = endTimeStamp + 5.0f - currentTime;

                    if (isTestingMeasure)
                    {
                        if (Physics.Raycast(controller.transform.position, controller.transform.forward, out hit, 200, pointingMask))
                        {
                            hitPoint = hit.point;
                            UpdateLaser(hitPoint);

                            if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch))
                            {
                                endTimeStamp = currentTime;
                                responseVec = hitPoint;
                                Debug.LogWarning(responseVec.ToString("F3"));
                                AddData();


                                if (blockNum < 2)
                                {
                                    response.SetActive(true);
                                    response.transform.position = responseVec;
                                    response.transform.LookAt(Vector3.zero);

                                    ansCylinder.SetActive(true);
                                    ansAlpha = 0.5f;
                                    ansMaterial.color = new Color(0, 1, 1, ansAlpha);
                                    ansCylinder.transform.position = targetTransform.position;
                                    ansCylinder.transform.LookAt(Vector3.zero);
                                    StartCoroutine(ShowAns(2f));
                                }

                                // reset variables
                                isTestingMeasure = false;

                                DisableLaser();
                                
                                this.transform.rotation = Quaternion.Euler(0, 0, 0);

                                // arrow.SetActive(true);
                                // rotationCue.SetActive(true);

                                isTrialRunning = false;
                                restingTime = 0;

                                if (trialNum < 3) 
                                {
                                    trialNum += 1;
                                }
                                else
                                {
                                    trialNum = 0;
                                    blockNum += 1;
                                    Helpers.Shuffle(conditionArray);
                                    Helpers.Shuffle(signs);
                                }
                                PrepareTrial();

                                //////////////////////////////
                                // TODO
                                // instantiate object position if needed
                                //////////////////////////////
                                StartTrialPanel.SetActive(true);
                            }
                        }   
                        else
                        {
                            DisableLaser();
                        }        
                    }
                }
                else
                {
                    if (StartTrialPanel.activeSelf)
                    {
                        // wait for 5 seconds and show the next trial
                        restingTime += Time.deltaTime;

                        if (restingTime < restingInterval)
                        {
                            startTrialPanelText.text = UpdateProgressInfo() + "\n" + (5.4 - restingTime).ToString("F0");
                            if (blockNum < 2) instructionText.text = "Pause 5 secs, put your arm down.";
                        } 
                        else
                        {
                            startTrialPanelText.text = "Start Trial";
                            if (blockNum == 2 && trialNum == 0) instructionText.text = "Do you need more practice?\nIf not, continue by touching the start trial.";
                            else instructionText.text = "";
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("The end of the study");
            StartTrialPanel.SetActive(false);
            if (!isDataSaved)
            {
                StartCoroutine(WriteERFStudyData());
                isDataSaved = true;
                fadeEffect.fadeInBlackEffect();
            }
        }
    }

    private string UpdateProgressInfo()
    {
        if (blockNum < 2)
        {
            return (blockNum*4 + trialNum).ToString("F0") + "/8";
        }
        else
        {
            if (blockNum == 2 && trialNum == 0) return "End of Practice";
            else return ((blockNum-2)*4 + trialNum).ToString("F0") + "/32";
        }
    }

    private void FadeBehavior()
    {
        if (fadeInRW)
        {
            if (lerpTimeElapsed < lerpDuration)
            {
                passthroughLayer.textureOpacity = Mathf.Lerp(0, 1,  lerpTimeElapsed / lerpDuration);
                lerpTimeElapsed += Time.deltaTime;
            }
            else
            {
                fadeInRW = false;
            }
        }

        if (fadeInVR)
        {
            if (lerpTimeElapsed < lerpDuration)
            {
                passthroughLayer.textureOpacity = Mathf.Lerp(1, 0,  lerpTimeElapsed / lerpDuration);
                lerpTimeElapsed += Time.deltaTime;
            }
            else
            {
                fadeInVR = false;
                passthroughLayer.enabled = false;
            }
        }
    }

    IEnumerator ShowAns(float duration = 2f)
    {
        yield return new WaitForSeconds(duration);
        ansCylinder.SetActive(false);
        response.SetActive(false);
        yield return 0;
    }

    public void InitializeVirtualCylinderOnArc()
    {
        float angle = 90f - UnityEngine.Random.Range(10.0f, 45.0f) * signs[virtualCylinderCount % 2];
        float x = Alignment.calibratedDistance * Mathf.Cos(angle * Mathf.Deg2Rad);
        float z = Alignment.calibratedDistance * Mathf.Sin(angle * Mathf.Deg2Rad);
        virtualCylinder.transform.position = new Vector3(x, 0f, z);
        virtualCylinder.transform.LookAt(Vector3.zero);
        virtualCylinderCount += 1;
    }

    public void InitializePhysicalCylinderOnArc()
    {
        /// we ask participants to turn to a specific angle then reveal passthrough view,
        /// so they physical cylinder appears at different position on the arc in each trial.
        /// 1) how many degrees do we need? in which direction (left or right?)
        /// 2) rotate this to that angle

    }

    public void PrepareTrial()
    {
        if (conditionArray[trialNum] == 0) 
        {
            currentCondition = Conditions.virtualStatic;
            currentRotation = SelfRotation.none;
            currentTarget = Targets.virtualTarget;
        }
        else if (conditionArray[trialNum] == 1)
        {
            currentCondition = Conditions.virtualRotate;
            currentRotation = SelfRotation.rotate;
            currentTarget = Targets.virtualTarget;
        }
        else if (conditionArray[trialNum] == 2)
        {
            currentCondition = Conditions.physicalStatic;
            currentRotation = SelfRotation.none;
            currentTarget = Targets.physicalTarget;
        }
        else
        {
            currentCondition = Conditions.physicalRotate;
            currentRotation = SelfRotation.rotate;
            currentTarget = Targets.physicalTarget;
        }

        if (currentTarget == Targets.physicalTarget)
        {
            InitializePhysicalCylinderOnArc();
        }
        else
        {
            /// turn back to the center? or we use the current forward angle as center?
        }

    }

    public IEnumerator ShowTargetAndRetention(float _showTimeStamp = 5.0f, float _retentionTimeStamp = 10.0f)
	{
        currentTime = 0.0f;
        instructionText.text = "";
        // arrow.SetActive(false);
        StartTrialPanel.SetActive(false);
        // show a target
        if (currentTarget == Targets.virtualTarget)
        {
            virtualCylinder.SetActive(true);
            InitializeVirtualCylinderOnArc();
            targetTransform = virtualCylinder.transform;
        }
        else 
        {
            physicalCylinder.SetActive(true);
            targetTransform = physicalCylinder.transform;
            // show real-world using passthrough
            passthroughLayer.enabled = true;
            lerpTimeElapsed = 0;
            fadeInRW = true;
        } 
        yield return new WaitUntil(() => currentTime > _showTimeStamp);
        // retention starts
        if (currentTarget == Targets.virtualTarget)
        {
            virtualCylinder.SetActive(false);
        }
        else
        {
            physicalCylinder.SetActive(false);
            lerpTimeElapsed = 0;
            fadeInVR = true;
        }
        fadeEffect.fadeInBlackEffect();
        yield return new WaitUntil(() => currentTime > _retentionTimeStamp - 0.5f);

        fadeEffect.fadeInEffect(); // fade in effect takes around 0.334-0.347 secs. Therefore we save 0.5s as a buffer.

        yield return new WaitUntil(() => currentTime > _retentionTimeStamp);
        // prepare for the pointing task
        beginTimeStamp = currentTime;
        pointingIndicator.Play();
        rectify.SetActive(true);
        circleRectify.SetActive(true);
        if (blockNum < 2) instructionText.text = "Once see the raycast, point\nto the target and confirm with Button A.";

        laser.SetActive(true);
        isBaselineMeasure = true;
        yield return 0;
    }

    public IEnumerator ShowRotateOrientation(float _showTimeStamp)
    {
        // arrow.SetActive(true);
        if (currentRotation == SelfRotation.none)
        {
            // RotationCueControl.isCueComplete = true; // I hard coded this ... since we don't need to show cue for static condition.
            rotationCue.SetActive(true);
            outOfViewCue.SetActive(false);
            // this.transform.LookAt(new Vector3(targetTransform.position.x, 0, targetTransform.position.z));
            this.transform.rotation = Quaternion.Euler(0, 0, 0); 
        }
        else
        {
            rotationCue.SetActive(true);
            this.transform.LookAt(new Vector3(targetTransform.position.x, 0, targetTransform.position.z));
            this.transform.rotation = Quaternion.Euler(0, this.transform.rotation.eulerAngles.y + 120, 0);
            outOfViewCue.SetActive(true);

            // Sets the transform's current rotation to a new rotation that rotates 120 degrees around the y-axis(Vector3.up)
            // this.transform.rotation = Quaternion.AngleAxis(120, Vector3.up);
        }

        yield return new WaitUntil(() => currentTime > _showTimeStamp && RotationCueControl.isCueComplete);
        beginTimeStamp = currentTime;
        // prepare for the pointing task
        // arrow.SetActive(false);
        RotationCueControl.isCueComplete = false;
        rotationCue.SetActive(false);

        pointingIndicator.Play();
        rectify.SetActive(true);
        circleRectify.SetActive(true);
        laser.SetActive(true);
        if (blockNum < 2) instructionText.text = "Once see the raycast, point\nto the target and confirm with Button A.";
        isTestingMeasure = true;
        yield return 0;
    }

    void DisableLaser()
    {
        // make sure they won't be visible directly when displaying 
        if (rectify.activeSelf) rectify.transform.position = Vector3.zero;
        if (laser.activeSelf) laser.transform.position = Vector3.zero;
        laser.SetActive(false);
        rectify.SetActive(false);
    }

    private void UpdateLaser(Vector3 hitPoint)
    {
        laser.SetActive(true);
        rectify.SetActive(true);
        rectify.transform.position = hitPoint;
        rectify.transform.LookAt(Vector3.zero);
        laserTransform.position = Vector3.Lerp(controller.transform.position, hitPoint, .5f); // move laser to the middle
        laserTransform.LookAt(hitPoint); // rotate and face the hit point
        laserTransform.localScale = new Vector3(laserTransform.localScale.x, laserTransform.localScale.y, Vector3.Distance(controller.transform.position, hitPoint));
    }

    IEnumerator WriteERFStudyData()
    {
        erfStudyWriter.Write("Participant"    + ","+
                        "Block"               + ","+
                        "Trial"               + ","+
                        "Condition"           + ","+
                        "Self-Rotation"       + ","+
                        "TargetType"          + ","+
                        "Baseline"            + ","+
                        "BeginTime"           + ","+
                        "EndTime"             + ","+
                        "TargetPos_X"         + ","+
                        "TargetPos_Y"         + ","+
                        "TargetPos_Z"         + ","+
                        "SelectedPos_X"       + ","+
                        "SelectedPos_Y"       + ","+
                        "SelectedPos_Z"       + ","+
                        "ControllerPos_X"     + ","+
                        "ControllerPos_Y"     + ","+
                        "ControllerPos_Z"     + "\n");
        foreach (var data in dataList)
        {
            erfStudyWriter.Write("P" + data.particiapntID.ToString()          + "," +
                                 data.blockNum.ToString()                     + "," +
                                 data.trialNum.ToString()                     + "," +
                                 data.currentCondition.ToString()             + "," +
                                 data.currentRotation.ToString()              + "," +
                                 data.currentTarget.ToString()                + "," +
                                 data.isBaselineMeasure.ToString()            + "," +
                                 data.beginTime.ToString("F6")                + "," +
                                 data.endTime.ToString("F6")                  + "," +
                                 data.targetPos.x.ToString("F6")              + "," +
                                 data.targetPos.y.ToString("F6")              + "," +
                                 data.targetPos.z.ToString("F6")              + "," +
                                 data.selectedPos.x.ToString("F6")            + "," +
                                 data.selectedPos.y.ToString("F6")            + "," +
                                 data.selectedPos.z.ToString("F6")            + "," +
                                 data.controllerPos.x.ToString("F6")          + "," +
                                 data.controllerPos.y.ToString("F6")          + "," +
                                 data.controllerPos.z.ToString("F6")          + "\n");
        }
        erfStudyWriter.Flush();
        erfStudyWriter.Close();

        // Change Scene
        SceneManager.LoadScene("Questionnaire");
        yield return 0;
    }

    private void AddData()
    {
        dataList.Add(new TrialData(participantID.ToString(), 
                            blockNum,
                            trialNum,
                            currentCondition.ToString(),
                            currentRotation.ToString(),
                            currentTarget.ToString(),
                            isBaselineMeasure,
                            beginTimeStamp,
                            endTimeStamp,
                            targetTransform.position,
                            responseVec,
                            controller.transform.position));
        // record baseline measure (two time stamps, target position, selected position)
        Debug.LogWarning("Participant: P" +    participantID.ToString()                + ", " +
                        "Block: " +            blockNum                                + ", " +
                        "Trial: " +            trialNum                                + ", " +
                        "Condition: " +        currentCondition.ToString()             + ", " +
                        "Self-Rotation: " +    currentRotation.ToString()              + ", " +
                        "TargetType: " +       currentTarget.ToString()                + ", " +
                        "Baseline: " +         isBaselineMeasure                       + ", " +
                        "BeginTime: " +        beginTimeStamp.ToString("F6")           + ", " +
                        "EndTime: " +          endTimeStamp.ToString("F6")             + ", " +
                        "TargetPos: " +        targetTransform.position.ToString("F6") + ", " +
                        "SelectedPos: " +      hitPoint.ToString("F6")                 + ", " +
                        "ControllerPos: " +    controller.transform.position.ToString("F6")   + ", "); 
    }

    public struct TrialData
    {
        public string particiapntID;
        public int blockNum;
        public int trialNum;
        public string currentCondition;
        public string currentRotation;
        public string currentTarget;
        public bool isBaselineMeasure;
        public float beginTime;
        public float endTime;
        public Vector3 targetPos;
        public Vector3 selectedPos;
        public Vector3 controllerPos;

        public TrialData(string particiapntID, int blockNum, int trialNum, string currentCondition, string currentRotation, string currentTarget, bool isBaselineMeasure,
                         float beginTime, float endTime, Vector3 targetPos, Vector3 selectedPos, Vector3 controllerPos)
        {
            this.particiapntID = particiapntID;
            this.blockNum = blockNum; 
            this.trialNum = trialNum;
            this.currentCondition = currentCondition;
            this.currentRotation = currentRotation;
            this.currentTarget = currentTarget;
            this.isBaselineMeasure = isBaselineMeasure; 
            this.beginTime = beginTime;
            this.endTime = endTime;
            this.targetPos = targetPos;
            this.selectedPos = selectedPos;
            this.controllerPos = controllerPos;
        }
    }

    private string GetDataPath()
    {
        string fileName = "P" + participantID.ToString() + "_erf.csv";
#if UNITY_EDITOR
        return Application.dataPath + "/Data/" + fileName;
#elif UNITY_ANDROID
        return Application.persistentDataPath + fileName;
#elif UNITY_IPHONE
        return Application.persistentDataPath + "/" + fileName;
#else
        return Application.dataPath + "/" + fileName;
#endif
    }
}

public static class Helpers
{
    public static void Shuffle<T>(this IList<T> list)
    {
        // https://forum.unity.com/threads/randomize-array-in-c.86871/
        // https://stackoverflow.com/questions/273313/randomize-a-listt
        // Knuth shuffle algorithm :: courtesy of Wikipedia :)
        for (int n = 0; n < list.Count; n++)
        {
            T tmp = list[n];
            int r = UnityEngine.Random.Range(n, list.Count);
            list[n] = list[r];
            list[r] = tmp;
        }


    }

    public static float RandomGaussian(float minValue = 0.0f, float maxValue = 1.0f)
    {
        //https://discussions.unity.com/t/normal-distribution-random/66530/4
        float u, v, S;

        do
        {
            u = 2.0f * UnityEngine.Random.value - 1.0f;
            v = 2.0f * UnityEngine.Random.value - 1.0f;
            S = u * u + v * v;
        }
        while (S >= 1.0f);

        // Standard Normal Distribution
        float std = u * Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);

        // Normal Distribution centered between the min and max value
        // and clamped following the "three-sigma rule"
        float mean = (minValue + maxValue) / 2.0f;
        float sigma = (maxValue - mean) / 3.0f;
        return Mathf.Clamp(std * sigma + mean, minValue, maxValue);
    }

    public static float DegreeToRadian(float deg)
    {
        return deg * Mathf.PI / 180;
    }
}