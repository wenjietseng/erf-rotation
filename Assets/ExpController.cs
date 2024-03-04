using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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
    
    void Start()
    {
        Helpers.Shuffle(physicalTargetList);
        Helpers.Shuffle(virtualTargetList);
        bluePhysicalTarget.SetActive(false);
        greenPhysicalTarget.SetActive(false);
        dottedVirtualTarget.SetActive(false);
        stripesVirtualTarget.SetActive(false);



    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            InitializeVirtualTargets();
        }
        
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
            Debug.LogWarning("Distance: " + Vector3.Distance(dottedPos, stripesPos) + "\nAngle: " + Vector3.Angle(dottedPos, stripesPos));
        }

        dottedVirtualTarget.SetActive(true);
        stripesVirtualTarget.SetActive(true);
        dottedVirtualTarget.transform.position = dottedPos;
        stripesVirtualTarget.transform.position = stripesPos;
    }
}
