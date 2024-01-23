using System.Collections;
using System.Collections.Generic;
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
    public float remainingDuration;
    public float duration;
    public static bool isCueComplete;
    bool isConfirming;
    RaycastHit hitVisualCue;

    void Start()
    {
           
    }

    void Update()
    {
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hitVisualCue, 200, pointingMask))
        {
            if (hitVisualCue.transform.name == "Visual Collider" && !isConfirming)
            {
                // Debug.LogWarning("yaassss");  
                Being(duration);
                isConfirming = true;
                isCueComplete = false;
                if (outOfViewIndicator.activeSelf) outOfViewIndicator.SetActive(false);
            }
        }
        else
        {
            isConfirming = false;
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
        isCueComplete = true;
        uiFill.fillAmount = 0;
        print("Ënd Visual Cue");
    }
}
