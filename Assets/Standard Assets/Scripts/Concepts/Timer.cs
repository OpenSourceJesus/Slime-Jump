using System;
using SlimeJump;
using UnityEngine;
using System.Collections;

[Serializable]
public class Timer
{
	public float duration;
	public float timeRemaining;
	public float TimeElapsed
	{
		get
		{
			return timeElapsed;
		}
		private set
		{
			timeElapsed = value;
		}
	}
	public bool loop;
	public delegate void OnFinished ();
	public event OnFinished onFinished;
	public bool IsRunning
	{
		get
		{
			return isRunning;
		}
		private set
		{
			isRunning = value;
		}
	}
	public bool realtime;
	public bool autoStopIfNotLooping = true;
	Coroutine timerRoutine;
	float timeElapsed;
	bool isRunning;

	public void Start ()
	{
		if (timerRoutine == null)
			timerRoutine = GameManager.Instance.StartCoroutine(TimerRoutine ());
	}

	public void Stop ()
	{
		if (timerRoutine != null)
		{
			GameManager.Instance.StopCoroutine(timerRoutine);
			timerRoutine = null;
			isRunning = false;
		}
	}

	public void Reset ()
	{
		Stop ();
		timeRemaining = duration;
		timeElapsed = 0;
	}

	IEnumerator TimerRoutine ()
	{
		isRunning = true;
		bool justEnded;
		while (true)
		{
			justEnded = false;
			if (realtime)
			{
				timeRemaining -= Time.unscaledDeltaTime;
				timeElapsed += Time.unscaledDeltaTime;
			}
			else
			{
				timeRemaining -= Time.deltaTime;
				timeElapsed += Time.deltaTime;
			}
			while (timeRemaining <= 0)
			{
				if (onFinished != null)
					onFinished ();
				if (loop)
					timeRemaining += duration;
				else if (autoStopIfNotLooping)
					Stop ();
				justEnded = true;
				yield return new WaitForEndOfFrame();
			}
			if (!justEnded)
				yield return new WaitForEndOfFrame();
		}
	}
}