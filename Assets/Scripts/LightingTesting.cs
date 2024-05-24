using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightingTesting : MonoBehaviour
{

    public GameObject envLight;
    public GameObject spotLight;
    float lerpTimeElapsed = 0;
    float lerpDuration = 1;
    public bool lightsOff;
    public bool lightsOn;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        if (lightsOff)
        {
            if (lerpTimeElapsed < lerpDuration)
            {
                spotLight.GetComponent<Light>().enabled = true;


                envLight.transform.eulerAngles = new Vector3 (Mathf.Lerp(30, -10, lerpTimeElapsed / lerpDuration), 0, 0);
                spotLight.GetComponent<Light>().spotAngle = Mathf.Lerp(0, 100, lerpTimeElapsed / lerpDuration);
                lerpTimeElapsed += Time.deltaTime;
            }
            else
            {
                lightsOff = false;
                lerpTimeElapsed = 0;
            }
        }

        if (lightsOn)
        {
            if (lerpTimeElapsed < lerpDuration)
            {

                spotLight.GetComponent<Light>().enabled = true;


                envLight.transform.eulerAngles = new Vector3(Mathf.Lerp(-10, 30, lerpTimeElapsed / lerpDuration), 0, 0);
                spotLight.GetComponent<Light>().spotAngle = Mathf.Lerp(100, 0, lerpTimeElapsed / lerpDuration);
                lerpTimeElapsed += Time.deltaTime;
            }
            else
            {
                lightsOn = false;

                spotLight.GetComponent<Light>().enabled = false;
                lerpTimeElapsed = 0;
            }
        }
    }
}
