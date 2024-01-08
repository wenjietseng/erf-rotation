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
     
    // public GameObject uiPanel;
    // public GameObject startButton;
    // public Logger logger;
    public FadeEffect fadeEffect;
    public GameObject cam;


    bool isTrialRunning;
    bool isBaselineMeasure;
   
    void Start()
    {
        // setting for the study
        fadeEffect = this.GetComponent<FadeEffect>();
        fadeEffect.fadeInEffect();
        virtualCube.SetActive(false);
        physicalCylinder.SetActive(false);
        arrow.SetActive(false);
        conditionArray = new List<int> {0, 1, 2, 3};

        // initialize the first block
        // Helpers.Shuffle(conditionArray);

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

    void Update()
    {
        // testing
        if (Input.GetKeyDown(KeyCode.Q)) fadeEffect.fadeInEffect();
        if (Input.GetKeyDown(KeyCode.W)) fadeEffect.fadeInBlackEffect(); 


        // fixedupdate?
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100))
        {
            Vector3 vec = hit.point - cam.transform.position;
            Debug.DrawRay(cam.transform.position, vec, Color.red, 0.02f);

        }






        // keep tracking time for each trial
        if (isTrialRunning)
        {
            currentTime += Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            isTrialRunning = true;
            StartCoroutine(ShowTargetAndRetention());
        }


        // this is for one block
        if (trialNum < 4)
        {
            // this is for one trial
            
            if (isBaselineMeasure)
            {


                // keep updating retify and raycast
                // record baseline measure (two time stamps, target position, selected position)
                // showOrientation procedure
            }


        }
        
    }

    public IEnumerator ShowTargetAndRetention(float _showTimeStamp = 5.0f, float _retentionTimeStamp = 10.0f)
	{
        // show a target
        if (currentTarget == Targets.virtualTarget)
        {
            virtualCube.SetActive(true);
            //////////////////////////////
            // TODO
            // instantiate object position
            //////////////////////////////
        }
        else 
        {
            physicalCylinder.SetActive(true);
            //////////////////////////////
            // TODO
            // instantiate object position
            //////////////////////////////
        } 
        yield return new WaitUntil(() => currentTime > _showTimeStamp);
        // retention starts
        if (currentTarget == Targets.virtualTarget) virtualCube.SetActive(false);
        else physicalCylinder.SetActive(false);
        fadeEffect.fadeInBlackEffect();
        yield return new WaitUntil(() => currentTime > _retentionTimeStamp);
        // prepare for the baseline measure
        fadeEffect.fadeInEffect();
        //////////////////////////////
        // TODO
        // Initiate retify and raycast
        //////////////////////////////
        isBaselineMeasure = true;
        yield return 0;
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
