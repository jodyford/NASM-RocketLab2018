// Camera Path
// Available on the Unity3D Asset Store
// Copyright (c) 2013 Jasper Stocker http://camerapath.jasperstocker.com
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.

using UnityEngine;
using System.Collections;

/*
 * Animator
 * Governs how the path will be animated
*/
public class CameraPathBezierAnimator : MonoBehaviour {
	
	public enum modes{
		once,
		loop,
		reverse,
		reverseLoop,
		pingPong
	}
	
	public CameraPathBezier _bezier;
	//do you want this path to automatically animate at the start of your scene
	public bool playOnStart = true;
	//the actual transform you want to animate
	public Transform animationTarget = null;
	//is the transform you are animating a camera?
	public bool isCamera = true;
	private bool playing = false;
	public modes mode = modes.once;
	private float pingPongDirection = 1;
	public bool normalised = true;
	
	//public bool loop = false;
	
	//the time used in the editor to preview the path animation
	public float editorTime = 0;
	//the time the path animation should last for
	public float pathTime = 10;
	private float _percentage = 0;
	private float usePercentage;
	private int atPointNumber = 0;
	
	//the sensitivity of the mouse in mouselook
	public float sensitivity = 5.0f;
	//the minimum the mouse can move down
	public float minX = -90.0f;
	//the maximum the mouse can move up
	public float maxX = 90.0f;
	private float rotationX = 0;
	private float rotationY = 0;
	
	public CameraPathBezierAnimator nextAnimation = null;
	
	//PUBLIC METHODS
	
	//Script based controls - hook up your scripts to these to control your
	
	/// <summary>
	/// Gets or sets the path speed.
	/// </summary>
	/// <value>
	/// The path speed.
	/// </value>
	public float pathSpeed
	{
		get
		{
			return bezier.storedTotalArcLength/pathTime;
		}
		set
		{
			float newPathSpeed = value;
			pathTime = bezier.storedTotalArcLength/Mathf.Max(newPathSpeed,0.000001f);
		}
	}
	
	//play the animation as runtime
	public void Play()
	{
		playing = true;
		if(!isReversed)
		{
			if(_percentage==0)
				CameraPathEvent.Broadcast(CameraPathEvent.ANIMATION_STARTED);
		}else{
			if(_percentage==1)
				CameraPathEvent.Broadcast(CameraPathEvent.ANIMATION_STARTED);
		}
	}
	
	//stop and set the animation at the beginning
	public void Stop()
	{
		playing = false;
		CancelInvoke("Play");
		_percentage = 0;
		CameraPathEvent.Broadcast(CameraPathEvent.ANIMATION_STOPPED);
	}
	
	//pasue the animation where it is
	public void Pause()
	{
		playing = false;
		CancelInvoke("Play");
		CameraPathEvent.Broadcast(CameraPathEvent.ANIMATION_PAUSED);
	}
	
	//set the time of the animtion (0-1)
	public void Seek(float value)
	{
		_percentage = Mathf.Clamp01(value);
		//thanks kelnishi!
		UpdateAnimationTime();
		bool p = playing;
		playing = true;
		UpdateAnimation();
		playing = p;
	}
	
	public bool isPlaying
	{
		get{return playing;}
	}
	
	public float percentage
	{
		get{return _percentage;}
	}
	
	public bool pingPongGoingForward
	{
		get{return pingPongDirection==1;}
	}
	
	public CameraPathBezier bezier
	{
		get{
			if(!_bezier)
				_bezier = GetComponent<CameraPathBezier>();
			return _bezier;
		}
	}
	
	//normalise the curve and apply easing
	public float RecalculatePercentage(float percentage)
	{
		if(bezier.numberOfControlPoints==0)
			return percentage;
		float normalisedPercentage = bezier.GetNormalisedT(percentage);
		int numberOfCurves = bezier.numberOfCurves;
		float curveT = 1.0f/(float)numberOfCurves;
		int point = Mathf.FloorToInt(normalisedPercentage/curveT);
		float curvet = Mathf.Clamp01((normalisedPercentage - point * curveT) * numberOfCurves);
		return bezier.controlPoints[point]._curve.Evaluate(curvet)/numberOfCurves + (point*curveT);
	}
	
	//MONOBEHAVIOURS
	
	void Start()
	{
		if(isCamera && !animationTarget.GetComponent<Camera>()){
			Debug.LogWarning("Warning: Do not set animation to 'isCamera' when not using a camera");
			isCamera=false;
		}
		
		bezier.RecalculateStoredValues();
		//playing = playOnStart;
		
		if(!isReversed)
		{
			_percentage = 0;
			atPointNumber = -1;
		}else{
			_percentage = 1;
			atPointNumber = bezier.numberOfControlPoints-1;
		}
		
		Vector3 initalRotation = bezier.GetPathRotation(0).eulerAngles;
		rotationX = initalRotation.y;
		rotationY = initalRotation.x;
		
		if(playOnStart)
			Play();
	}
	
	void Update()
	{
		if(!isCamera){
			if(playing){
				UpdateAnimationTime();
				UpdateAnimation();
				UpdatePointReached();
			}else{
				if(nextAnimation!=null && _percentage >= 1)
				{
					nextAnimation.Play();
					nextAnimation=null;
				}
			}
		}
	}
	
	void LateUpdate()
	{
		if(isCamera){
			if(playing){
				UpdateAnimationTime();
				UpdateAnimation();
				UpdatePointReached();
			}else{
				if(nextAnimation!=null && _percentage >= 1)
				{
					nextAnimation.Play();
					nextAnimation=null;
				}
			}
		}
	}
	
	void OnDrawGizmos()
	{
		if(bezier==null)
			return;
		
		if(Application.isPlaying)
			return;
		
		if(bezier.numberOfControlPoints==0)
			return;
		
		Vector3 debugPosition = bezier.GetPathPosition(Mathf.Clamp01(RecalculatePercentage(editorTime)));
		
		Gizmos.color = new Color(1.0f,0.0f,1.0f);
		Gizmos.DrawLine(debugPosition,debugPosition+Vector3.up * 0.5f);
		Gizmos.DrawLine(debugPosition,debugPosition+Vector3.down * 0.5f);
		Gizmos.DrawLine(debugPosition,debugPosition+Vector3.left * 0.5f);
		Gizmos.DrawLine(debugPosition,debugPosition+Vector3.right * 0.5f);
		Gizmos.DrawLine(debugPosition,debugPosition+Vector3.forward * 0.5f);
		Gizmos.DrawLine(debugPosition,debugPosition+Vector3.back * 0.5f);	
	}
	
	//PRIVATE METHODS
	
	void UpdateAnimation()
	{
		if(animationTarget==null){
			Debug.LogError("There is no aniamtion target specified in the Camera Path Bezier Animator component. Nothing to animate.\nYou can find this component in the main camera path component.");
			return;
		}
		
		if(!playing)
			return;
		
		animationTarget.position = bezier.GetPathPosition(usePercentage);
		if(isCamera)
			animationTarget.GetComponent<Camera>().fieldOfView = bezier.GetPathFOV(usePercentage);
		
		Vector3 minusPoint, plusPoint;
		switch(bezier.mode)
		{
		case CameraPathBezier.viewmodes.usercontrolled:
			animationTarget.rotation = bezier.GetPathRotation(_percentage);
			break;
			
		case CameraPathBezier.viewmodes.target:
			
			animationTarget.LookAt(bezier.target.transform.position);
			break;
			
		case CameraPathBezier.viewmodes.followpath:
			if(!bezier.loop){
				minusPoint = bezier.GetPathPosition(Mathf.Clamp01(usePercentage-0.05f));
				plusPoint = bezier.GetPathPosition(Mathf.Clamp01(usePercentage+0.05f));
			}else{
				float minus = usePercentage-0.05f;
				if(minus<0)
					minus+=1;
				float plus = usePercentage+0.05f;
				if(plus>1)
					plus+=-1;
				minusPoint = bezier.GetPathPosition(minus);
				plusPoint = bezier.GetPathPosition(plus);
			}
			
			animationTarget.LookAt(animationTarget.position+(plusPoint-minusPoint));
			animationTarget.eulerAngles += transform.forward*-bezier.GetPathTilt(usePercentage);
			break;
			
		case CameraPathBezier.viewmodes.reverseFollowpath:
			if(!bezier.loop){
				minusPoint = bezier.GetPathPosition(Mathf.Clamp01(usePercentage-0.05f));
				plusPoint = bezier.GetPathPosition(Mathf.Clamp01(usePercentage+0.05f));
			}else{
				float minus = usePercentage-0.05f;
				if(minus<0)
					minus+=1;
				float plus = usePercentage+0.05f;
				if(plus>1)
					plus+=-1;
				minusPoint = bezier.GetPathPosition(minus);
				plusPoint = bezier.GetPathPosition(plus);
			}
			
			animationTarget.LookAt(animationTarget.position+(minusPoint-plusPoint));
			break;	
			
		case CameraPathBezier.viewmodes.mouselook:
			
			animationTarget.rotation = GetMouseLook();
			break;
		}
	}
	
	private void UpdatePointReached()
	{
		int currentPointNumber = bezier.GetPointNumber(usePercentage);
		
		if(currentPointNumber != atPointNumber)
		{
			//we've hit a point
			CameraPathBezierControlPoint atPoint;
			if(!isReversed)
				atPoint = bezier.controlPoints[currentPointNumber];
			else
				atPoint = bezier.controlPoints[atPointNumber];
			
			CameraPathEvent.Broadcast(CameraPathEvent.POINT_REACHED);
			if(!isReversed)
				CameraPathEvent<int>.Broadcast(CameraPathEvent.POINT_REACHED_WITH_NUMBER, currentPointNumber);
			else
				CameraPathEvent<int>.Broadcast(CameraPathEvent.POINT_REACHED_WITH_NUMBER, atPointNumber);
			
			switch(atPoint.delayMode)
			{
			case CameraPathBezierControlPoint.DELAY_MODES.none:
				//do nothing extra
				break;
			case CameraPathBezierControlPoint.DELAY_MODES.indefinite:
				Pause();
				break;
			case CameraPathBezierControlPoint.DELAY_MODES.timed:
				Pause();
				Invoke("Play",atPoint.delayTime);
				break;
			}
		}
		
		atPointNumber = currentPointNumber;
	}
	
	private void UpdateAnimationTime()
	{
		switch(mode){
			
		case modes.once:
			if(_percentage>=1)
			{
				playing=false;
				CameraPathEvent.Broadcast(CameraPathEvent.POINT_REACHED);
				CameraPathEvent<int>.Broadcast(CameraPathEvent.POINT_REACHED_WITH_NUMBER, bezier.numberOfControlPoints-1);
				CameraPathEvent.Broadcast(CameraPathEvent.ANIMATION_FINISHED);
			}else{
				_percentage += Time.deltaTime * (1.0f/pathTime);
			}
			break;
			
		case modes.loop:
			if(_percentage>=1)
			{
				_percentage=0;
				CameraPathEvent.Broadcast(CameraPathEvent.POINT_REACHED);
				CameraPathEvent<int>.Broadcast(CameraPathEvent.POINT_REACHED_WITH_NUMBER, bezier.numberOfControlPoints-1);
				CameraPathEvent.Broadcast(CameraPathEvent.ANIMATION_LOOPED);
			}
			_percentage += Time.deltaTime * (1.0f/pathTime);
			break;
			
		case modes.reverseLoop:
			if(_percentage<=0)
			{
				_percentage=1;
				CameraPathEvent.Broadcast(CameraPathEvent.POINT_REACHED);
				CameraPathEvent<int>.Broadcast(CameraPathEvent.POINT_REACHED_WITH_NUMBER, 0);
				CameraPathEvent.Broadcast(CameraPathEvent.ANIMATION_LOOPED);
			}
			_percentage += -Time.deltaTime * (1.0f/pathTime);
			break;
			
		case modes.reverse:
			if(_percentage<=0)
			{
				playing=false;
				CameraPathEvent.Broadcast(CameraPathEvent.POINT_REACHED);
				CameraPathEvent<int>.Broadcast(CameraPathEvent.POINT_REACHED_WITH_NUMBER, 0);
				CameraPathEvent.Broadcast(CameraPathEvent.ANIMATION_FINISHED);
			}else{
				_percentage += -Time.deltaTime * (1.0f/pathTime);
			}
			break;
			
		case modes.pingPong:
			_percentage += Time.deltaTime * (1.0f/pathTime) * pingPongDirection;
			if(_percentage>=1)
			{
				_percentage = 0.99f;
				pingPongDirection=-1;
				CameraPathEvent.Broadcast(CameraPathEvent.POINT_REACHED);
				CameraPathEvent<int>.Broadcast(CameraPathEvent.POINT_REACHED_WITH_NUMBER, bezier.numberOfControlPoints-1);
				CameraPathEvent.Broadcast(CameraPathEvent.ANIMATION_PINGPONG);
			}
			if(_percentage<=0)
			{
				_percentage = 0.01f;
				pingPongDirection=1;
				CameraPathEvent.Broadcast(CameraPathEvent.POINT_REACHED);
				CameraPathEvent<int>.Broadcast(CameraPathEvent.POINT_REACHED_WITH_NUMBER, 0);
				CameraPathEvent.Broadcast(CameraPathEvent.ANIMATION_PINGPONG);
			}
			break;
		}
		
		_percentage = Mathf.Clamp01(_percentage);
		usePercentage = normalised? RecalculatePercentage(_percentage): _percentage;//this is the percentage used by everything but the rotation
	}
	
	private Quaternion GetMouseLook()
	{
		if(animationTarget==null)
			return Quaternion.identity;
		rotationX += Input.GetAxis("Mouse X") * sensitivity;
		rotationY += -Input.GetAxis("Mouse Y") * sensitivity;
		
		rotationY = Mathf.Clamp(rotationY,minX,maxX);
		
		return Quaternion.Euler(new Vector3(rotationY, rotationX, 0));
	}
	
	private float ClampAngle(float angle, float min, float max) 
	{
	   if (angle < -360)
	      angle += 360;
	
	   if (angle > 360)
	      angle -= 360;
	
	   return Mathf.Clamp (angle, -max, -min);
	
	}
	
	private bool isReversed
	{
		get{ return (mode == modes.reverse || mode == modes.reverseLoop || pingPongDirection < 0);}
	}
}
