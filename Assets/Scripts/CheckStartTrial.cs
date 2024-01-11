using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckStartTrial : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.name == "RightOVRControllerPrefab")
        {
            ExperimentController.isStartTrialPanelTriggered = true;
        }
    }

}
