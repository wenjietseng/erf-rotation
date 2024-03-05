using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TMPro;
using System.IO;
using Oculus.Platform;
using System;
using Meta.WitAi;
using Unity.VisualScripting;

public class ExpController : MonoBehaviour
{
    /// <summary>
    /// This script is for the 2nd version of ERF study. The goal is to observe how the real-world ERF changes
    /// while using VR by manipulating 1) duration and 2) rotation.
    /// The task shows two targets and the participant would have a baseline measure and a testing measure after a small waiting time with or without rotation.
    /// Virtual targets {show two targets (stripes and dotted) -> retention 5s -> baseline measure -> interval for waiting or rotation -> testing measure -> rest for the next task}
    /// Real-world targets [show two targets (blue and green) -> {Virtual targets} -> {Virtual targets} -> testing measure -> rest for the next trial] 
    /// </summary>
    // Start is called before the first frame update

    [Header("User Info")]
    public int participantID = 0;

    [Header("Stimuli and Materials")]
    public GameObject bluePhysicalTarget;
    public GameObject greenPhysicalTarget;
    public GameObject dottedVirtualTarget;
    public GameObject stripesVirtualTarget;
    public List<GameObject> physicalTargetList;
    public List<GameObject> virtualTargetList;
    // for procedures
    public GameObject StartTrialPanel;

    private AudioSource pointingIndicator;


    [Header("Variables and Trials")]
    public int layoutBlockNum = 0; // 4 layouts of physical pairs
    public enum PhyTargetsLayouts {A = 0, B = 1, C = 2, D = 3};
    public PhyTargetsLayouts currentPhyTargetsLayout;

    public int conditionBlockNum = 0; // condition block repeat 3 times, each includes 4 combinations
    public enum Conditions {durationShort_0 = 0, durationShort_120 = 1, durationLong_0 = 2, durationLong_120 = 3};
    public Conditions currentCondition;
    public List<int> conditionArray;

    public int trialNum = 0; // 0-4 (4 trials per condition block), 4 x 3 x 4 = 48 total
    public float currentTime = 0;
    bool isTrialRunning;
    bool isBaselineMeasure;
    bool isTestingMeasure;
    float beginTimeStamp;
    float endTimeStamp;
    int pairCounter = 0;

    [Header("Pointing")]
    public GameObject controller; 
    public LayerMask pointingMask;
    public GameObject laser;
    private Transform laserTransform;
    public Vector3 hitPoint;
    public GameObject recticleGameObjects;
    public GameObject dottedRecticle;
    public GameObject stripesRecticle;
    public GameObject blueRecticle;
    public GameObject greenRecticle;
    public GameObject ans;
    Vector3 firstResponseVec;
    Vector3 secondResponseVec;

    Transform recticle;
    Ray ray;
    RaycastHit hit;

    public enum SelfRotation {none = 0, rotate = 1};
    public SelfRotation currentRotation;
    public enum Targets {virtualTarget = 0, physicalTarget = 1}; // see the procedure :D
    public Targets currentTarget;
    [Header("UI")]

    public GameObject instruction;
    public TMP_Text instructionText;


    
    void Start()
    {

        pointingIndicator = GetComponent<AudioSource>();
        laserTransform = laser.transform;


        Helpers.Shuffle(physicalTargetList);
        Helpers.Shuffle(virtualTargetList);
        bluePhysicalTarget.SetActive(false);
        greenPhysicalTarget.SetActive(false);
        dottedVirtualTarget.SetActive(false);
        stripesVirtualTarget.SetActive(false);
        laser.SetActive(false);


    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            InitializeVirtualTargets();
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isTrialRunning = true;
            StartCoroutine(RunOneVirtualTrial());
        }
        
        if (layoutBlockNum < 4)
        {
            if (conditionBlockNum < 3)
            {
                if (trialNum < 4)
                {
                    if (isTrialRunning) // this seems to be physical trial
                    {
                        currentTime += Time.deltaTime;
                        if (isBaselineMeasure)
                        {
                            // recall virtual targets
                            if (Physics.Raycast(controller.transform.position, controller.transform.forward, out hit, 200, pointingMask))
                            {
                                hitPoint = hit.point;
                                UpdateLaser(hitPoint);       
                                UpdateReticle(hitPoint);   
                            
                                if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch))
                                {
                                    if (pairCounter == 0)
                                    {
                                        firstResponseVec = hitPoint;
                                    }
                                    else
                                    {
                                        secondResponseVec = hitPoint;
                                    }
                                    // show ans
                                    StartCoroutine(ShowAns());
                                    
                                    // record data
                                    endTimeStamp = currentTime;
                                    AddData();
                                    
                                    // update recticle
                                    pairCounter += 1;
                                    beginTimeStamp = currentTime;
                                    
                                    if (pairCounter == 2)
                                    {
                                        // reset variables
                                        isBaselineMeasure = false;
                                        DisablePointing();
                                        StartCoroutine(RemoveResponse()); // practice 1s, formal study 0s
                                        pairCounter = 0;
                                        // show rotation cue
                                        // after showing roatation cue, reset two response vectors
                                        // StartCoroutine(ShowRotateOrientation(endTimeStamp + 5.0f));
                                        Debug.LogWarning("Go to rotation and testing");
                                    }
                                    

                                }
                            }
                            else
                            {
                                DisablePointing();
                            }
                        }




                    }
                }

            }
        }

    }

    private void AddData()
    {
        // dataList.Add(new TrialData(participantID.ToString(), 
        //                     blockNum,
        //                     trialNum,
        //                     currentCondition.ToString(),
        //                     currentRotation.ToString(),
        //                     currentTarget.ToString(),
        //                     isBaselineMeasure,
        //                     beginTimeStamp,
        //                     endTimeStamp,
        //                     targetTransform.position,
        //                     responseVec,
        //                     controller.transform.position));
        // record baseline measure (two time stamps, target position, selected position)
        Debug.LogWarning("Participant: P" +    participantID.ToString()                + ", " +
                        "Layout Block: " +     layoutBlockNum                          + ", " +
                        "Condition Block: " +  conditionBlockNum                       + ", " +
                        "Trial: " +            trialNum                                + ", " +
                        "Condition: " +        currentCondition.ToString()             + ", " +
                        // "Self-Rotation: " +    currentRotation.ToString()              + ", " +
                        // "TargetType: " +       currentTarget.ToString()                + ", " +
                        "Baseline: " +         isBaselineMeasure                       + ", " +
                        "PairCount: " +        pairCounter.ToString()                  + ", " +
                        "BeginTime: " +        beginTimeStamp.ToString("F6")           + ", " +
                        "EndTime: " +          endTimeStamp.ToString("F6")             + ", " +
                        "SelectedPos: " +      hitPoint.ToString("F6")                 + ", " +
                        "AnsPos: " +           virtualTargetList[pairCounter].transform.position.ToString("F6") + "," +
                        "ControllerPos: " +    controller.transform.position.ToString("F6")   + ", "); 
    }

    void DisablePointing()
    {
        // make sure they won't be visible directly when displaying 
        if (laser.activeSelf) laser.transform.position = recticleGameObjects.transform.position;
        laser.SetActive(false);
    }

    private void UpdateLaser(Vector3 hitPoint)
    {
        laser.SetActive(true);
        laserTransform.position = Vector3.Lerp(controller.transform.position, hitPoint, .5f); // move laser to the middle
        laserTransform.LookAt(hitPoint); // rotate and face the hit point
        laserTransform.localScale = new Vector3(laserTransform.localScale.x, laserTransform.localScale.y, Vector3.Distance(controller.transform.position, hitPoint));
    }

    private void UpdateReticle(Vector3 hitPoint)
    {
        // difference between virtual and physical? or in two functions?
        if (virtualTargetList[pairCounter].name == "Dotted Virtual Target")
        {
            recticle = dottedRecticle.transform;
        }
        else if (virtualTargetList[pairCounter].name == "Stripes Virtual Target")
        {
            recticle = stripesRecticle.transform;
        }
        recticle.position = hitPoint;
    }

    IEnumerator ShowAns(float duration = 1f)
    {
        GameObject tempAns = Instantiate(ans, virtualTargetList[pairCounter].transform.position, Quaternion.identity);
        yield return new WaitForSeconds(duration);
        Destroy(tempAns);
        yield return 0;
    }

    IEnumerator RemoveResponse(float duration = 1f)
    {
        yield return new WaitForSeconds(duration);
        dottedRecticle.transform.position = recticleGameObjects.transform.position;
        stripesRecticle.transform.position = recticleGameObjects.transform.position;
        blueRecticle.transform.position = recticleGameObjects.transform.position;
        greenRecticle.transform.position = recticleGameObjects.transform.position;
        yield return 0;
    }

    void InitializeVirtualTargets()
    {
        // select far, middle, near for dotted

        List<float> depthList = new List<float> {1f, 2f, 3f};
        Helpers.Shuffle(depthList); 

        Vector3 dottedPos = Vector3.zero;
        Vector3 stripesPos = Vector3.zero;

        while (Vector3.Distance(dottedPos, stripesPos) < 0.31f || Vector3.Angle(dottedPos, stripesPos) < 10f)
        {
            dottedPos = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, depthList[0] + Helpers.RandomGaussian(-.8f, 0.8f));
            stripesPos = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, depthList[1] + Helpers.RandomGaussian(-.8f, 0.8f));
            // Debug.LogWarning("Distance: " + Vector3.Distance(dottedPos, stripesPos) + "\nAngle: " + Vector3.Angle(dottedPos, stripesPos));
        }

        dottedVirtualTarget.SetActive(true);
        stripesVirtualTarget.SetActive(true);
        dottedVirtualTarget.transform.position = dottedPos;
        stripesVirtualTarget.transform.position = stripesPos;
    }

    public IEnumerator RunOneVirtualTrial(float _showTimeStamp = 5.0f, float _retentionTimeStamp = 10.0f, float rotation_amount = 0f)
	{
        currentTime = 0.0f;
        // instructionText.text = "";
        // StartTrialPanel.SetActive(false);

        // show a target
        InitializeVirtualTargets();

        yield return new WaitUntil(() => currentTime > _showTimeStamp);

        /// 
        /// add fade in transparent for smoothness
        /// 
        dottedVirtualTarget.SetActive(false);
        stripesVirtualTarget.SetActive(false);

        yield return new WaitUntil(() => currentTime > _retentionTimeStamp);

        // prepare for the pointing task
        beginTimeStamp = currentTime;
        pointingIndicator.Play();

        laser.SetActive(true);
        Helpers.Shuffle(virtualTargetList);
        isBaselineMeasure = true;
        yield return 0;
    }
}
