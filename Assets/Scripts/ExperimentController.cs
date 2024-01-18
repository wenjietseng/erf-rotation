using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Oculus.Interaction.Locomotion;
using Unity.VisualScripting;
using DataFileIO;

public class ExperimentController : MonoBehaviour
{
    [Header("User Info")]
    public int participantID = 0;
    WriteFile dataFile; 


    [Header("Trial Info")]
    public GameObject virtualCube;
    public GameObject physicalCylinder;
    public Transform targetTransform;
    public GameObject arrow;
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
    public GameObject virtualRectify;
    public GameObject physicalRectify;
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

    void Start()
    {
        // Data file
        dataFile = new WriteFile();
        





        passthroughLayer.enabled = false;
        fadeEffect = this.GetComponent<FadeEffect>();
        fadeEffect.fadeInEffect();
        conditionArray = new List<int> {0, 1, 2, 3};
        laserTransform = laser.transform;
        restingTime = 5.1f;
        startTrialPanelText.text = "Start Trial";

        // initialize the first block
        InitializeVirtualCubePos();
        Helpers.Shuffle(conditionArray);
        PrepareTrial();

        virtualCube.SetActive(false);
        physicalCylinder.SetActive(false);
        rectify.SetActive(false);
        virtualRectify.SetActive(false);
        physicalRectify.SetActive(false);
        laser.SetActive(false);
    }

    void Update()
    {


        if (Input.GetKeyDown(KeyCode.Q))
        {
            fadeEffect.fadeInBlackEffect();
        }
        if (Input.GetKeyDown(KeyCode.W)) fadeEffect.fadeInEffect();

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


        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            // For PC debugging
            isTrialRunning = true;
            StartCoroutine(ShowTargetAndRetention());
        }

        if (isStartTrialPanelTriggered)
        {
            isTrialRunning = true;
            StartCoroutine(ShowTargetAndRetention());
            isStartTrialPanelTriggered = false;
        }



        if (blockNum < 8)
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
                                // record baseline measure (two time stamps, target position, selected position)
                                Debug.LogWarning("Participant: P" +    participantID.ToString()                + ", " +
                                                "Block: " +            blockNum                                + ", " +
                                                "Trial: " +            trialNum                                + ", " +
                                                "Self-Rotation: " +    currentRotation.ToString()              + ", " +
                                                "TargetType: " +       currentTarget.ToString()                + ", " +
                                                "Baseline: " +         isBaselineMeasure                       + ", " +
                                                "BeginTime: " +        beginTimeStamp.ToString("F6")           + ", " +
                                                "EndTime: " +          endTimeStamp.ToString("F6")             + ", " +
                                                "TargetPos: " +        targetTransform.position.ToString("F6") + ", " +
                                                "SelectedPos: " +      hitPoint.ToString("F6")                 + ", " +
                                                "ControllerPos: " +    controller.transform.position.ToString("F6")   + ", "); // change to controller
                                // reset variables
                                isBaselineMeasure = false;


                                DisableLaser();
                                
                                // show arrow
                                StartCoroutine(ShowRotateOrientation(endTimeStamp + 5.0f));
                            }
                        }
                        else
                        {
                            DisableLaser();
                        }
                    }

                    if (isTestingMeasure)
                    {
                        if (Physics.Raycast(controller.transform.position, controller.transform.forward, out hit, 200, pointingMask))
                        {
                            hitPoint = hit.point;
                            UpdateLaser(hitPoint);

                            if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch))
                            {
                                endTimeStamp = currentTime;
                                // record baseline measure (two time stamps, target position, selected position)
                                Debug.LogWarning("Participant: P" +    participantID.ToString()                + ", " +
                                                "Block: " +            blockNum                                + ", " +
                                                "Trial: " +            trialNum                                + ", " +
                                                "Self-Rotation: " +    currentRotation.ToString()              + ", " +
                                                "TargetType: " +       currentTarget.ToString()                + ", " +
                                                "Baseline: " +         isBaselineMeasure                       + ", " +
                                                "BeginTime: " +        beginTimeStamp.ToString("F6")           + ", " +
                                                "EndTime: " +          endTimeStamp.ToString("F6")             + ", " +
                                                "TargetPos: " +        targetTransform.position.ToString("F6") + ", " +
                                                "SelectedPos: " +      hitPoint.ToString("F6")                 + ", " +
                                                "ControllerPos: " +    controller.transform.position.ToString("F6")   + ", "); // change to controller
                                // reset variables
                                isTestingMeasure = false;

                                DisableLaser();
                                
                                this.transform.rotation = Quaternion.Euler(0, 0, 0);
                                arrow.SetActive(true);
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
                                }
                                InitializeVirtualCubePos();
                                //////////////////////////////
                                // TODO
                                // instantiate object position if needed
                                //////////////////////////////
                                PrepareTrial();
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
                            startTrialPanelText.text = (5.4 - restingTime).ToString("F0");
                        }
                        else
                        {
                            startTrialPanelText.text = "Start Trial";
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("The end of the study");
        }
    }

    public void InitializeVirtualCubePos()
    {
        virtualCube.SetActive(true);
        float z = Random.Range(1.0f, 3.0f);
        float x_half = z * Mathf.Tan(rad);
        float x = Random.Range(-x_half, x_half);
        virtualCube.transform.position = new Vector3(x, 0.05f, z);
        virtualCube.SetActive(false);
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
    }

    public IEnumerator ShowRotateOrientation(float _showTimeStamp)
    {
        if (currentRotation == SelfRotation.none)
        {
            this.transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            this.transform.rotation = Quaternion.Euler(0, 120, 0);
        }
        arrow.SetActive(true);
        yield return new WaitUntil(() => currentTime > _showTimeStamp);
        // prepare for the pointing task
        arrow.SetActive(false);
        rectify.SetActive(true);
        if (currentTarget == Targets.virtualTarget)
        {
            virtualRectify.SetActive(true);
            physicalRectify.SetActive(false);
        }
        else
        {
            virtualRectify.SetActive(false);
            physicalRectify.SetActive(true);
        }
        laser.SetActive(true);
        isTestingMeasure = true;
        beginTimeStamp = currentTime;
        yield return 0;
    }

    public IEnumerator ShowTargetAndRetention(float _showTimeStamp = 5.0f, float _retentionTimeStamp = 10.0f)
	{
        currentTime = 0.0f;
        arrow.SetActive(false);
        StartTrialPanel.SetActive(false);
        // show a target
        if (currentTarget == Targets.virtualTarget)
        {
            virtualCube.SetActive(true);
            targetTransform = virtualCube.transform;
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
            virtualCube.SetActive(false);
        }
        else
        {
            physicalCylinder.SetActive(false);
            lerpTimeElapsed = 0;
            fadeInVR = true;
        }
        fadeEffect.fadeInBlackEffect();
        yield return new WaitUntil(() => currentTime > _retentionTimeStamp);
        // prepare for the pointing task
        fadeEffect.fadeInEffect();
        rectify.SetActive(true);
        if (currentTarget == Targets.virtualTarget)
        {
            virtualRectify.SetActive(true);
            physicalRectify.SetActive(false);
        }
        else
        {
            virtualRectify.SetActive(false);
            physicalRectify.SetActive(true);
        }
        laser.SetActive(true);
        isBaselineMeasure = true;
        beginTimeStamp = currentTime;
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
        laserTransform.position = Vector3.Lerp(controller.transform.position, hitPoint, .5f); // move laser to the middle
        laserTransform.LookAt(hitPoint); // rotate and face the hit point
        laserTransform.localScale = new Vector3(laserTransform.localScale.x, laserTransform.localScale.y, Vector3.Distance(controller.transform.position, hitPoint));
    }

    public struct TrialData
    {
        public string particiapntID;
        public int blockNum;
        public int trialNum;
        public string currentRotation;
        public string currentTarget;
        public bool isBaselineMeasure;
        public float beginTime;
        public float endTime;
        public Vector3 targetPos;
        public Vector3 selectedPos;
        public Vector3 controllerPos;

        public TrialData(string particiapntID, int blockNum, int trialNum, string currentRotation, string currentTarget, bool isBaselineMeasure,
                         float beginTime, float endTime, Vector3 targetPos, Vector3 selectedPos, Vector3 controllerPos)
        {
            this.particiapntID = particiapntID;
            this.blockNum = blockNum; 
            this.trialNum = trialNum;
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

    public static float DegreeToRadian(float deg)
    {
        return deg * Mathf.PI / 180;
    }
}
