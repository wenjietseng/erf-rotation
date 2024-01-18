using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Meta.WitAi.Attributes;
using Unity.VisualScripting.ReorderableList;

public class QuestionnaireController : MonoBehaviour
{
    public int participantID;
    public TMP_Text mainText;
    public GameObject scale;
    public Button startButton;
    public Button confirmButton;
    public GameObject controller; 
    public LayerMask pointingMask;
    public List<string> items;
    public GameObject laser;



    private RaycastHit hit;
    private Vector3 hitPoint;
    private Transform laserTransform;
    // SPES: All items were designed to be answered on a 5-point Likert scale ranging from 1 (= I do not agree at all) to 5 (= I fully agree).
    private string sl01 = "I felt like I was actually there in the environment of the presentation.";
    private string sl02 = "It seemed as though I actually took part in the action of the presentation.";
    private string sl03 = "It was as though my true location had shifted into the environment in the presentation.";
    private string sl04 = "I felt as though I was physically present in the environment of the presentation.";
    private string pa01 = "The objects in the presentation gave me the feeling that I could do things with them.";
    private string pa02 = "I had the impression that I could be active in the environment of the presentation.";
    private string pa05 = "I felt like I could move around among the objects in the presentation.";
    private string pa08 = "It seemed to me that I could do whatever I wanted in the environment of the presentation.";

    void Start()
    {
        scale.SetActive(false);
        laser.SetActive(false);
        confirmButton.enabled = false;
        laserTransform = laser.transform;
        items.Add(sl01);
        items.Add(sl02);
        items.Add(sl03);
        items.Add(sl04);
        items.Add(pa01);
        items.Add(pa02);
        items.Add(pa05);
        items.Add(pa08);
        Helpers.Shuffle(items);

    }

    void Update()
    {
        if (Physics.Raycast(controller.transform.position, controller.transform.forward, out hit, 200, pointingMask))
        {
            hitPoint = hit.point;
            Debug.LogWarning(hit.transform.name);
            mainText.text = hit.transform.name;

            laser.SetActive(true);
            laserTransform.position = Vector3.Lerp(controller.transform.position, hitPoint, .5f); // move laser to the middle
            laserTransform.LookAt(hitPoint); // rotate and face the hit point
            laserTransform.localScale = new Vector3(laserTransform.localScale.x, laserTransform.localScale.y, Vector3.Distance(controller.transform.position, hitPoint));
        }
        else
        {
            laser.SetActive(false);
        }
    }



}
