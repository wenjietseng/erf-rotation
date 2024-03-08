using System.Collections;
using System.Collections.Generic;
using System.Security;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SimpleRotationCue : MonoBehaviour
{
    public GameObject cam; // hmd
    public LayerMask layerMask;
    public GameObject visualCollider;
    public static bool isCueComplete;
    RaycastHit hitVisualCue;

    void Update()
    {
        // Debug.LogWarning(ExperimentController.selfRotationDuration);
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hitVisualCue, 200, layerMask))
        {
            if (hitVisualCue.transform.name == "Visual Collider" && !isCueComplete)
            {
                visualCollider.SetActive(false);
                isCueComplete = true;
            }
        }
    }
}
