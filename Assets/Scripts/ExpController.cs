using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

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
    public enum BuildFor {Practice = 0, FormalStudy = 1};
    public BuildFor buildFor;
    public string dataFilePath;
    private StreamWriter erfStudyWriter;
    private List<PointingData> dataList;
    private bool isDataSaved;

    [Header("Conditions")]
    public int layoutBlockNum = 0; 
    /// <summary> a Latin square for 4 conditions
    /// 4 layouts of physical targets
    /// A C B D | P0, P4, P8 
    /// B A D C | P1, P5, P9
    /// C D A B | P2, P6, P10
    /// D B C A | P3, P7, P11
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
    public float restingDuration = 60f;
    bool isTrialRunning;
    bool isBaselineMeasure;
    bool isTestingMeasure;
    bool isDecoyRunning;
    bool isDecoyBaseline;
    bool isDecoyTesting;
    float beginTimeStamp;
    float endTimeStamp;
    float restingTime;
    int pairCounter = 0;
    public static bool isStartTrialPanelTriggered;

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
    RaycastHit hit;

    [Header("UI")]
    public GameObject StartTrialPanel;
    public TMP_Text textOnStartPanel;
    public TMP_Text instructions;

    [Header("Passthrough")]
    public OVRPassthroughLayer passthroughLayer;
    public bool fadeInRW;
    public bool fadeInVR;
    float lerpTimeElapsed = 0;
    float lerpDuration = 1;
    int checkPhysicalLayout = 0;
    

    void Start()
    {
        string note = (buildFor == BuildFor.Practice) ? "_Practice" : "_FormalStudy";
        dataFilePath = Helpers.CreateDataPath(participantID, note);
        erfStudyWriter = new StreamWriter(dataFilePath, true);
        dataList = new List<PointingData>();

        /// When begin, show their participant no and practice
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
        if (OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            checkPhysicalLayout += 1;
            if (checkPhysicalLayout % 4 == 0)
            {
                currentPhyTargetsLayout = PhyTargetsLayouts.A;

            }
            else if (checkPhysicalLayout % 4 == 1)
            {
                currentPhyTargetsLayout = PhyTargetsLayouts.B;
            }
            else if (checkPhysicalLayout % 4 == 2)
            {
                currentPhyTargetsLayout = PhyTargetsLayouts.C;
            }
            else
            {
                currentPhyTargetsLayout = PhyTargetsLayouts.D;                
            }
            InitializePhysicalTargets();
            bluePhysicalTarget.SetActive (true);
            greenPhysicalTarget.SetActive(true);
            InitializeVirtualTargets(dottedVirtualTarget, stripesVirtualTarget);
        }

        if (Input.GetKeyDown(KeyCode.W))
        {

        }

        PassthroughControl();
        
        if (isStartTrialPanelTriggered  && Alignment.isCalibrated) 
        {
            isStartTrialPanelTriggered = false;
            StartTrialPanel.SetActive(false);
            StartCoroutine(ShowTargetsAndRetention());
            isTrialRunning = true;
        }        

        // if (layoutBlockNum < 4)
        // {
        if (conditionBlockNum < 8)
        {
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

                            if (buildFor == BuildFor.Practice) StartCoroutine(ShowAns());

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
                                StartCoroutine(RemoveResponse()); // practice 1s, formal study 0s
                                pairCounter = 0;
                                isDecoyRunning = true;

                                StartCoroutine(ShortPauseBeforeNextDecoy(5f));
                            }
                        }
                    }
                    else
                    {
                        DisablePointing();
                        if (reticle != null) reticle.position = reticleGameObjects.transform.position;
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

                                if (buildFor == BuildFor.Practice) StartCoroutine(ShowAns()); //practice...? how to fix?

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
                                    StartCoroutine(RemoveResponse()); // practice 1s, formal study 0s
                                    pairCounter = 0;

                                    StartCoroutine(ShowRotationCue(endTimeStamp, whichDirection, rotationAngleList[decoyNum]));
                                }
                            }
                        }
                        else
                        {
                            DisablePointing();
                            if (reticle != null) reticle.position = reticleGameObjects.transform.position;
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

                                if (buildFor == BuildFor.Practice) StartCoroutine(ShowAns()); //practice...? how to fix?

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
                                    StartCoroutine(RemoveResponse()); // practice 1s, formal study 0s
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
                        else
                        {
                            DisablePointing();
                            if (reticle != null) reticle.position = reticleGameObjects.transform.position;
                        }
                    }
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

                            if (buildFor == BuildFor.Practice) StartCoroutine(ShowAns()); //practice...? how to fix?

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
                                StartCoroutine(RemoveResponse()); // practice 1s, formal study 0s
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
                                    Debug.LogWarning(2);
                                    trialNum = 0;
                                    conditionBlockNum += 1;
                                    Helpers.Shuffle(conditionArray);
                                    if (conditionBlockNum < 8)
                                    {
                                        Debug.LogWarning(1);
                                        PreparePhyTargetsLayout();
                                        InitializePhysicalTargets();                                
                                        StartTrialPanel.SetActive(true);
                                        instructions.text = "Please tell the experimenter it's '" + currentPhyTargetsLayout.ToString() +
                                            "'\nand wait for the experimenter's instruction.";
                                        restingTime = restingDuration;
                                        StartTrialPanel.GetComponent<BoxCollider>().enabled = false;
                                    }
                                    else
                                    {
                                        if (!isDataSaved)
                                        {
                                            Debug.LogWarning("End of the study");
                                            StartTrialPanel.SetActive(false);
                                            StartCoroutine(WriteDataList());
                                            isDataSaved = true;
                                        }
                                    }
                                }

                                PrepareCondition();
                                StartTrialPanel.SetActive(true);
                                // instructions.text = "";
                                // textOnStartPanel.text = layoutBlockNum*8 + conditionBlockNum*4 + trialNum + "/32\nStart Next Trial";
                                // go back to the starting orientation
                            }
                        }
                    }
                    else
                    {
                        DisablePointing();
                        if (reticle != null) reticle.position = reticleGameObjects.transform.position;
                    }
                }
            }

            if (restingTime > 0)
            {
                restingTime -= Time.deltaTime;
                textOnStartPanel.text = restingTime.ToString("F0");
            }
            else
            {
                if (StartTrialPanel.activeSelf)
                {
                    instructions.text = "";
                    if ((layoutBlockNum*8 + conditionBlockNum*4 + trialNum) == 0) textOnStartPanel.text = "P" + participantID.ToString() + ", " + buildFor.ToString();
                    else textOnStartPanel.text = layoutBlockNum*8 + conditionBlockNum*4 + trialNum + "/32\nStart Next Trial";
                    StartTrialPanel.GetComponent<BoxCollider>().enabled = true;
                }
            }
        }
        else
        {
            // conditionBlockNum = 0;
            // layoutBlockNum += 1;
            // if (conditionBlockNum < 7)
            // {
            //     PreparePhyTargetsLayout();
            //     InitializePhysicalTargets();
            //     instructions.text = "Please tell the experimenter it's '" + currentPhyTargetsLayout.ToString() +
            //         "'\nand wait for the experimenter's instruction.";
            //     restingTime = restingDuration;
            //     StartTrialPanel.GetComponent<BoxCollider>().enabled = false;
            // }
            // else
            // {
            //     if (!isDataSaved)
            //     {
            //         Debug.LogWarning("End of the study");
            //         StartTrialPanel.SetActive(false);
            //         StartCoroutine(WriteDataList());
            //         isDataSaved = true;
            //     }
            // }
        }
        // }
        // else
        // {
        //     if (!isDataSaved)
        //     {
        //         Debug.LogWarning("End of the study");
        //         StartTrialPanel.SetActive(false);
        //         StartCoroutine(WriteDataList());
        //         isDataSaved = true;
        //     }
        // }
    }

    private void PassthroughControl()
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

    private void PreparePhyTargetsLayout()
    {
        if (latinSquare4x4[participantID % 4, conditionBlockNum % 4] == 1)
        {
            currentPhyTargetsLayout = PhyTargetsLayouts.A;
        }
        else if (latinSquare4x4[participantID % 4, conditionBlockNum % 4] == 2)
        {
            currentPhyTargetsLayout = PhyTargetsLayouts.B;
        }
        else if (latinSquare4x4[participantID % 4, conditionBlockNum % 4] == 3)
        {
            currentPhyTargetsLayout = PhyTargetsLayouts.C;
        }
        else if (latinSquare4x4[participantID % 4, conditionBlockNum % 4] == 4)
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

    private IEnumerator RemoveResponse()
    {
        float duration = (buildFor == BuildFor.Practice) ? 1f : 0f;
        yield return new WaitForSeconds(duration);
        dottedReticle.transform.position = reticleGameObjects.transform.position;
        stripesReticle.transform.position = reticleGameObjects.transform.position;
        blueReticle.transform.position = reticleGameObjects.transform.position;
        greenReticle.transform.position = reticleGameObjects.transform.position;
        yield return 0;
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

    private void InitializeVirtualTargets(GameObject targetA, GameObject targetB)
    {
        // select far, middle, near for dotted

        // List<float> depthList = new List<float> {1f, 2f, 3f};
        List<float> depthList = new List<float> {1.5f, 2.5f, 3.5f};
        Helpers.Shuffle(depthList); 

        Vector3 posA = Vector3.zero;
        Vector3 posB = Vector3.zero;

        while (Vector3.Distance(posA, posB) < 0.31f || Vector3.Angle(posA, posB) < 10f)
        {
            posA = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, depthList[0] + Helpers.RandomGaussian(-.5f, 0.5f));
            posB = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, depthList[1] + Helpers.RandomGaussian(-.5f, 0.5f));
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
            bluePhysicalTarget.transform.position  = new Vector3(-0.87f, 0, 1.5f); // tan 30
            greenPhysicalTarget.transform.position = new Vector3(-0.67f, 0, 2.5f);  // tan 15
        }
        else if (currentPhyTargetsLayout == PhyTargetsLayouts.B)
        {
            bluePhysicalTarget.transform.position  = new Vector3(-0.2f, 0, 2.5f);
            greenPhysicalTarget.transform.position = new Vector3( 0.8f, 0, 2.5f);
        }
        else if (currentPhyTargetsLayout == PhyTargetsLayouts.C)
        {
            bluePhysicalTarget.transform.position  = new Vector3( 0.3f, 0, 1.5f);
            greenPhysicalTarget.transform.position = new Vector3(-0.7f, 0, 1.5f);
        }
        else
        {
            bluePhysicalTarget.transform.position  = new Vector3(0.67f, 0, 2.5f); // tan 15
            greenPhysicalTarget.transform.position = new Vector3( 1.5f, 0, 1.5f); // tan 45
        }
        bluePhysicalTarget.SetActive(false);
        greenPhysicalTarget.SetActive(false);
    }

    private IEnumerator ShowTargetsAndRetention(float showTimeStamp = 7f, float retentionTimeStamp = 12f)
	{
        currentTime = 0.0f;

        // show a target
        if (currentTarget == Targets.virtualTarget) InitializeVirtualTargets(blueVirtualTarget, greenVirtualTarget);
        else
        {
            Debug.LogWarning("Passthorugh gogogo");
            passthroughLayer.enabled = true;
            lerpTimeElapsed = 0;
            fadeInRW = true;
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
            lerpTimeElapsed = 0;
            fadeInVR = true;
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
        decoys.transform.rotation = Quaternion.identity; // reset decoys
        InitializeVirtualTargets(dottedVirtualTarget, stripesVirtualTarget);
        float decoysRotateAmount = 0f;
        if (currentRotation == SelfRotation.rotate)
        {
            if (decoyNum == 0) decoysRotateAmount = 0f;
            else if (decoyNum == 1) decoysRotateAmount = (whichDirection % 2 == 0) ? -rotationAngleList[decoyNum-1] : rotationAngleList[decoyNum-1];
            else if (decoyNum == 2) decoysRotateAmount = (whichDirection % 2 == 0) ? -(120 - rotationAngleList[decoyNum]) : (120 - rotationAngleList[decoyNum]); // would never run this line :D
            decoys.transform.rotation = Quaternion.Euler(0, decoys.transform.rotation.eulerAngles.y + decoysRotateAmount, 0);
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
        // Debug.LogWarning("Rotation is: " + rotateDirection + ", " + rotateAmount);
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

    private IEnumerator WriteDataList()
    {
        erfStudyWriter.Write(
            "trialID"                       + "," +
            "Participant"                   + "," +
            "LayoutBlockNum"                + "," +
            "ConditionBlockNum"             + "," +
            "TrialNum"                      + "," +
            "LayoutType"                    + "," +
            "Condition"                     + "," +
            "TargetType"                    + "," +
            "PairCount"                     + "," +
            "SelfRotation"                  + "," +
            "RotateDirection"               + "," +
            "DecoyAmount"                   + "," +
            "CurrentDecoy"                  + "," +
            "RotationAmount"                + "," +
            "Baseline"                      + "," +
            "Testing"                       + "," +
            "DecoyBaseline"                 + "," +
            "DecoyTesting"                  + "," +
            "BeginTime"                     + "," + // RT
            "EndTime"                       + "," +
            "ResponsePos_X"                 + "," + // position error
            "ResponsePos_Y"                 + "," + 
            "ResponsePos_Z"                 + "," + 
            "AnsPos_X"                      + "," + 
            "AnsPos_Y"                      + "," + 
            "AnsPos_Z"                      + "," + 
            "TargetName"                    + "," +
            "ControllerPos_X"               + "," + 
            "ControllerPos_Y"               + "," + 
            "ControllerPoss_Z"              + "\n"); 

        foreach (var data in dataList)
        {
            erfStudyWriter.Write(
                "P" + data.participantID.ToString()        + "," +
                data.trialID.ToString()                    + "," +
                data.layoutBlockNum                        + "," +
                data.conditionBlockNum                     + "," +
                data.trialNum                              + "," +
                data.currentPhyTargetsLayout               + "," +
                data.currentCondition                      + "," +
                data.currentTarget                         + "," +
                data.pairCounter                           + "," +
                data.currentRotation                       + "," +
                data.whichDirection                        + "," +
                data.decoyAmountThisTrial                  + "," +
                data.decoyNum                              + "," +
                data.rotationAmount                        + "," +
                data.isBaselineMeasure                     + "," +
                data.isTestingMeasure                      + "," +
                data.isDecoyBaseline                       + "," +
                data.isDecoyTesting                        + "," +
                data.beginTime.ToString("F6")              + "," +
                data.endTime.ToString("F6")                + "," +
                data.responsePos.x.ToString("F6")          + "," +
                data.responsePos.y.ToString("F6")          + "," +
                data.responsePos.z.ToString("F6")          + "," +
                data.groundTruthPos.x.ToString("F6")       + "," +
                data.groundTruthPos.y.ToString("F6")       + "," +
                data.groundTruthPos.z.ToString("F6")       + "," +
                data.groundTruthName                       + "," +
                data.controllerPos.x.ToString("F6")        + "," +
                data.controllerPos.y.ToString("F6")        + "," +
                data.controllerPos.z.ToString("F6")        + "\n"
            );
        }

        erfStudyWriter.Flush();
        erfStudyWriter.Close();
        // Change Scene
        SceneManager.LoadScene("ERFv2_Questionnaire");
        yield return 0;
    }

    private void AddData()
    {
        if (isDecoyRunning) groundTruth = decoyTargetList[pairCounter];
        else
        {
            if (currentTarget == Targets.virtualTarget) groundTruth = virtualTargetList[pairCounter];
            else groundTruth = physicalTargetList[pairCounter];
        }

        int trialID = layoutBlockNum*8 + conditionBlockNum*4 + trialNum;

        dataList.Add(new PointingData(participantID , trialID, layoutBlockNum, conditionBlockNum, trialNum,
            currentPhyTargetsLayout.ToString(), currentCondition.ToString(), currentTarget.ToString(), pairCounter,
            currentRotation.ToString(), whichDirection, decoyAmountThisTrial, decoyNum, rotationAngleList[decoyNum],
            isBaselineMeasure, isTestingMeasure, isDecoyBaseline, isDecoyTesting, beginTimeStamp, endTimeStamp, 
            responsePos, groundTruth.transform.position, groundTruth.name, controller.transform.position));

        Debug.LogWarning(// Response Info
            trialID                                                                             + ", " +
            "Participant: P" +          participantID.ToString()                                + ", " +
            "Layout Block Num: " +      layoutBlockNum                                          + ", " +
            "Condition Block Num: " +   conditionBlockNum                                       + ", " +
            "Trial: " +                 trialNum                                                + ", " +
            "Layout Type: " +           currentPhyTargetsLayout.ToString()                      + ", " +
            "Condition: " +             currentCondition.ToString()                             + ", " +
            "TargetType: " +            currentTarget.ToString()                                + ", " +
            "PairCount: " +             pairCounter                                             + ", " +
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
            "ResponsePos: " +           responsePos.ToString("F6")                              + ", " +
            "AnsPos: " +                groundTruth.transform.position.ToString("F6")           + ", " +
            "TargetName: " +            groundTruth.name                                        + ", " +
            "ControllerPos: " +         controller.transform.position.ToString("F6")            + "\n"); 
    }

    public struct PointingData
    {
        public int participantID;
        public int trialID;
        public int layoutBlockNum;
        public int conditionBlockNum;
        public int trialNum;
        public string currentPhyTargetsLayout;
        public string currentCondition;
        public string currentTarget;
        public int pairCounter;
        public string currentRotation;
        public int whichDirection;
        public int decoyAmountThisTrial;
        public int decoyNum;
        public int rotationAmount;
        public bool isBaselineMeasure;
        public bool isTestingMeasure;
        public bool isDecoyBaseline;
        public bool isDecoyTesting;
        public float beginTime;
        public float endTime;
        public Vector3 responsePos;
        public Vector3 groundTruthPos;
        public string groundTruthName;
        public Vector3 controllerPos;

        public PointingData(int participantID, int trialID, int layoutBlockNum, int conditionBlockNum, int trialNum,
                            string currentPhyTargetsLayout, string currentCondition, string currentTarget, int pairCounter,
                            string currentRotation, int whichDirection, int decoyAmountThisTrial, int decoyNum, int rotationAmount,
                            bool isBaselineMeasure, bool isTestingMeasure, bool isDecoyBaseline, bool isDecoyTesting,
                            float beginTime, float endTime, Vector3 responsePos, Vector3 groundTruthPos, string groundTruthName, Vector3 controllerPos)
        {
            this.participantID = participantID;
            this.trialID = trialID;
            this.layoutBlockNum = layoutBlockNum;
            this.conditionBlockNum = conditionBlockNum;
            this.trialNum = trialNum;
            this.currentPhyTargetsLayout = currentPhyTargetsLayout;
            this.currentCondition = currentCondition;
            this.currentTarget = currentTarget;
            this.pairCounter = pairCounter;
            this.currentRotation = currentRotation;
            this.whichDirection = whichDirection;
            this.decoyAmountThisTrial = decoyAmountThisTrial;
            this.decoyNum = decoyNum;
            this.rotationAmount = rotationAmount;
            this.isBaselineMeasure = isBaselineMeasure;
            this.isTestingMeasure = isTestingMeasure;
            this.isDecoyBaseline = isDecoyBaseline;
            this.isDecoyTesting = isDecoyTesting;
            this.beginTime = beginTime;
            this.endTime = endTime;
            this.responsePos = responsePos;
            this.groundTruthPos = groundTruthPos;
            this.groundTruthName = groundTruthName;
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

    public static string CreateDataPath(int id, string note = "")
    {
        string fileName = "P" + id.ToString() + note + "_erf.csv";
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