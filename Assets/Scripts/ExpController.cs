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
using Oculus.Platform.Models;

public class ExpController : MonoBehaviour
{
    /// <summary>
    /// Blocked by physical target layout because we wanna easier procedure for collecting data.
    /// Latin square
    /// Overall, 32 trials, two rotations, recall virtual or physical targets 
    /// Layout 1, 2, 3, 4
    /// 
    /// 
    /// </summary>
    // Start is called before the first frame update

    [Header("User Info")]
    public int participantID = 0;

    [Header("Conditions")]
    public int layoutBlockNum = 0; 
    /// <summary> a Latin square for 4 conditions
    /// 4 layouts of physical targets
    /// A C B D | P1, 5,  9, ... 
    /// B A D C | P2, 6, 10, ...
    /// C D A B | P3, 7, 11, ...
    /// D B C A | P4, 8, 12, ...
    /// </summary>
    int [,] latinSquare4x4 = new int [4, 4] {{1, 3, 2, 4},
                                             {2, 1, 4, 3},
                                             {3, 4, 1, 2},
                                             {4, 2, 3, 1}};

    public enum PhyTargetsLayouts {A = 1, B = 2, C = 3, D = 4};
    public PhyTargetsLayouts currentPhyTargetsLayout;
    // 4 conditions, virtual/physical X static/rotate, four trials form one block. 8 blocks in total.
    // In each physical layout, repeat 2 blocks
    public int conditionBlockNum = 0; // 0, 1
    public enum Conditions {virtualStatic = 0, virtualRotate = 1, physicalStatic = 2, physicalRotate = 3};
    public Conditions currentCondition;
    public List<int> conditionArray;
    public enum SelfRotation {none = 0, rotate = 1};
    public SelfRotation currentRotation;
    public enum Targets {virtualTarget = 0, physicalTarget = 1}; // see the procedure :D
    public Targets currentTarget;

    public int trialNum = 0; // 0-3 (4 trials per condition block), 4 x 2 x 4 = 32 total
    public int decoyNum = 0;
    public int decoyAmountThisTrial = 0;
    public List<int> rotationAngleList;
    // rotate direction
    public int[] directionTable = new int[4] {0, 0, 0, 0}; // VirtualLeft, VirtualRight, PhysicalLeft, PhysicalRight
    public int whichDirection;

    [Header("Procedures")]
    public float currentTime = 0;
    bool isTrialRunning;
    bool isBaselineMeasure;
    bool isTestingMeasure;
    bool isDecoyRunning;
    bool isDecoyBaseline;
    bool isDecoyTesting;
    float beginTimeStamp;
    float endTimeStamp;
    int pairCounter = 0;

    [Header("Stimuli and Materials")]
    public GameObject bluePhysicalTarget;
    public GameObject greenPhysicalTarget;
    public GameObject blueVirtualTarget;
    public GameObject greenVirtualTarget;
    public GameObject dottedVirtualTarget;
    public GameObject stripesVirtualTarget;
    public List<GameObject> physicalTargetList;
    public List<GameObject> virtualTargetList;
    public List<GameObject> decoyTargetList;
    public GameObject decoys;
    public GameObject StartTrialPanel;
    public GameObject rotationCue;
    public AudioSource turnLeftSound;
    public AudioSource turnRightSound;
    private AudioSource pointingIndicator;

    [Header("Pointing")]
    public GameObject controller; 
    public LayerMask pointingMask;
    public GameObject laser;
    private Transform laserTransform;
    public Vector3 hitPoint;
    public GameObject reticleGameObjects;
    public GameObject blueReticle;
    public GameObject greenReticle;
    public GameObject dottedReticle;
    public GameObject stripesReticle;
    public GameObject ans;
    public GameObject groundTruth;
    Vector3 firstBaselineResponse;
    Vector3 secondBaselineResponse;
    Vector3 firstTestResponse;
    Vector3 secondTestResponse;
    Vector3 decoy_firstBaselineResponse;
    Vector3 decoy_secondBaselineResponse;
    Vector3 decoy_firstTestResponse;
    Vector3 decoy_secondTestResponse;
    Vector3 responsePos;
    Transform reticle;
    Ray ray;
    RaycastHit hit;

    [Header("UI")]
    public GameObject instruction;
    public TMP_Text instructionText;

    void Start()
    {
        /// When begin, show their participant no
        pointingIndicator = GetComponent<AudioSource>();
        laserTransform = laser.transform;

        // initialize condition
        PreparePhyTargetsLayout();
        conditionArray = new List<int> {0, 1, 2, 3};
        Helpers.Shuffle(conditionArray);
        PrepareCondition();
        // PrintConditionInfo();
        InitializePhysicalTargets();

        // initialize procedure

        // variables control before starting the first trial
        bluePhysicalTarget.SetActive(false);
        greenPhysicalTarget.SetActive(false);
        blueVirtualTarget.SetActive(false);
        greenVirtualTarget.SetActive(false);
        dottedVirtualTarget.SetActive(false);
        stripesVirtualTarget.SetActive(false);
        laser.SetActive(false);
        rotationCue.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.W))
        {
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            isTrialRunning = true;
            StartCoroutine(ShowTargetsAndRetention());
        }
        
        if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.LTouch))
        {

        }
        

        if (layoutBlockNum < 4)
        {
            if (conditionBlockNum < 2)
            {
                // touch starting panel, or use keyboard imitate
                if (isTrialRunning)
                {
                    currentTime += Time.deltaTime;
                    
                    if (isBaselineMeasure)
                    {
                        if (Physics.Raycast(controller.transform.position, controller.transform.forward, out hit, 200, pointingMask))
                        {
                            hitPoint = hit.point;
                            UpdateLaser(hitPoint);
                            UpdateReticle(hitPoint);
                        
                            if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch))
                            {
                                // record data
                                responsePos = hitPoint;
                                endTimeStamp = currentTime;
                                AddData();

                                StartCoroutine(ShowAns()); //practice...? how to fix?

                                // prepare the next target
                                if (pairCounter == 0)
                                {
                                    beginTimeStamp = currentTime;
                                    firstBaselineResponse = responsePos;
                                }
                                else
                                {
                                    secondBaselineResponse = responsePos;
                                }
                                pairCounter += 1;
                               
                                if (pairCounter == 2)
                                {
                                    // reset variables
                                    isBaselineMeasure = false;
                                    DisablePointing();
                                    StartCoroutine(RemoveResponse(1f)); // practice 1s, formal study 0s
                                    pairCounter = 0;
                                    isDecoyRunning = true;

                                    StartCoroutine(ShortPauseBeforeNextDecoy(5f));
                                    // StartCoroutine(ShowDecoyTargetsAndRetention(endTimeStamp+7f, endTimeStamp+12f));
                                }
                            }
                        }
                        else
                        {
                            DisablePointing();
                        }
                    }

                    if (isDecoyRunning && decoyNum < decoyAmountThisTrial)
                    {
                        if (isDecoyBaseline)
                        {
                            if (Physics.Raycast(controller.transform.position, controller.transform.forward, out hit, 200, pointingMask))
                            {
                                hitPoint = hit.point;
                                UpdateLaser(hitPoint);
                                UpdateReticle(hitPoint);

                                if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch))
                                {
                                    // record data
                                    responsePos = hitPoint;
                                    endTimeStamp = currentTime;
                                    AddData();

                                    StartCoroutine(ShowAns()); //practice...? how to fix?

                                    // prepare the next target
                                    if (pairCounter == 0)
                                    {
                                        beginTimeStamp = currentTime;
                                        decoy_firstBaselineResponse = responsePos;
                                    }
                                    else
                                    {
                                        decoy_secondBaselineResponse = responsePos;
                                    }
                                    pairCounter += 1;
                                
                                    if (pairCounter == 2)
                                    {
                                        // reset variables
                                        isDecoyBaseline = false;
                                        DisablePointing();
                                        StartCoroutine(RemoveResponse(1f)); // practice 1s, formal study 0s
                                        pairCounter = 0;

                                        StartCoroutine(ShowRotationCue(endTimeStamp, whichDirection, rotationAngleList[decoyNum]));
                                    }
                                }
                            }
                        }

                        if (isDecoyTesting)
                        {
                            if (Physics.Raycast(controller.transform.position, controller.transform.forward, out hit, 200, pointingMask))
                            {
                                hitPoint = hit.point;
                                UpdateLaser(hitPoint);
                                UpdateReticle(hitPoint);
                            
                                if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch))
                                {
                                    // record data
                                    responsePos = hitPoint;
                                    endTimeStamp = currentTime;
                                    AddData();

                                    StartCoroutine(ShowAns()); //practice...? how to fix?

                                    // prepare the next target
                                    if (pairCounter == 0)
                                    {
                                        beginTimeStamp = currentTime;
                                        decoy_firstTestResponse = responsePos;
                                    }
                                    else
                                    {
                                        decoy_secondTestResponse = responsePos;
                                    }
                                    pairCounter += 1;
                                
                                    if (pairCounter == 2)
                                    {
                                        // reset variables
                                        isDecoyTesting = false;
                                        DisablePointing();
                                        StartCoroutine(RemoveResponse(1f)); // practice 1s, formal study 0s
                                        pairCounter = 0;
                                        decoyNum += 1;
                                        
                                        if (decoyNum < decoyAmountThisTrial) StartCoroutine(ShortPauseBeforeNextDecoy(5f));
                                        else 
                                        {
                                            decoyNum = 0;
                                            isDecoyRunning = false;
                                            StartCoroutine(ShortPauseBeforeBackToTest()); 
                                        }
                                    }
                                }

                            }
                        }
                        // decoy (based on decoyNum) and rotate (if any)
                    }
                    
                    if (isTestingMeasure)
                    {
                        if (Physics.Raycast(controller.transform.position, controller.transform.forward, out hit, 200, pointingMask))
                        {
                            hitPoint = hit.point;
                            UpdateLaser(hitPoint);
                            UpdateReticle(hitPoint);

                            if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch))
                            {                                    
                                // record data
                                responsePos = hitPoint;
                                endTimeStamp = currentTime;
                                AddData();

                                StartCoroutine(ShowAns()); //practice...? how to fix?

                                // prepare the next target
                                if (pairCounter == 0)
                                {
                                    beginTimeStamp = currentTime;
                                    firstTestResponse = responsePos;
                                }
                                else
                                {
                                    secondTestResponse = responsePos;
                                }
                                pairCounter += 1;
                            
                                if (pairCounter == 2)
                                {
                                    // reset variables
                                    isTestingMeasure = false;
                                    DisablePointing();
                                    StartCoroutine(RemoveResponse(1f)); // practice 1s, formal study 0s
                                    pairCounter = 0;
                                    this.transform.rotation = Quaternion.Euler(0, 0, 0);
                                    
                                    // complete one trial
                                    isTrialRunning = false;
                                    if (trialNum < 3)
                                    {
                                        trialNum += 1;
                                    }
                                    else
                                    {
                                        trialNum = 0;
                                        conditionBlockNum += 1;
                                        Helpers.Shuffle(conditionArray);
                                    }
                                    PrepareCondition();
                                    // StartTrialPanel.SetActive(true);
                                    // go back to the starting orientation
                                }
                            }
                        }
                    }
                }
                // resting time for participants
            }
            else
            {
                conditionBlockNum = 0;
                layoutBlockNum += 1;
                if (layoutBlockNum < 4)
                {
                    PreparePhyTargetsLayout();
                    InitializePhysicalTargets();
                }
            }
        }
        else
        {
            Debug.LogWarning("End of the study");
        }
    }

    private void PreparePhyTargetsLayout()
    {
        if (latinSquare4x4[participantID % 4, layoutBlockNum] == 1)
        {
            currentPhyTargetsLayout = PhyTargetsLayouts.A;
        }
        else if (latinSquare4x4[participantID % 4, layoutBlockNum] == 2)
        {
            currentPhyTargetsLayout = PhyTargetsLayouts.B;
        }
        else if (latinSquare4x4[participantID % 4, layoutBlockNum] == 3)
        {
            currentPhyTargetsLayout = PhyTargetsLayouts.C;
        }
        else if (latinSquare4x4[participantID % 4, layoutBlockNum] == 4)
        {
            currentPhyTargetsLayout = PhyTargetsLayouts.D;
        }
        Debug.LogWarning("The current layout of physical targets is: " + currentPhyTargetsLayout.ToString());
    }

    private void PrepareCondition()
    {
        // get condition
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

        // prepare decoys with rotation
        decoyAmountThisTrial = (UnityEngine.Random.value < 0.5f) ? 2 : 3;
        if (currentRotation == SelfRotation.none)
        {
            rotationAngleList = (decoyAmountThisTrial == 2) ? new List<int> {0, 0} : new List<int> {0, 0, 0};
        }
        else
        {
            rotationAngleList = (decoyAmountThisTrial == 2) ? new List<int> {40, 80} : new List<int> {0, 40, 80};
        }
        Helpers.Shuffle(rotationAngleList);

        // balance direction
        if (currentRotation == SelfRotation.rotate)
        {
            if (currentTarget == Targets.virtualTarget)
            {
                whichDirection = (UnityEngine.Random.value < 0.5f) ? 0 : 1; // left or right?
                if (directionTable[whichDirection] < 4)
                {
                    directionTable[whichDirection] += 1;
                }
                else
                {
                    whichDirection = (whichDirection == 0) ? 1 : 0;
                    directionTable[whichDirection] += 1;
                }
            }
            else
            {
                whichDirection = (UnityEngine.Random.value < 0.5f) ? 2 : 3; // left or right?
                if (directionTable[whichDirection] < 4)
                {
                    directionTable[whichDirection] += 1;
                }
                else
                {
                    whichDirection = (whichDirection == 2) ? 3 : 2;
                    directionTable[whichDirection] += 1;
                }
            }
        }
        else whichDirection = -1;
    }

    private void PrintConditionInfo()
    {
        Debug.LogWarning(layoutBlockNum*8 + conditionBlockNum*4 + trialNum                  + ", " +
                        "Participant: P" +         participantID.ToString()                 + ", " +
                        "Layout Block Num: " +      layoutBlockNum                          + ", " +
                        "Condition Block Num: " +   conditionBlockNum                       + ", " +
                        "Trial: " +                 trialNum                                + ", " +
                        "Layout Type: " +           currentPhyTargetsLayout.ToString()      + ", " +
                        "Condition: " +             currentCondition.ToString()             + ", " +
                        "Self-Rotation: " +         currentRotation.ToString()              + ", " +
                        "TargetType: " +            currentTarget.ToString()                + ", " +
                        "Decoy: " +                 decoyAmountThisTrial                    + ", " );
    }

    private void AddData()
    {
        // this part could be together with ShowAns...
        if (isDecoyRunning) groundTruth = decoyTargetList[pairCounter];
        else
        {
            if (currentTarget == Targets.virtualTarget) groundTruth = virtualTargetList[pairCounter];
            else groundTruth = physicalTargetList[pairCounter];
        }

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

        Debug.LogWarning(// Response Info
                        layoutBlockNum*8 + conditionBlockNum*4 + trialNum                                   + ", " +
                        "Participant: P" +          participantID.ToString()                                + ", " +
                        "Layout Block Num: " +      layoutBlockNum                                          + ", " +
                        "Condition Block Num: " +   conditionBlockNum                                       + ", " +
                        "Trial: " +                 trialNum                                                + ", " +
                        "Layout Type: " +           currentPhyTargetsLayout.ToString()                      + ", " +
                        "Condition: " +             currentCondition.ToString()                             + ", " +
                        "TargetType: " +            currentTarget.ToString()                                + ", " +
                        "PairCount: " +             pairCounter.ToString()                                  + ", " +
                        "Self-Rotation: " +         currentRotation.ToString()                              + ", " +
                        "RotateDirection: " +       whichDirection                                          + ", " +
                        "DecoyAmount: " +           decoyAmountThisTrial                                    + ", " +
                        "CurrentDecoy: " +          decoyNum                                                + ", " +
                        "RotationAmount: " +        rotationAngleList[decoyNum]                             + ", " +
                        "Baseline: " +              isBaselineMeasure                                       + ", " +
                        "Testing: "  +              isTestingMeasure                                        + ", " +
                        "DecoyBaseline: " +         isDecoyBaseline                                         + ", " +
                        "DecoyTesting: "  +         isDecoyTesting                                          + ", " +
                        // RT
                        "BeginTime: " +             beginTimeStamp.ToString("F6")                           + ", " +
                        "EndTime: " +               endTimeStamp.ToString("F6")                             + ", " +
                        // position error
                        "SelectedPos: " +           responsePos.ToString("F6")                              + ", " +
                        "AnsPos: " +                groundTruth.transform.position.ToString("F6")           + ", " +
                        "TargetName: " +            groundTruth.name                                        + ", " +
                        "ControllerPos: " +         controller.transform.position.ToString("F6")            + "\n"); 
    }

    private void DisablePointing()
    {
        // make sure they won't be visible directly when displaying 
        if (laser.activeSelf) laser.transform.position = reticleGameObjects.transform.position;
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
        if (isDecoyRunning)
        {
            if (decoyTargetList[pairCounter].name == "Dotted Decoy Target")
            {
                reticle = dottedReticle.transform;
            }
            else if (decoyTargetList[pairCounter].name == "Stripes Decoy Target")
            {
                reticle = stripesReticle.transform;
            }            
        }
        else
        {
            if (currentTarget == Targets.virtualTarget)
            {
                if (virtualTargetList[pairCounter].name == "Blue Virtual Target")
                {
                    reticle = blueReticle.transform;
                }
                else if (virtualTargetList[pairCounter].name == "Green Virtual Target")
                {
                    reticle = greenReticle.transform;
                }
            }
            else
            {
                if (physicalTargetList[pairCounter].name == "Blue Physical Target")
                {
                    reticle = blueReticle.transform;
                }
                else if (physicalTargetList[pairCounter].name == "Green Physical Target")
                {
                    reticle = greenReticle.transform;
                }
            }
        }
        reticle.position = hitPoint;
    }

    private IEnumerator ShowAns(float duration = 1f)
    {
        GameObject tempAns;
        if (isDecoyRunning)
        {
            tempAns = Instantiate(ans, decoyTargetList[pairCounter].transform.position, Quaternion.identity);
        }
        else
        {
            if (currentTarget == Targets.virtualTarget)
            {
                tempAns = Instantiate(ans, virtualTargetList[pairCounter].transform.position, Quaternion.identity);
            }
            else
            {
                tempAns = Instantiate(ans, physicalTargetList[pairCounter].transform.position, Quaternion.identity);
            }
        }
        yield return new WaitForSeconds(duration);
        Destroy(tempAns);
        yield return 0;
    }

    private IEnumerator ShortPauseBeforeNextDecoy(float duration = 5f)
    {
        yield return new WaitForSeconds(duration);
        StartCoroutine(ShowDecoyTargetsAndRetention(currentTime));
        yield return 0;
    }

    private IEnumerator ShortPauseBeforeBackToTest(float duration = 5f)
    {
        Debug.LogWarning("Waiting before back to testing blue and green");
        yield return new WaitForSeconds(duration);
        beginTimeStamp = currentTime;
        pointingIndicator.Play();
        laser.SetActive(true);
        if (currentTarget == Targets.virtualTarget)
            CheckBaselinePerformance(virtualTargetList, firstBaselineResponse, secondBaselineResponse);
        else
            CheckBaselinePerformance(physicalTargetList, firstBaselineResponse, secondBaselineResponse);
        isTestingMeasure = true;

        yield return 0;

    }

    private IEnumerator RemoveResponse(float duration = 1f)
    {
        yield return new WaitForSeconds(duration);
        dottedReticle.transform.position = reticleGameObjects.transform.position;
        stripesReticle.transform.position = reticleGameObjects.transform.position;
        blueReticle.transform.position = reticleGameObjects.transform.position;
        greenReticle.transform.position = reticleGameObjects.transform.position;
        yield return 0;
    }

    private void InitializeVirtualTargets(GameObject targetA, GameObject targetB)
    {
        // select far, middle, near for dotted

        List<float> depthList = new List<float> {1f, 2f, 3f};
        Helpers.Shuffle(depthList); 

        Vector3 posA = Vector3.zero;
        Vector3 posB = Vector3.zero;

        while (Vector3.Distance(posA, posB) < 0.31f || Vector3.Angle(posA, posB) < 10f)
        {
            posA = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, depthList[0] + Helpers.RandomGaussian(-.8f, 0.8f));
            posB = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, depthList[1] + Helpers.RandomGaussian(-.8f, 0.8f));
            // Debug.LogWarning("Distance: " + Vector3.Distance(dottedPos, stripesPos) + "\nAngle: " + Vector3.Angle(dottedPos, stripesPos));
        }

        targetA.SetActive(true);
        targetB.SetActive(true);
        targetA.transform.position = posA;
        targetB.transform.position = posB;
    }

    private void InitializePhysicalTargets()
    {
        // note: four layout might need to be different?
        bluePhysicalTarget.SetActive(true);
        greenPhysicalTarget.SetActive(true);
        if (currentPhyTargetsLayout == PhyTargetsLayouts.A)
        {
            bluePhysicalTarget.transform.position = new Vector3(-0.8f, 0, 0.95f);
            greenPhysicalTarget.transform.position = new Vector3(0.8f, 0, 1.45f);
        }
        else if (currentPhyTargetsLayout == PhyTargetsLayouts.B)
        {
            bluePhysicalTarget.transform.position = new Vector3(-0.8f, 0, 1.45f);
            greenPhysicalTarget.transform.position = new Vector3(0.8f, 0, 0.95f);
        }
        else if (currentPhyTargetsLayout == PhyTargetsLayouts.C)
        {
            bluePhysicalTarget.transform.position = new Vector3( 0.8f, 0, 0.95f);
            greenPhysicalTarget.transform.position = new Vector3(-0.8f, 0, 1.45f);
        }
        else
        {
            bluePhysicalTarget.transform.position = new Vector3(  0.8f, 0, 1.45f);
            greenPhysicalTarget.transform.position = new Vector3(-0.8f, 0, 0.95f);
        }
        bluePhysicalTarget.SetActive(false);
        greenPhysicalTarget.SetActive(false);
    }

    private IEnumerator ShowTargetsAndRetention(float showTimeStamp = 7f, float retentionTimeStamp = 12f)
	{
        currentTime = 0.0f;
        // instructionText.text = "";
        // StartTrialPanel.SetActive(false);

        // show a target
        if (currentTarget == Targets.virtualTarget) InitializeVirtualTargets(blueVirtualTarget, greenVirtualTarget);
        else
        {
            Debug.LogWarning("Passthorugh gogogo");
            bluePhysicalTarget.SetActive(true); // this is just for collecting data, in fact, participants see the target in passthrough
            greenPhysicalTarget.SetActive(true);
        }

        yield return new WaitUntil(() => currentTime > showTimeStamp);

        /// 
        /// add fade in transparent for smoothness, if needed
        /// 
        if (currentTarget == Targets.virtualTarget)
        {
            blueVirtualTarget.SetActive(false);
            greenVirtualTarget.SetActive(false);
        }
        else
        {
            bluePhysicalTarget.SetActive(false);
            greenPhysicalTarget.SetActive(false);
            Debug.LogWarning(bluePhysicalTarget.transform.position);
        }

        yield return new WaitUntil(() => currentTime > retentionTimeStamp);

        // prepare for the pointing task
        beginTimeStamp = currentTime;
        if (currentTarget == Targets.virtualTarget) Helpers.Shuffle(virtualTargetList);
        else Helpers.Shuffle(physicalTargetList);
        pointingIndicator.Play();
        laser.SetActive(true);
        isBaselineMeasure = true;
        yield return 0;
    }

    private IEnumerator ShowDecoyTargetsAndRetention(float callTimeStamp)
    {
        InitializeVirtualTargets(dottedVirtualTarget, stripesVirtualTarget);
        if (currentRotation == SelfRotation.rotate)
        {
            if (decoyNum != 0)
            {
                if (whichDirection % 2 == 0)
                {
                    decoys.transform.rotation = Quaternion.Euler(0, decoys.transform.rotation.eulerAngles.y - rotationAngleList[decoyNum-1], 0);
                }
                else
                {
                    decoys.transform.rotation = Quaternion.Euler(0, decoys.transform.rotation.eulerAngles.y + rotationAngleList[decoyNum-1], 0);
                }
            }
        }

        yield return new WaitUntil(() => currentTime > callTimeStamp + 7f);
        dottedVirtualTarget.SetActive(false);
        stripesVirtualTarget.SetActive(false);
        yield return new WaitUntil(() => currentTime > callTimeStamp + 12f);
        // prepare for the pointing task
        beginTimeStamp = currentTime;
        Helpers.Shuffle(decoyTargetList);
        pointingIndicator.Play();
        laser.SetActive(true);
        isDecoyBaseline = true;
        yield return 0;
    }

    private IEnumerator ShowRotationCue(float callTimeStamp, int rotateDirection, float rotateAmount = 0f)
    {
        Debug.LogWarning("Rotation is: " + rotateDirection + ", " + rotateAmount);
        rotationCue.SetActive(true);
        rotationCue.GetComponent<SimpleRotationCue>().visualCollider.SetActive(true);
        SimpleRotationCue.isCueComplete = false;

        if (rotateDirection != -1)
        {
            if (rotateDirection % 2 == 0)
            {
                if (rotateAmount != 0) turnLeftSound.Play();
                this.transform.rotation = Quaternion.Euler(0, this.transform.rotation.eulerAngles.y - rotateAmount, 0);
            }
            else
            {
                if (rotateAmount != 0) turnRightSound.Play();
                this.transform.rotation = Quaternion.Euler(0, this.transform.rotation.eulerAngles.y + rotateAmount, 0);
            }
        }

        yield return new WaitUntil(() => currentTime > callTimeStamp + 5f && SimpleRotationCue.isCueComplete);
        rotationCue.SetActive(false);
        beginTimeStamp = currentTime;
        pointingIndicator.Play();
        laser.SetActive(true);

        CheckBaselinePerformance(decoyTargetList, decoy_firstBaselineResponse, decoy_secondBaselineResponse);

        // recall order depending which target had a better performance in the baseline measure
        // can we make this one into a function?
        // float firstDistError = Vector3.Distance(virtualTargetList[0].transform.position, firstBaselineResponse);
        // float secondDistError = Vector3.Distance(virtualTargetList[1].transform.position, secondBaselineResponse);
        // if (firstDistError > secondDistError)
        // {
        //     virtualTargetList.Reverse();
        // }

        isDecoyTesting = true;
        yield return 0;
    }

    private void CheckBaselinePerformance(List<GameObject> list, Vector3 firstResponse, Vector3 secondResponse)
    {
        float firstDistError = Vector3.Distance(list[0].transform.position, firstResponse);
        float secondDistError = Vector3.Distance(list[1].transform.position, secondResponse);
        if (firstDistError > secondDistError)
        {
            list.Reverse();
        }
    }
}
