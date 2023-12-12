using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ExperimentController : MonoBehaviour
{

    public GameObject uiPanel;
    public GameObject startButton;
    public Logger logger;


    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void StartCondition()
    {
        logger.LogWarning("Condition starts");
        startButton.SetActive(false);
        uiPanel.SetActive(false);
        
    }
}
