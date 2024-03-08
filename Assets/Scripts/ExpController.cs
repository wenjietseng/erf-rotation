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

    [Header("Stimuli and Materials")]
    public GameObject bluePhysicalTarget;
    public GameObject greenPhysicalTarget;
    public GameObject dottedVirtualTarget;
    public GameObject stripesVirtualTarget;
    public List<GameObject> physicalTargetList;
    public List<GameObject> virtualTargetList;
    // for procedures
    public GameObject StartTrialPanel;
    public GameObject rotationCue;

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
    Vector3 vr_firstBaselineResponse;
    Vector3 vr_secondBaselineResponse;
    Vector3 vr_firstTestResponse;
    Vector3 vr_secondTestResponse;
    Vector3 rw_firstBaselineResponse;
    Vector3 rw_secondBaselineResponse;
    Vector3 rw_firstTestResponse;
    Vector3 rw_secondTestResponse;

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
        rotationCue.SetActive(false);


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

        if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.LTouch))
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
                    if (isTrialRunning) // this seems to be physical trial, we might need to separate virtual and physical
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
                                        vr_firstBaselineResponse = hitPoint;
                                    }
                                    else
                                    {
                                        vr_secondBaselineResponse = hitPoint;
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
                                        StartCoroutine(ShowRotateOrientation(endTimeStamp + 5.0f)); // test without rotate
                                        // StartCoroutine(ShowRotateOrientation(endTimeStamp + 5.0f, 120f)); // test with rotate
                                        Debug.LogWarning("Go to rotation and testing");
                                    }
                                }
                            }
                            else
                            {
                                DisablePointing();
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
                                    if (pairCounter == 0)
                                    {
                                        vr_firstTestResponse = hitPoint;
                                    }
                                    else
                                    {
                                        vr_secondTestResponse = hitPoint;
                                    }
                                    // show ans
                                    StartCoroutine(ShowAns());

                                    endTimeStamp = currentTime;
                                    AddData();

                                    // update recticle
                                    pairCounter += 1;
                                    beginTimeStamp = currentTime;

                                    if (pairCounter == 2)
                                    {
                                        // reset variables
                                        isTestingMeasure = false;
                                        DisablePointing(); 
                                        StartCoroutine(RemoveResponse()); // practice 1s, formal study 0s
                                        pairCounter = 0;
                                        this.transform.rotation = Quaternion.Euler(0, 0, 0);
                                        isTrialRunning = false;
                                        // restingTime = 0;

                                        if (trialNum < 3) 
                                        {
                                            trialNum += 1;
                                        }
                                        else
                                        {
                                            trialNum = 0;
                                            conditionBlockNum += 1;
                                            // Helpers.Shuffle(conditionArray);
                                            // Helpers.Shuffle(signs);
                                        }
                                        // PrepareTrial();

                                        // StartTrialPanel.SetActive(true);
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

    public void PrepareTrial()
    {
        // if (conditionArray[trialNum] == 0) 
        // {
        //     currentCondition = Conditions.virtualStatic;
        //     currentRotation = SelfRotation.none;
        //     currentTarget = Targets.virtualTarget;
        // }
        // else if (conditionArray[trialNum] == 1)
        // {
        //     currentCondition = Conditions.virtualRotate;
        //     currentRotation = SelfRotation.rotate;
        //     currentTarget = Targets.virtualTarget;
        // }
        // else if (conditionArray[trialNum] == 2)
        // {
        //     currentCondition = Conditions.physicalStatic;
        //     currentRotation = SelfRotation.none;
        //     currentTarget = Targets.physicalTarget;
        // }
        // else
        // {
        //     currentCondition = Conditions.physicalRotate;
        //     currentRotation = SelfRotation.rotate;
        //     currentTarget = Targets.physicalTarget;
        // }

        // if (currentTarget == Targets.physicalTarget)
        // {
        //     InitializePhysicalCylinderOnArc();
        // }
        // else
        // {
        //     /// turn back to the center? or we use the current forward angle as center?
        // }
    }


    private void AddData()
    {
        /// Need to consider virtual/physical targets...
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
                        "TargetName" +         virtualTargetList[pairCounter].name     + ", " +
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

    public IEnumerator RunOneVirtualTrial(float _showTimeStamp = 5.0f, float _retentionTimeStamp = 10.0f)
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
        Helpers.Shuffle(virtualTargetList); // randomized the first target to recall
        isBaselineMeasure = true;
        yield return 0;
    }

    public IEnumerator ShowRotateOrientation(float _showTimeStamp, float rotation_amount = 0f)
    {
        rotationCue.SetActive(true);
        if (currentRotation == SelfRotation.none)
        {
            this.transform.rotation = Quaternion.Euler(0, 0, 0); 
        }
        else
        {
            this.transform.rotation = Quaternion.Euler(0, this.transform.rotation.eulerAngles.y + rotation_amount, 0);
            // need to show another audio cue or use a visual cue
        }

        yield return new WaitUntil(() => currentTime > _showTimeStamp && SimpleRotationCue.isCueComplete);
        rotationCue.SetActive(false);
        RotationCueControl.isCueComplete = false;
        
        // prepare for the pointing task
        beginTimeStamp = currentTime;
        pointingIndicator.Play();
        laser.SetActive(true);
        
        // recall order depending which target had a better performance in the baseline measure
        // can we make this one into a function?
        float firstDistError = Vector3.Distance(virtualTargetList[0].transform.position, vr_firstBaselineResponse);
        float secondDistError = Vector3.Distance(virtualTargetList[1].transform.position, vr_secondBaselineResponse);
        if (firstDistError > secondDistError)
        {
            virtualTargetList.Reverse();
        }

        // if (blockNum < 2) instructionText.text = "Once see the raycast, point\nto the target and confirm with Button A.";
        isTestingMeasure = true;
        yield return 0;
    }
}
