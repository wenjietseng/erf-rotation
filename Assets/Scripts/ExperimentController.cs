using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExperimentController : MonoBehaviour
{
    [Header("User Info")]
    public int participantID = 0;
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
    public GameObject rectify;
    public GameObject virtualRectify;
    public GameObject physicalRectify;
    public GameObject laser;
    private Transform laserTransform;
     
    // public GameObject uiPanel;
    // public GameObject startButton;
    // public Logger logger;
    public FadeEffect fadeEffect;
    public GameObject cam;
    public Vector3 hitPoint;


    bool isTrialRunning;
    bool isBaselineMeasure;
    bool isTestingMeasure;
    float beginTimeStamp;
    float endTimeStamp;
    Ray ray;
    RaycastHit hit;
    float rad = 55.0f * Mathf.Deg2Rad; // Degrees-to-radians conversion constant (Read Only). This is equal to (PI * 2) / 360.

    void Start()
    {
        
        fadeEffect = this.GetComponent<FadeEffect>();
        fadeEffect.fadeInEffect();
        conditionArray = new List<int> {0, 1, 2, 3};
        laserTransform = laser.transform;

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
        if (Input.GetKeyDown(KeyCode.Q)) InitializeVirtualCubePos();
        // if (Input.GetKeyDown(KeyCode.W)) this.transform.rotation = Quaternion.Euler(0, 120, 0);


        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            // replace this part with 3D UI
            isTrialRunning = true;
            StartCoroutine(ShowTargetAndRetention());
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
                        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        if (Physics.Raycast(ray, out hit, 100))
                        {
                            hitPoint = hit.point;
                            Vector3 vec = hitPoint - cam.transform.position;
                            // Debug.DrawRay(cam.transform.position, vec, Color.green, 0.02f); // for debug
                            rectify.transform.position = hitPoint;
                            UpdateLaser();

                            // press button to confirm the respond
                            if (Input.GetMouseButtonDown(0))
                            {
                                endTimeStamp = currentTime;
                                // record baseline measure (two time stamps, target position, selected position)
                                Debug.LogWarning("Block: " +           blockNum                                + ", " +
                                                "Trial: " +            trialNum                                + ", " +
                                                "Self-Rotation: " +    currentRotation.ToString()              + ", " +
                                                "TargetType: " +       currentTarget.ToString()                + ", " +
                                                "Baseline: " +         isBaselineMeasure                       + ", " +
                                                "BeginTime: " +        beginTimeStamp.ToString("F6")           + ", " +
                                                "EndTime: " +          endTimeStamp.ToString("F6")             + ", " +
                                                "TargetPos: " +        targetTransform.position.ToString("F6") + ", " +
                                                "SelectedPos: " +      hitPoint.ToString("F6")                 + ", " +
                                                "ControllerPos: " +    cam.transform.position.ToString("F6")   + ", "); // change to controller
                                // reset variables
                                isBaselineMeasure = false;
                                laser.SetActive(false);
                                rectify.SetActive(false);
                                // show arrow
                                StartCoroutine(ShowRotateOrientation(endTimeStamp + 5.0f));
                            }
                        }
                    }
                    
                    if (isTestingMeasure)
                    {
                        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        if (Physics.Raycast(ray, out hit, 100))
                        {
                            hitPoint = hit.point;
                            Vector3 vec = hitPoint - cam.transform.position;
                            // Debug.DrawRay(cam.transform.position, vec, Color.green, 0.02f); // for debug
                            rectify.transform.position = hitPoint;
                            UpdateLaser();

                            // press button to confirm the respond
                            if (Input.GetMouseButtonDown(0))
                            {
                                endTimeStamp = currentTime;
                                // record baseline measure (two time stamps, target position, selected position)
                                Debug.LogWarning("Block: " +           blockNum                                + ", " +
                                                "Trial: " +            trialNum                                + ", " +
                                                "Self-Rotation: " +    currentRotation.ToString()              + ", " +
                                                "TargetType: " +       currentTarget.ToString()                + ", " +
                                                "Baseline: " +         isBaselineMeasure                       + ", " +
                                                "BeginTime: " +        beginTimeStamp.ToString("F6")           + ", " +
                                                "EndTime: " +          endTimeStamp.ToString("F6")             + ", " +
                                                "TargetPos: " +        targetTransform.position.ToString("F6") + ", " +
                                                "SelectedPos: " +      hitPoint.ToString("F6")                 + ", " +
                                                "ControllerPos: " +    cam.transform.position.ToString("F6")   + ", "); // change to controller
                                // reset variables
                                isTestingMeasure = false;
                                laser.SetActive(false);
                                rectify.SetActive(false);
                                this.transform.rotation = Quaternion.Euler(0, 0, 0);
                                arrow.SetActive(true);
                                isTrialRunning = false;
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
                                PrepareTrial();
                            }
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
        }
        else
        {
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
        // show a target
        if (currentTarget == Targets.virtualTarget)
        {
            virtualCube.SetActive(true);
            //////////////////////////////
            // TODO
            // instantiate object position
            //////////////////////////////
            targetTransform = virtualCube.transform;
        }
        else 
        {
            physicalCylinder.SetActive(true);
            //////////////////////////////
            // TODO
            // instantiate object position if needed
            //////////////////////////////
            targetTransform = physicalCylinder.transform;
        } 
        yield return new WaitUntil(() => currentTime > _showTimeStamp);
        // retention starts
        if (currentTarget == Targets.virtualTarget) virtualCube.SetActive(false);
        else physicalCylinder.SetActive(false);
        fadeEffect.fadeInBlackEffect();
        yield return new WaitUntil(() => currentTime > _retentionTimeStamp);
        // prepare for the pointing task
        fadeEffect.fadeInEffect();
        rectify.SetActive(true);
        if (currentTarget == Targets.virtualTarget)
        {
            virtualRectify.SetActive(true);
        }
        else
        {
            physicalRectify.SetActive(true);
        }
        laser.SetActive(true);
        isBaselineMeasure = true;
        beginTimeStamp = currentTime;
        yield return 0;
    }

    private void UpdateLaser()
    {
        laserTransform.position = Vector3.Lerp(cam.transform.position, hitPoint, .5f); // move laser to the middle
        laserTransform.LookAt(hitPoint); // rotate and face the hit point
        laserTransform.localScale = new Vector3(laserTransform.localScale.x, laserTransform.localScale.y, Vector3.Distance(cam.transform.position, hitPoint));
    }


    // public void StartCondition()
    // {
    //     // logger.LogWarning("Condition starts");
    //     // startButton.SetActive(false);
    //     // uiPanel.SetActive(false);
        
    // }
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
