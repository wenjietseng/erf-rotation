using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class FadeEffect : MonoBehaviour {

	private Renderer fadeRenderer;
	bool fadeIn = false;
	bool fadeOut = false;
	float counter = 0;
	float speed = 3.0f;
	Action fadeInCallBack;
	Action fadeInBlackCallBack;

	void Start ()
	{
		fadeRenderer = GameObject.Find("FadeEffect").GetComponent<Renderer>();
	}
	
	void Update ()
	{
		if(fadeIn)
		{
			counter -= Time.deltaTime * speed;
			fadeRenderer.sharedMaterial.SetFloat("_Alpha", counter);
			if(counter <= 0)
			{
				fadeIn = false;
				fadeRenderer.sharedMaterial.SetFloat("_Alpha", 0);
				if(fadeInCallBack != null) fadeInCallBack();
			}
		}

		if(fadeOut)
		{
			counter += Time.deltaTime * speed;
			fadeRenderer.sharedMaterial.SetFloat("_Alpha", counter);
			if(counter >= 1.0f)
			{
				fadeOut = false;
				fadeRenderer.sharedMaterial.SetFloat("_Alpha", 1);
				if(fadeInBlackCallBack != null) fadeInBlackCallBack();
			}
		}
	}
	public void fadeInBlackEffect(Action callBack = null)
	{
		counter = 0;
		fadeOut = true;
		fadeInBlackCallBack = callBack;
	}

	public void fadeInEffect(Action callBack = null)
	{
		counter = 1;
		fadeIn = true;
		fadeInCallBack = callBack;
	}
}
