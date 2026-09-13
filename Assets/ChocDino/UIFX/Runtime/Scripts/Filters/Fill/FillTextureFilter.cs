//--------------------------------------------------------------------------//
// Copyright 2023-2026 Chocolate Dinosaur Ltd. All rights reserved.         //
// For full documentation visit https://www.chocolatedinosaur.com           //
//--------------------------------------------------------------------------//

using UnityEngine;

namespace ChocDino.UIFX
{
	public enum FillTextureWrapMode
	{
		Default,
		Clamp,
		Repeat,
		Mirror,
	}

	public enum FillSpace
	{
		Geometry,
		Screen,
	}

	/// <summary>
	/// A visual filter that fills a uGUI component using a texture.
	/// </summary>
	[AddComponentMenu("UI/Chocolate Dinosaur UIFX/Filters/UIFX - Fill Texture Filter")]
	public class FillTextureFilter : FilterBase
	{
		[Tooltip("The texture to fill with.")]
		[SerializeField] Texture _texture = null;

		[Tooltip("The scale mode to use.")]
		[SerializeField] bool _relativeToTexture = true;

		[Tooltip("The default scale mode to use for a new texture.")]
		[SerializeField] ScaleMode _textureScaleMode = ScaleMode.ScaleToFit;

		[SerializeField] FillTextureWrapMode  _textureWrapMode = FillTextureWrapMode.Default;

		[SerializeField] Color _color = Color.white;

		[Tooltip("")]
		[SerializeField] FillSpace _fillSpace = FillSpace.Geometry;

		[Range(0f, 32f)]
		[Tooltip("The ammount to scale the texture by.")]
		[SerializeField] float _textureScale = 1f;

		[Range(0f, 360f)]
		[Tooltip("The amount to rotate the texture by in degrees.")]
		[SerializeField] float _textureRotation = 0f;

		[Tooltip("The point in the rectangle where scale and rotation is centered.")]
		[SerializeField] TextAnchor _pivot = TextAnchor.MiddleCenter;

		[Tooltip("The ammount to offset/translate the texture by.")]
		[SerializeField] Vector2 _textureOffset = Vector2.zero;

		[Tooltip("The time delta to use when updating scrolling.")]
		[SerializeField] TimeDeltaMode _scrollDeltaTime = TimeDeltaMode.Normal;

		[Tooltip("The speed to scroll the gradient. XY is 2D offset and Z is 2D rotation.")]
		[SerializeField] Vector3 _scrollSpeed = Vector2.zero;

		[Tooltip("How to composite the fill with the source graphic.")]
		[SerializeField] FillGradientBlendMode _blendMode = FillGradientBlendMode.Blend;

		[Tooltip("Whether to force blend operations in happen gamma-space to match the default in software such as PhotoShop. Otherwise blend operations happen in the active color-space.")]
		[SerializeField] bool _blendModeForceGammaSpace = false;

		[Tooltip("The transparency of the source content. Set to zero to make only the fill show.")]
		[Range(0f, 1f)]
		[SerializeField] float _sourceAlpha = 1.0f;

		/// <summary>The texture to fill with.</summary>
		public Texture Texture { get { return _texture; } set { ChangePropertyRef(ref _texture, value); } }

		/// <summary>The default scale mode to use for a new texture.</summary>
		public bool RelativeToTexture { get { return _relativeToTexture; } set { ChangeProperty(ref _relativeToTexture, value); } }

		/// <summary>The default scale mode to use for a new texture.</summary>
		public ScaleMode ScaleMode { get { return _textureScaleMode; } set { ChangeProperty(ref _textureScaleMode, value); } }

		/// <summary></summary>
		public FillTextureWrapMode WrapMode { get { return _textureWrapMode; } set { ChangeProperty(ref _textureWrapMode, value); } }

		public Color Color { get { return _color; } set { ChangeProperty(ref _color, value); } }

		/// <summary></summary>
		public FillSpace FillSpace { get { return _fillSpace; } set { ChangeProperty(ref _fillSpace, value); } }

		/// <summary>The point in the rectangle where scale and rotation is centered.</summary>
		public TextAnchor Pivot { get { return _pivot; } set { ChangeProperty(ref _pivot, value); } }

		/// <summary>The amount to scale the texture by.</summary>
		public float Scale { get { return _textureScale; } set { ChangeProperty(ref _textureScale, value); } }

		/// <summary>The amount to rotate the texture by in degrees.</summary>
		public float Rotation { get { return _textureRotation; } set { ChangeProperty(ref _textureRotation, value); } }

		/// <summary>The amount to offset/translate the texture by.</summary>
		public Vector2 Offset { get { return _textureOffset; } set { ChangeProperty(ref _textureOffset, value); } }

		/// <summary>The time delta to use when updating scrolling.</summary>
		public TimeDeltaMode ScrollDeltaMode { get { return _scrollDeltaTime; } set { ChangeProperty(ref _scrollDeltaTime, value); } }

		/// <summary>The speed to scroll the gradient. XY is 2D offset and Z is 2D rotation.</summary>
		public Vector3 ScrollSpeed { get { return _scrollSpeed; } set { ChangeProperty(ref _scrollSpeed, value); } }

		/// <summary>How to composite the fill with the source graphic.</summary>
		public FillGradientBlendMode BlendMode { get { return _blendMode; } set { ChangeProperty(ref _blendMode, value); } }

		/// <summary>Whether to force blend operations in happen gamma-space to match the default in software such as PhotoShop. Otherwise blend operations happen in the active color-space.</summary>
		public bool BlendModeForceGammaSpace { get { return _blendModeForceGammaSpace; } set { ChangeProperty(ref _blendModeForceGammaSpace, value); } }
		
		/// <summary>The transparency of the source content. Set to zero to make only the fill show. Range is [0..1] Default is 1.0</summary>
		public float SourceAlpha { get { return _sourceAlpha; } set { ChangeProperty(ref _sourceAlpha, Mathf.Clamp01(value)); } }

		internal bool IsPreviewScroll { get; set; }

		private Vector3 _scroll = Vector3.zero;
		private Vector3 _scrollRelativeSize = Vector3.zero;

		static new class ShaderProp
		{
			public readonly static int Color = Shader.PropertyToID("_Color");
			public readonly static int FillTex = Shader.PropertyToID("_FillTex");
			public readonly static int FillTexMatrix = Shader.PropertyToID("_FillTex_Matrix");
			public readonly static int SourceAlpha = Shader.PropertyToID("_SourceAlpha");
			public readonly static int BlendMode = Shader.PropertyToID("_BlendMode");
		}
		static class ShaderKeyword
		{
			public const string WrapClamp = "WRAP_CLAMP";
			public const string WrapRepeat = "WRAP_REPEAT";
			public const string WrapMirror = "WRAP_MIRROR";
			public const string BlendAlphaBlend = "BLEND_ALPHABLEND";
			public const string BlendForceGammaSpace = "BLEND_FORCEGAMMASPACE";
		}

		private const string BlendShaderPath = "Hidden/ChocDino/UIFX/Blend-Fill-Texture";

		protected override string GetDisplayShaderPath()
		{
			return BlendShaderPath;
		}

		protected override bool DoParametersModifySource()
		{
			if (!base.DoParametersModifySource())
			{
				return false;
			}

			if (_texture == null) return false;
			//if (_color.a <= 0f && (_blendMode == FillGradientBlendMode.AlphaBlend || _blendMode == FillGradientBlendMode.Lighten)) return false;

			return true;
		}

		internal bool HasScrollSpeed()
		{
			return _scrollSpeed != Vector3.zero;
		}

		public void ResetScroll()
		{
			if (_scroll != Vector3.zero)
			{
				_scroll = Vector3.zero;
				ForceUpdate();
			}
		}

		protected override void OnEnable()
		{
			_expand = FilterExpand.None;
			_scrollRelativeSize = Vector3.zero;
			ResetScroll();
			base.OnEnable();
		}

		private bool IsScrolling()
		{
			if (HasScrollSpeed()
				#if UNITY_EDITOR
				&& (Application.isPlaying || IsPreviewScroll)
				#endif
			)
			{
				return true;
			}
			return false;
		}

		protected override void Update()
		{
			if (IsScrolling())
			{
				float deltaTime = 0f;
				if (_scrollDeltaTime == TimeDeltaMode.Normal)
				{
					deltaTime = Time.deltaTime;
				}
				else
				{
					if (Time.captureDeltaTime == 0f)
					{
						deltaTime = Time.unscaledDeltaTime;
					}
					else
					{
						deltaTime = Time.captureDeltaTime;
					}
				}
				_scroll += Vector3.Scale(_scrollSpeed, _scrollRelativeSize) * deltaTime;
				ForceUpdate();
			}
			base.Update();
		}

		void CalculateRelativeSpeed()
		{
			Rect textureAspectAdjust = Rect.zero;
			{
				Rect geometryRect = _screenRect.GetRect();
				// NOTE: Added the following line as it fixes the juddering when moving/scaling across subpixels.
				geometryRect = new Rect(Mathf.FloorToInt(geometryRect.xMin), Mathf.FloorToInt(geometryRect.yMin), Mathf.CeilToInt(geometryRect.xMax) - Mathf.FloorToInt(geometryRect.xMin), Mathf.CeilToInt(geometryRect.yMax) - Mathf.FloorToInt(geometryRect.yMin));
				if (_fillSpace == FillSpace.Screen && _renderSpace == FilterRenderSpace.Screen)
				{
					geometryRect = _canvas.pixelRect;
				}

				textureAspectAdjust = Rect.MinMaxRect(0f, 0f, geometryRect.width, geometryRect.height);
			}

			Vector3 scrollAdjust = Vector3.zero;
			if (textureAspectAdjust.width != 0 && textureAspectAdjust.height != 0)
			{
				scrollAdjust = new Vector3(1f / textureAspectAdjust.width, 1f / textureAspectAdjust.height, 1f);
			}

			_scrollRelativeSize = scrollAdjust;
			if (_renderSpace == FilterRenderSpace.Screen)
			{
				_scrollRelativeSize *= ResolutionScalingFactor;
			}
		}

		protected override void SetupDisplayMaterial(Texture source, Texture result)
		{
			CalculateRelativeSpeed();

			// Calculate scale and offset values for our texture to fit it within the geometry rectangle with various layout controls
			Rect textureAspectAdjust = Rect.zero;
			if (_texture)
			{
				Rect geometryRect = _screenRect.GetRect();
				// NOTE: Added the following line as it fixes the juddering when moving/scaling across subpixels.
				geometryRect = new Rect(Mathf.FloorToInt(geometryRect.xMin), Mathf.FloorToInt(geometryRect.yMin), Mathf.CeilToInt(geometryRect.xMax) - Mathf.FloorToInt(geometryRect.xMin), Mathf.CeilToInt(geometryRect.yMax) - Mathf.FloorToInt(geometryRect.yMin));
				if (_fillSpace == FillSpace.Screen && _renderSpace == FilterRenderSpace.Screen)
				{
					geometryRect = _canvas.pixelRect;
				}

				float scale = Mathf.Max(0.001f, Mathf.Abs(_textureScale)) * Mathf.Sign(_textureScale);
				if (_relativeToTexture)
				{
					if (_renderSpace == FilterRenderSpace.Screen)
					{
						if (ResolutionScalingFactor != 0f)
						{
							scale /= ResolutionScalingFactor;
						}
						else
						{
							scale = 0f;
						}
					}
					textureAspectAdjust = Rect.MinMaxRect(0f, 0f, (geometryRect.width / _texture.width) * scale, (geometryRect.height / _texture.height) * scale);
				}
				else
				{
					float textureAspect = (float)_texture.width / (float)_texture.height;
					Rect aspectGeometryRect = MathUtils.ResizeRectToAspectRatio(geometryRect, _textureScaleMode, textureAspect);
					textureAspectAdjust = MathUtils.GetRelativeRect(geometryRect, aspectGeometryRect);

					textureAspectAdjust.xMin *= scale;
					textureAspectAdjust.yMin *= scale;
					textureAspectAdjust.xMax *= scale;
					textureAspectAdjust.yMax *= scale;
				}
			}

			_displayMaterial.SetTexture(ShaderProp.FillTex, _texture);

			Vector2 offset = (_textureOffset * _scrollRelativeSize) + new Vector2(_scroll.x, _scroll.y);
			Vector3 pivot = MathUtils.GetNormalizedAnchorPosition(_pivot);
			Matrix4x4 m = Matrix4x4.Translate(pivot);
			m *= Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, _textureRotation + _scroll.z));
			m *= Matrix4x4.Scale(new Vector3(textureAspectAdjust.width, textureAspectAdjust.height, 1f));
			m *= Matrix4x4.Translate(-pivot);
			m *= Matrix4x4.Translate(new Vector3(offset.x, offset.y, 0f));

			if (_fillSpace == FillSpace.Screen && _renderSpace == FilterRenderSpace.Screen)
			{
				Rect r = GetScreenRect();
				Vector4 v = new Vector4(r.x, r.y, r.width, r.height);
				m *= Matrix4x4.Translate(new Vector3(r.x, r.y, 0f));
				m *= Matrix4x4.Scale(new Vector3(r.width, r.height, 1f));
			}

			_displayMaterial.SetMatrix(ShaderProp.FillTexMatrix, m);
			_displayMaterial.SetColor(ShaderProp.Color, _color);
			_displayMaterial.SetFloat(FilterBase.ShaderProp.Strength, _strength);

			switch (_textureWrapMode)
			{
				case FillTextureWrapMode.Default:
				_displayMaterial.DisableKeyword(ShaderKeyword.WrapClamp);
				_displayMaterial.DisableKeyword(ShaderKeyword.WrapRepeat);
				_displayMaterial.DisableKeyword(ShaderKeyword.WrapMirror);
				break;
				case FillTextureWrapMode.Clamp:
				_displayMaterial.EnableKeyword(ShaderKeyword.WrapClamp);
				_displayMaterial.DisableKeyword(ShaderKeyword.WrapRepeat);
				_displayMaterial.DisableKeyword(ShaderKeyword.WrapMirror);
				break;
				case FillTextureWrapMode.Repeat:
				_displayMaterial.DisableKeyword(ShaderKeyword.WrapClamp);
				_displayMaterial.EnableKeyword(ShaderKeyword.WrapRepeat);
				_displayMaterial.DisableKeyword(ShaderKeyword.WrapMirror);
				break;
				case FillTextureWrapMode.Mirror:
				_displayMaterial.DisableKeyword(ShaderKeyword.WrapClamp);
				_displayMaterial.DisableKeyword(ShaderKeyword.WrapRepeat);
				_displayMaterial.EnableKeyword(ShaderKeyword.WrapMirror);
				break;
			}

			_displayMaterial.SetFloat(ShaderProp.SourceAlpha, _sourceAlpha);

			//_displayMaterial.DisableKeyword(ShaderKeyword.BlendAlphaBlend);
			_displayMaterial.SetInt(ShaderProp.BlendMode, (int)_blendMode);

			if (!_blendModeForceGammaSpace)
			{
				_displayMaterial.DisableKeyword(ShaderKeyword.BlendForceGammaSpace);
			}
			else
			{
				_displayMaterial.EnableKeyword(ShaderKeyword.BlendForceGammaSpace);
			}

			base.SetupDisplayMaterial(source, result);
		}
	}
}