using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;

public class QuestionnaireController : MonoBehaviour
{
    public int participantID;
    public TMP_Text mainText;
    public TMP_Text smallInstruction;
    public TMP_Text largeInstruction;
    public GameObject scale;
    public List<GameObject> scales;
    public List<int> responses;
    private List<QuestionnaireData> items = new List<QuestionnaireData>();
    // SPES: All items were designed to be answered on a 5-point Likert scale ranging from 1 (= I do not agree at all) to 5 (= I fully agree).
    private QuestionnaireData sl01;
    private QuestionnaireData sl02;
    private QuestionnaireData sl03;
    private QuestionnaireData sl04;
    private QuestionnaireData pa01;
    private QuestionnaireData pa02;
    private QuestionnaireData pa05;
    private QuestionnaireData pa08;
    private GameObject currentScaleGO;
    private bool isStart;
    private bool isAllowedCheck;
    private bool isEnd;
    private int currentScale;
    public int currentItem;
    private StreamWriter questionnaireWriter;
    // public FadeEffect fadeEffect;
    

    void Start()
    {
        // fadeEffect = this.GetComponent<FadeEffect>();
        // fadeEffect.fadeInEffect();

        string questionnairePath = Helpers.CreateDataPath(participantID, "_questionnaire");
        questionnaireWriter = new StreamWriter(questionnairePath, true);

        isStart = false;
        scale.SetActive(false);
        sl01 = new QuestionnaireData("P" + participantID.ToString(), "I felt like I was actually there in the environment of the presentation.");
        sl02 = new QuestionnaireData("P" + participantID.ToString(), "It seemed as though I actually took part in the action of the presentation.");
        sl03 = new QuestionnaireData("P" + participantID.ToString(), "It was as though my true location had shifted into the environment in the presentation.");
        sl04 = new QuestionnaireData("P" + participantID.ToString(), "I felt as though I was physically present in the environment of the presentation.");
        pa01 = new QuestionnaireData("P" + participantID.ToString(), "The objects in the presentation gave me the feeling that I could do things with them.");
        pa02 = new QuestionnaireData("P" + participantID.ToString(), "I had the impression that I could be active in the environment of the presentation.");
        pa05 = new QuestionnaireData("P" + participantID.ToString(), "I felt like I could move around among the objects in the presentation.");
        pa08 = new QuestionnaireData("P" + participantID.ToString(), "It seemed to me that I could do whatever I wanted in the environment of the presentation.");
        items.Add(sl01);
        items.Add(sl02);
        items.Add(sl03);
        items.Add(sl04);
        items.Add(pa01);
        items.Add(pa02);
        items.Add(pa05);
        items.Add(pa08);
        responses = new List<int>{0, 0, 0, 0, 0, 0, 0, 0};
        Helpers.Shuffle(items);
        currentItem = 0;
        currentScale = 3;
        currentScaleGO = scales[currentScale-1];
        smallInstruction.text = "";
        largeInstruction.text = "Press A to Start.";
        isAllowedCheck = false;
        
        foreach (var s in scales) s.SetActive(false);
    }

    void Update()
    {
        if (!isStart)
        {
            if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch)) //(Input.GetKeyDown(KeyCode.A))
            {
                if (!isEnd)
                {
                    isStart = true;
                    mainText.text = items[currentItem].item;
                    scale.SetActive(true);
                    currentScaleGO.SetActive(true);
                    smallInstruction.text = (currentItem + 1).ToString("F0") + "/8";
                    largeInstruction.text = "Use Left/Right to select a response and press A to confrim.";
                }
            }
        }
        else
        {
            // collecting questionnaire data
            if (OVRInput.GetUp(OVRInput.Button.PrimaryThumbstickLeft, OVRInput.Controller.RTouch)) //(Input.GetKeyDown(KeyCode.LeftArrow))
            {
                if (currentScale > 1)
                {
                    currentScaleGO.SetActive(false);
                    currentScale -= 1;
                    scales[currentScale-1].SetActive(true);
                    currentScaleGO = scales[currentScale-1];
                }
            }
            else if (OVRInput.GetUp(OVRInput.Button.PrimaryThumbstickRight, OVRInput.Controller.RTouch)) // (Input.GetKeyDown(KeyCode.RightArrow))
            {
                if (currentScale < 5)
                {
                    currentScaleGO.SetActive(false);
                    currentScale += 1;
                    scales[currentScale-1].SetActive(true);
                    currentScaleGO = scales[currentScale-1];
                }
            }
            else if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch)) // (Input.GetKeyDown(KeyCode.A))
            {
                if (currentItem < 8)
                {
                    responses[currentItem] = currentScale;
                    currentScaleGO.SetActive(false);
                    Debug.LogWarning(currentItem + ", " + 
                                        items[currentItem].item + ", " +
                                        responses[currentItem].ToString("F0"));

                    if (!isAllowedCheck)
                    {
                        currentItem += 1;
                        if (currentItem < 7)
                        {
                            currentScale = 3;
                            currentScaleGO = scales[currentScale-1];
                        }
                    }
                    else
                    {
                        foreach (var s in scales) s.SetActive(false);
                        scales[responses[currentItem]-1].SetActive(true);
                        currentScale = responses[currentItem];
                        currentScaleGO = scales[responses[currentItem]-1];
                    }
                    currentScaleGO.SetActive(true);

                    if (currentItem < 8)
                    {
                        mainText.text = items[currentItem].item;
                        smallInstruction.text = (currentItem + 1).ToString("F0") + "/8";
                    }
                    else 
                    {
                        smallInstruction.text = "Use Left/Right to select a response and press A to confrim.\n" + currentItem.ToString("F0") + "/8";
                        largeInstruction.text = "Use Up/Down to check your responses and Press B to end the test.";
                        isAllowedCheck = true;
                        currentItem = 7;
                    }
                }
            }

            if (isAllowedCheck)
            {
                // once fill out everything
                if (OVRInput.GetUp(OVRInput.Button.PrimaryThumbstickUp, OVRInput.Controller.RTouch)) //(Input.GetKeyDown(KeyCode.UpArrow))
                {
                    if (currentItem > 0)
                    {
                        currentItem -= 1;
                        smallInstruction.text = "Use Left/Right to select a response and press A to confrim.\n" + (currentItem + 1).ToString("F0") + "/8";
                        mainText.text = items[currentItem].item;
                        foreach (var s in scales) s.SetActive(false);
                        scales[responses[currentItem]-1].SetActive(true);
                        currentScale = responses[currentItem];
                        currentScaleGO = scales[responses[currentItem]-1];
                    }
                }
                else if (OVRInput.GetUp(OVRInput.Button.PrimaryThumbstickDown, OVRInput.Controller.RTouch)) // (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    if (currentItem < 7)
                    {
                        currentItem += 1;
                        smallInstruction.text = "Use Left/Right to select a response and press A to confrim.\n" + (currentItem + 1).ToString("F0") + "/8";
                        mainText.text = items[currentItem].item;
                        foreach (var s in scales) s.SetActive(false);
                        scales[responses[currentItem]-1].SetActive(true);
                        currentScale = responses[currentItem];
                        currentScaleGO = scales[responses[currentItem]-1];
                    }

                }
                else if (OVRInput.GetUp(OVRInput.Button.Two, OVRInput.Controller.RTouch)) // (Input.GetKeyDown(KeyCode.B))
                {
                    StartCoroutine(WriteQuestionnaireData());
                    Debug.LogWarning("End, Write data");
                    isStart = false;
                    isEnd = true;
                    scale.SetActive(false);
                    smallInstruction.text = "";
                    largeInstruction.text = "";
                    mainText.text = "This is the end of the study.\nPlease contact the experimentor, thanks!";
                }
            }
        }
    }

    IEnumerator WriteQuestionnaireData()
    {
        questionnaireWriter.Write("ParticipantID"   + "," +
                                  "Item"            + "," +
                                  "Response"        + "\n");
        for (int i = 0; i < 8; i++)
        {
            questionnaireWriter.Write(  items[i].participantID + "," +
                                        items[i].item          + "," +
                                        responses[i]           + "\n");
        }                          
        questionnaireWriter.Flush();
        questionnaireWriter.Close();
        yield return 0;
    }

    public struct QuestionnaireData
    {
        public string participantID;
        public string item;

        public QuestionnaireData(string participantID, string item)
        {
            this.participantID = participantID;
            this.item = item;
        }
    }
}
