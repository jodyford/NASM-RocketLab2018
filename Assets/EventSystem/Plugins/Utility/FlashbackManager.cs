using UnityEngine;

public static class FlashbackManager
{
	static MTRenderSettings savedRenderSettings;

	static bool flashbackActive = false;
	public static bool FlashbackActive
	{
		get
		{
			return flashbackActive;
		}
		set
		{
			flashbackActive = value;
		}
	}
	
	public static void SaveSceneState (string flashbackName)
	{
		savedRenderSettings = new MTRenderSettings();
		savedRenderSettings.fog = RenderSettings.fog;
		savedRenderSettings.fogColor = RenderSettings.fogColor;
		savedRenderSettings.fogDensity = RenderSettings.fogDensity;
		savedRenderSettings.ambientLight = RenderSettings.ambientLight;
		savedRenderSettings.skybox = RenderSettings.skybox;
		savedRenderSettings.haloStrength = RenderSettings.haloStrength;
		savedRenderSettings.flareStrength = RenderSettings.flareStrength;
		//savedRenderSettings.haloTexture = RenderSettings.haloTexture;
		//savedRenderSettings.spotCookie = RenderSettings.spotCookie;
		
		MTRenderSettings newSettings = (MTRenderSettings)Resources.Load("RenderSettings/" + flashbackName);
		if (newSettings == null)
		{
			
			Debug.LogWarning("No render settings saved (should be present in RenderSettings/" + flashbackName +
							 ").  These can be generated by tweaking your flashback scene's render settings to taste and dumping them via Edit->Dump Scene Render Settings");
		}
		else
		{
			RenderSettings.fog = newSettings.fog;
			RenderSettings.fogColor = newSettings.fogColor;
			RenderSettings.fogDensity = newSettings.fogDensity;
			RenderSettings.ambientLight = newSettings.ambientLight;
			RenderSettings.skybox = newSettings.skybox;
			RenderSettings.haloStrength = newSettings.haloStrength;
			RenderSettings.flareStrength = newSettings.flareStrength;
			//RenderSettings.haloTexture = newSettings.haloTexture;
			//RenderSettings.spotCookie = newSettings.spotCookie;
		}
		
		DeactivateNPCs();
		notebookManagerFinished = false;
		
		DisablePrimaryCamera();
		
		flashbackActive = true;
		Debug.Log("Scene state saved for flashback");
	}
	
	public static bool CheckFlashbackEndCondition ()
	{
		DialogPair[] dialogs = (DialogPair[])GameObject.FindObjectsOfType(typeof(DialogPair));
		bool finished = true;
		if (dialogs.Length == 0)
			finished = false;
		foreach(DialogPair pair in dialogs)
		{
			if (!pair.DialogFinished)
			{
				finished = false;
			}
		}
		
		if (finished && !NotebookManagerFinished && NotebookManager.Main == null)
		{
			#if GLOWEFFECT_MODIFIED
			{
				GameObject[] mainCameras = GameObject.FindGameObjectsWithTag("MainCamera");

				foreach (GameObject go in mainCameras)
				{
					GlowEffect obv = (GlowEffect)go.GetComponent(typeof(GlowEffect));
					
					if (obv)
					{
						obv.FinalEffect();
					}
					
					//((GlowEffect)((Camera)go.GetComponent(typeof(Camera))).GetComponent(typeof(GlowEffect))).end();
				}
			}
			#endif

            
			Debug.Log("Flashback over");
			foreach(DialogPair pair in dialogs)
			{
				pair.npc1.LOD.ForceLODDeactivate();
				pair.npc2.LOD.ForceLODDeactivate();
			}
			NotebookManager.Init();
		}
			
			
		return finished;
	}

	public static bool CompletelyFinished
	{
		get
		{
			return CheckFlashbackEndCondition() && NotebookManagerFinished;
		}
		private set 
		{
		}
	}

	static bool notebookManagerFinished = false;
	public static bool NotebookManagerFinished
	{
		get
		{
			return notebookManagerFinished;
		}
		set
		{
			notebookManagerFinished = value;
			FlashbackManager.RestoreSceneState();
		}
	}
	
	static void DisablePrimaryCamera ()
	{
		AudioListener l = (AudioListener)Camera.main.GetComponent(typeof(AudioListener));
		l.enabled = false;
		
		Camera.main.enabled = false;
	}
	
	static void EnableNonFlashbackCameras ()
	{
		GameObject[] mainCameras = GameObject.FindGameObjectsWithTag("MainCamera");
		foreach(GameObject go in mainCameras)
		{
			if (go.transform.parent == null || (go.transform.parent && go.transform.parent.name != "Flashback Root"))
			{
				((Camera)go.GetComponent(typeof(Camera))).enabled = true;
				AudioListener l = (AudioListener)go.GetComponent(typeof(AudioListener));
				l.enabled = true;
			}
		}
	}
	
	public static void RestoreSceneState ()
	{
		NotebookManager.Destroy();
		CameraController old = (CameraController)Camera.main.gameObject.GetComponent(typeof(CameraController));
		GameObject.Destroy(old);
		GameObject flashback = GameObject.Find("Flashback Root");
		GameObject.Destroy(flashback);

		EnableNonFlashbackCameras();

		flashbackActive = false;

		CameraController.BreakLock("Flashback manager");
		MovementController mc = MovementController.GetMovementController();
		mc.SetPCControl();
		
		RenderSettings.fog = savedRenderSettings.fog;
		RenderSettings.fogColor = savedRenderSettings.fogColor;
		RenderSettings.fogDensity = savedRenderSettings.fogDensity;
		RenderSettings.ambientLight = savedRenderSettings.ambientLight;
		RenderSettings.skybox = savedRenderSettings.skybox;
		RenderSettings.haloStrength = savedRenderSettings.haloStrength;
		RenderSettings.flareStrength = savedRenderSettings.flareStrength;
		//RenderSettings.haloTexture = savedRenderSettings.haloTexture;
		//RenderSettings.spotCookie = savedRenderSettings.spotCookie;

		ActivateNPCs();
		

	}
	
	public static void DeactivateNPCs()
	{
		NPCController[] npcs = (NPCController[])GameObject.FindObjectsOfType(typeof(NPCController));
		foreach (NPCController npc in npcs)
		{
			npc.gameObject.active = false;
		}
	}
	
	public static void ActivateNPCs()
	{
		NPCController[] npcs = (NPCController[])GameObject.FindObjectsOfType(typeof(NPCController));
		foreach (NPCController npc in npcs)
		{
			npc.gameObject.active = true;
		}
	}
	
}