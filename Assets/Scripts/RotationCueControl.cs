using System.Collections;
using System.Collections.Generic;
using System.Security;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class RotationCueControl : MonoBehaviour
{
    public GameObject cam; // hmd
    public Image uiFill;
    public LayerMask pointingMask;
    public GameObject outOfViewIndicator;
    public GameObject visualCollider;
    public float remainingDuration;
    public float duration;
    public static bool isCueComplete;
    bool isConfirmed;
    RaycastHit hitVisualCue;

    void Start()
    {
           
    }

    void Update()
    {
        // Debug.LogWarning(ExperimentController.selfRotationDuration);
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hitVisualCue, 200, pointingMask))
        {
            if (hitVisualCue.transform.name == "Visual Collider" && !isConfirmed)
            {
                visualCollider.SetActive(false);

                if (ExperimentController.selfRotationDuration < 1) Being(0f);
                else Being(duration);
                isCueComplete = false;
                isConfirmed = true;
                if (outOfViewIndicator.activeSelf) outOfViewIndicator.SetActive(false);
            }
        }
    }

    private void Being(float second)
    {
        uiFill.fillAmount = 1.0f;
        remainingDuration = second;
        StartCoroutine(UpdateTimer());
    }

    private IEnumerator UpdateTimer()
    {
        while (remainingDuration >= 0)
        {
            uiFill.fillAmount = Mathf.InverseLerp(0, duration, remainingDuration);
            remainingDuration -= Time.deltaTime;
            yield return 0;
        }
        OnEnd();
    }

    private void OnEnd()
    {
        visualCollider.SetActive(true);        
        isConfirmed = false;
        uiFill.fillAmount = 0;
        print("Ënd Visual Cue");
        
        isCueComplete = true;
    }
}
