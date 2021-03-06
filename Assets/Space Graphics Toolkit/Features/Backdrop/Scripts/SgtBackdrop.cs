using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

namespace SpaceGraphicsToolkit
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtBackdrop))]
	public class SgtBackdrop_Editor : SgtQuads_Editor<SgtBackdrop>
	{
		protected override void OnInspector()
		{
			var updateMaterial        = false;
			var updateMeshesAndModels = false;

			DrawMaterial(ref updateMaterial);

			Separator();

			DrawMainTex(ref updateMaterial, ref updateMeshesAndModels);
			DrawLayout(ref updateMaterial, ref updateMeshesAndModels);

			Separator();

			DrawDefault("Seed", ref updateMeshesAndModels, "This allows you to set the random seed used during procedural generation.");
			BeginError(Any(t => t.Radius <= 0.0f));
				DrawDefault("Radius", ref updateMaterial, ref updateMeshesAndModels, "The radius of the starfield.");
			EndError();
			DrawDefault("Squash", ref updateMeshesAndModels, "Should more stars be placed near the horizon?");

			Separator();

			BeginError(Any(t => t.StarCount < 0));
				DrawDefault("StarCount", ref updateMeshesAndModels, "The amount of stars that will be generated in the starfield.");
			EndError();
			DrawDefault("starColors", ref updateMeshesAndModels, "Each star is given a random color from this gradient.");
			BeginError(Any(t => t.StarRadiusMin < 0.0f || t.StarRadiusMin > t.StarRadiusMax));
				DrawDefault("StarRadiusMin", ref updateMeshesAndModels, "The minimum radius of stars in the starfield.");
			EndError();
			BeginError(Any(t => t.StarRadiusMax < 0.0f || t.StarRadiusMin > t.StarRadiusMax));
				DrawDefault("StarRadiusMax", ref updateMeshesAndModels, "The maximum radius of stars in the starfield.");
			EndError();
			DrawDefault("StarRadiusBias", ref updateMeshesAndModels, "How likely the size picking will pick smaller stars over larger ones (1 = default/linear).");

			Separator();

			DrawDefault("PowerRgb", ref updateMaterial, "Instead of just tinting the stars with the colors, should the RGB values be raised to the power of the color?");
			DrawDefault("ClampSize", ref updateMaterial, ref updateMeshesAndModels, "Prevent the quads from being too small on screen?");
			if (Any(t => t.ClampSize == true))
			{
				BeginIndent();
					DrawDefault("ClampSizeMin", ref updateMaterial, "The minimum size each star can be on screen in pixels. If the star goes below this size, it loses opacity proportional to the amount it would have gone under.");
				EndIndent();
			}

			if (updateMaterial        == true) DirtyEach(t => t.UpdateMaterial       ());
			if (updateMeshesAndModels == true) DirtyEach(t => t.UpdateMeshesAndModels());
		}
	}
}
#endif

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to generate procedurally placed quads on the edge of a sphere.
	/// The quads can then be textured using clouds or stars, and will follow the rendering camera, creating a backdrop.
	/// This backdrop is very quick to render, and provides a good alternative to skyboxes because of the vastly reduced memory requirements.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtBackdrop")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Backdrop")]
	public class SgtBackdrop : SgtQuads
	{
		/// <summary>This allows you to set the random seed used during procedural generation.</summary>
		[FormerlySerializedAs("seed")] [SgtSeed] public int Seed; public void SetSeed(int value) { Seed = value; UpdateMeshesAndModels(); }

		/// <summary>The radius of the starfield.</summary>
		[FormerlySerializedAs("radius")] public float Radius = 1.0f; public void SetRadius(float value) { Radius = value; UpdateMeshesAndModels(); }

		/// <summary>Should more stars be placed near the horizon?</summary>
		[FormerlySerializedAs("squash")] [Range(0.0f, 1.0f)] public float Squash; public void SetSquash(float value) { Squash = value; UpdateMeshesAndModels(); }

		/// <summary>The amount of stars that will be generated in the starfield.</summary>
		[FormerlySerializedAs("starCount")] public int StarCount = 1000; public void SetStarCount(int value) { StarCount = value; UpdateMeshesAndModels(); } public void SetStarCount(float value) { SetStarCount((int)value); }

		/// <summary>Each star is given a random color from this gradient.</summary>
		[FormerlySerializedAs("StarColors")] [SerializeField] private Gradient starColors; public Gradient StarColors { get { if (starColors == null) starColors = new Gradient(); return starColors; } }

		/// <summary>The minimum radius of stars in the starfield.</summary>
		[FormerlySerializedAs("starRadiusMin")] public float StarRadiusMin = 0.01f; public void SetStarRadiusMin(float value) { StarRadiusMin = value; UpdateMeshesAndModels(); }

		/// <summary>The maximum radius of stars in the starfield.</summary>
		[FormerlySerializedAs("starRadiusMax")] public float StarRadiusMax = 0.05f; public void SetStarRadiusMax(float value) { StarRadiusMax = value; UpdateMeshesAndModels(); }

		/// <summary>How likely the size picking will pick smaller stars over larger ones (1 = default/linear).</summary>
		[FormerlySerializedAs("starRadiusBias")] public float StarRadiusBias; public void SetStarRadiusBias(float value) { StarRadiusBias = value; UpdateMeshesAndModels(); }

		/// <summary>Instead of just tinting the stars with the colors, should the RGB values be raised to the power of the color?</summary>
		[FormerlySerializedAs("powerRgb")] public bool PowerRgb; public void SetPowerRgb(bool value) { PowerRgb = value; }

		/// <summary>Prevent the quads from being too small on screen?</summary>
		[FormerlySerializedAs("clampSize")] public bool ClampSize; public void SetClampSize(bool value) { ClampSize = value; }

		/// <summary>The minimum size each star can be on screen in pixels. If the star goes below this size, it loses opacity proportional to the amount it would have gone under.</summary>
		[FormerlySerializedAs("clampSizeMin")] public float ClampSizeMin = 10.0f; public void SetClampSizeMin(float value) { ClampSizeMin = value; }

		protected override string ShaderName
		{
			get
			{
				return SgtHelper.ShaderNamePrefix + "Backdrop";
			}
		}

		public static SgtBackdrop Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		public static SgtBackdrop Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			var gameObject = SgtHelper.CreateGameObject("Backdrop", layer, parent, localPosition, localRotation, localScale);
			var backdrop   = gameObject.AddComponent<SgtBackdrop>();

			backdrop.RenderQueue.Offset = -1;

			return backdrop;
		}

#if UNITY_EDITOR
		[MenuItem(SgtHelper.GameObjectMenuPrefix + "Backdrop", false, 10)]
		private static void CreateMenuItem()
		{
			var parent   = SgtHelper.GetSelectedParent();
			var backdrop = Create(parent != null ? parent.gameObject.layer : 0, parent);

			SgtHelper.SelectAndPing(backdrop);
		}
#endif

		protected override void OnEnable()
		{
			base.OnEnable();

			SgtCamera.OnCameraPreCull   += CameraPreCull;
			SgtCamera.OnCameraPreRender += CameraPreRender;
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			SgtCamera.OnCameraPreCull   -= CameraPreCull;
			SgtCamera.OnCameraPreRender -= CameraPreRender;
		}

		protected override void BuildMaterial()
		{
			base.BuildMaterial();

			if (BlendMode == BlendModeType.Default)
			{
				BuildAdditive();
			}

			if (PowerRgb == true)
			{
				SgtHelper.EnableKeyword("SGT_B", material); // PowerRgb
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_B", material); // PowerRgb
			}

			if (ClampSize == true)
			{
				SgtHelper.EnableKeyword("SGT_C", material); // Clamp Size

				material.SetFloat(SgtShader._ClampSizeMin, ClampSizeMin * Radius);
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_C", material); // Clamp Size
			}
		}

		protected override int BeginQuads()
		{
			SgtHelper.BeginRandomSeed(Seed);

			if (starColors == null)
			{
				starColors = SgtHelper.CreateGradient(Color.white);
			}
		
			return StarCount;
		}

		protected virtual void NextQuad(ref SgtBackdropQuad star, int starIndex)
		{
			var position = Random.insideUnitSphere;

			position.y *= 1.0f - Squash;

			star.Variant  = Random.Range(int.MinValue, int.MaxValue);
			star.Color    = StarColors.Evaluate(Random.value);
			star.Radius   = Mathf.Lerp(StarRadiusMin, StarRadiusMax, SgtHelper.Sharpness(Random.value, StarRadiusBias));
			star.Angle    = Random.Range(-180.0f, 180.0f);
			star.Position = position.normalized * Radius;
		}

		protected override void EndQuads()
		{
			SgtHelper.EndRandomSeed();
		}

		private static List<Vector3> positions = new List<Vector3>();
		private static List<Color32> colors32  = new List<Color32>();
		private static List<Vector2> coords1   = new List<Vector2>();
		private static List<Vector3> coords2   = new List<Vector3>();
		private static List<int>     indices   = new List<int>();

		protected override void BuildMesh(Mesh mesh, int starIndex, int starCount)
		{
			var minMaxSet = false;
			var min       = default(Vector3);
			var max       = default(Vector3);

			SgtHelper.Resize(positions, starCount * 4);
			SgtHelper.Resize(colors32, starCount * 4);
			SgtHelper.Resize(coords1, starCount * 4);
			SgtHelper.Resize(coords2, starCount * 4);
			SgtHelper.Resize(indices, starCount * 6);

			for (var i = 0; i < starCount; i++)
			{
				NextQuad(ref SgtBackdropQuad.Temp, starIndex + i);

				var offV     = i * 4;
				var offI     = i * 6;
				var radius   = SgtBackdropQuad.Temp.Radius;
				var uv       = tempCoords[SgtHelper.Mod(SgtBackdropQuad.Temp.Variant, tempCoords.Count)];
				var rotation = Quaternion.FromToRotation(Vector3.back, SgtBackdropQuad.Temp.Position) * Quaternion.Euler(0.0f, 0.0f, SgtBackdropQuad.Temp.Angle);
				var up       = rotation * Vector3.up    * radius;
				var right    = rotation * Vector3.right * radius;

				ExpandBounds(ref minMaxSet, ref min, ref max, SgtBackdropQuad.Temp.Position, radius);

				positions[offV + 0] = SgtBackdropQuad.Temp.Position - up - right;
				positions[offV + 1] = SgtBackdropQuad.Temp.Position - up + right;
				positions[offV + 2] = SgtBackdropQuad.Temp.Position + up - right;
				positions[offV + 3] = SgtBackdropQuad.Temp.Position + up + right;

				colors32[offV + 0] =
				colors32[offV + 1] =
				colors32[offV + 2] =
				colors32[offV + 3] = SgtBackdropQuad.Temp.Color;

				coords1[offV + 0] = new Vector2(uv.x, uv.y);
				coords1[offV + 1] = new Vector2(uv.z, uv.y);
				coords1[offV + 2] = new Vector2(uv.x, uv.w);
				coords1[offV + 3] = new Vector2(uv.z, uv.w);

				coords2[offV + 0] =
				coords2[offV + 1] =
				coords2[offV + 2] =
				coords2[offV + 3] = SgtBackdropQuad.Temp.Position;

				indices[offI + 0] = offV + 0;
				indices[offI + 1] = offV + 1;
				indices[offI + 2] = offV + 2;
				indices[offI + 3] = offV + 3;
				indices[offI + 4] = offV + 2;
				indices[offI + 5] = offV + 1;
			}

			mesh.SetVertices(positions);
			mesh.SetColors(colors32);
			mesh.SetUVs(0, coords1);
			mesh.SetUVs(1, coords2);
			mesh.SetTriangles(indices, 0, false);
			mesh.bounds = SgtHelper.NewBoundsFromMinMax(min, max);
		}

		protected virtual void CameraPreCull(Camera camera)
		{
			if (models != null)
			{
				for (var i = models.Count - 1; i >= 0; i--)
				{
					var model = models[i];

					if (model != null)
					{
						model.transform.position = camera.transform.position;

						model.Save(camera);
					}
				}
			}
		}

		protected void CameraPreRender(Camera camera)
		{
			if (material != null)
			{
				if (camera.orthographic == true)
				{
					material.SetFloat(SgtShader._ClampSizeScale, camera.orthographicSize * 0.0025f);
				}
				else
				{
					material.SetFloat(SgtShader._ClampSizeScale, Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * 2.0f);
				}
			}

			if (models != null)
			{
				for (var i = models.Count - 1; i >= 0; i--)
				{
					var model = models[i];

					if (model != null)
					{
						model.Restore(camera);
					}
				}
			}
		}
	}
}