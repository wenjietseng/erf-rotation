using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CalibrateControl : MonoBehaviour
{
    public GameObject physicalCube;
    public TMP_Text text;
    public GameObject leftController;
    public LayerMask pointingMask;
    public GameObject laser;
    Transform laserTransform;
    RaycastHit hit;
    Vector3 hitPoint;

    // Start is called before the first frame update
    void Start()
    {
        laserTransform = laser.transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (Physics.Raycast(leftController.transform.position, -leftController.transform.up, out hit, 200, pointingMask))
        {
            hitPoint = hit.point;
            // physicalCube.transform.position = new Vector3(hitPoint.x, 0.05f, hitPoint.z);
            laser.SetActive(true);
            laserTransform.position = Vector3.Lerp(leftController.transform.position, hitPoint, .5f); // move laser to the middle
            laserTransform.LookAt(hitPoint); // rotate and face the hit point
            laserTransform.localScale = new Vector3(laserTransform.localScale.x, laserTransform.localScale.y, Vector3.Distance(leftController.transform.position, hitPoint));
        }
        
        float dist = Vector3.Distance(hitPoint, Vector3.zero);

        text.text = physicalCube.transform.position.ToString("F3") + "\n" + dist.ToString("F3");
    }
}
