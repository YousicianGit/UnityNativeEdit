/*
 * Copyright (c) 2015 Kyungmin Bang
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */


/*
 *  NativeEditBox script should be attached to Unity UI InputField object 
 * 
 *  Limitation
 * 
 * 1. Screen auto rotation is not supported.
 */


using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(InputField))]
public class NativeEditBox : PluginMsgReceiver
{
	private struct EditBoxConfig
	{
		public bool multiline;
		public Color textColor;
		public Color backColor;
		public string contentType;
		public string font;
		public float fontSize;
		public string align;
		public string placeHolder;
		public int characterLimit;
		public Color placeHolderColor;
	}

	public enum ReturnKeyType
	{
		Default,
		Next,
		Done
	}
		
	public bool	withDoneButton = true;
	public ReturnKeyType returnKeyType;

	public event Action returnPressed;

	public bool updateRectEveryFrame;
	public bool useInputFieldFont;
	public UnityEngine.Events.UnityEvent OnReturnPressed;
	public UnityEngine.Events.UnityEvent OnBeginEditing;

	private bool bNativeEditCreated = false;

	private InputField	objUnityInput;
	private Text objUnityText;
	private bool focusOnCreate;
	private bool visibleOnCreate = true;

	private const string MSG_CREATE = "CreateEdit";
	private const string MSG_REMOVE = "RemoveEdit";
	private const string MSG_SET_TEXT = "SetText";
	private const string MSG_SET_RECT = "SetRect";
	private const string MSG_SET_TEXTSIZE = "SetTextSize";
	private const string MSG_SET_FOCUS = "SetFocus";
	private const string MSG_SET_VISIBLE = "SetVisible";
	private const string MSG_TEXT_CHANGE = "TextChange";
	private const string MSG_TEXT_BEGIN_EDIT = "TextBeginEdit";
	private const string MSG_TEXT_END_EDIT = "TextEndEdit";
	// to fix bug Some keys 'back' & 'enter' are eaten by unity and never arrive at plugin
	private const string MSG_ANDROID_KEY_DOWN = "AndroidKeyDown";
	private const string MSG_RETURN_PRESSED = "ReturnPressed";
	private const string MSG_GET_TEXT = "GetText";

	public InputField InputField { get { return objUnityInput; } }
	public bool Visible { get; private set; }

	public string text
	{
		get { return objUnityInput.text; }
		set
		{
			objUnityInput.text = value;
			SetTextNative(value);
		}
	}

	public static Rect GetScreenRectFromRectTransform(RectTransform rectTransform)
	{
		Vector3[] corners = new Vector3[4];
		
		rectTransform.GetWorldCorners(corners);
		
		float xMin = float.PositiveInfinity;
		float xMax = float.NegativeInfinity;
		float yMin = float.PositiveInfinity;
		float yMax = float.NegativeInfinity;
		
		for (int i = 0; i < 4; i++)
		{
			// For Canvas mode Screen Space - Overlay there is no Camera; best solution I've found
			// is to use RectTransformUtility.WorldToScreenPoint) with a null camera.
			Vector3 screenCoord = RectTransformUtility.WorldToScreenPoint(null, corners[i]);
			
			if (screenCoord.x < xMin)
				xMin = screenCoord.x;
			if (screenCoord.x > xMax)
				xMax = screenCoord.x;
			if (screenCoord.y < yMin)
				yMin = screenCoord.y;
			if (screenCoord.y > yMax)
				yMax = screenCoord.y;
		}
		Rect result = new Rect(xMin, Screen.height - yMax, xMax - xMin, yMax - yMin);
		return result;
	}

	private EditBoxConfig mConfig;

	private void Awake()
	{
		objUnityInput = this.GetComponent<InputField>();
		if (objUnityInput == null)
		{
			Debug.LogErrorFormat("No InputField found {0} NativeEditBox Error", this.name);
			throw new MissingComponentException();
		}

		objUnityText = objUnityInput.textComponent;
	}

	// Use this for initialization
	protected override void Start()
	{
		base.Start();

		// Wait until the end of frame before initializing to ensure that Unity UI layout has been built. We used to
		// initialize at Start, but that resulted in an invalid RectTransform position and size on the InputField if it
		// was instantiated at runtime instead of being built in to the scene.
		StartCoroutine(this.InitializeOnNextFrame());
	}

	private void OnEnable()
	{
		if (bNativeEditCreated)
			this.SetVisible(true);
	}

	private void OnDisable()
	{
		if (bNativeEditCreated)
			this.SetVisible(false);
	}

	protected override void OnDestroy()
	{
		RemoveNative();

		base.OnDestroy();
	}

	private void OnApplicationFocus(bool hasFocus)
	{
		if (!bNativeEditCreated || !this.Visible)
			return;

		this.SetVisible(hasFocus);
	}

	private IEnumerator InitializeOnNextFrame()
	{
		yield return null;

		this.PrepareNativeEdit();
		#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
		this.CreateNativeEdit();
		this.SetTextNative(this.objUnityInput.text);

		objUnityInput.placeholder.gameObject.SetActive(false);
		objUnityText.enabled = false;
		objUnityInput.enabled = false;
		#endif
	}

	private void Update()
	{
#if UNITY_ANDROID && !UNITY_EDITOR
		this.UpdateForceKeyeventForAndroid();
#endif

		if (updateRectEveryFrame && this.objUnityInput != null && bNativeEditCreated)
		{
			SetRectNative(this.objUnityText.rectTransform);
		}
	}

	private void PrepareNativeEdit()
	{
		var placeHolder = objUnityInput.placeholder.GetComponent<Text>();

		if (useInputFieldFont)
			mConfig.font = objUnityText.font.fontNames.Length > 0 ? objUnityText.font.fontNames[0] : "Arial";

		mConfig.placeHolder = placeHolder.text;
		mConfig.placeHolderColor = placeHolder.color;
		mConfig.characterLimit = objUnityInput.characterLimit;

		Rect rectScreen = GetScreenRectFromRectTransform(this.objUnityText.rectTransform);
		float fHeightRatio = rectScreen.height / objUnityText.rectTransform.rect.height;
		mConfig.fontSize = ((float)objUnityText.fontSize) * fHeightRatio;

		mConfig.textColor = objUnityText.color;
		mConfig.align = objUnityText.alignment.ToString();
		mConfig.contentType = objUnityInput.contentType.ToString();
		mConfig.backColor = new Color(1.0f, 1.0f, 1.0f, 0.0f);
		mConfig.multiline = (objUnityInput.lineType == InputField.LineType.SingleLine) ? false : true;
	}

	private void updateUnityInputText(string newText)
	{		
		// Avoid firing a delayed onValueChanged event if the text was changed from Unity with the text property in this
		// class.
		if (newText == this.objUnityInput.text)
			return;
		
		this.objUnityInput.text = newText;
	}

	public override void OnPluginMsgDirect(JsonObject jsonMsg)
	{
		PluginMsgHandler.getInst().StartCoroutine(PluginsMessageRoutine(jsonMsg));
	}

	private IEnumerator PluginsMessageRoutine(JsonObject jsonMsg)
	{
		// this is to avoid a deadlock for more info when trying to get data from two separate native plugins and handling them in Unity
		yield return null;

		string msg = jsonMsg.GetString("msg");
		if (msg.Equals(MSG_TEXT_CHANGE))
		{
			string text = jsonMsg.GetString("text");
			this.updateUnityInputText(text);
		}
		else if (msg.Equals(MSG_TEXT_BEGIN_EDIT))
		{
			if (this.OnBeginEditing != null)
				this.OnBeginEditing.Invoke();
		}
		else if (msg.Equals(MSG_TEXT_END_EDIT))
		{
			string text = jsonMsg.GetString("text");
			this.updateUnityInputText(text);
		}
		else if (msg.Equals(MSG_RETURN_PRESSED))
		{
			if (returnPressed != null)
				returnPressed();
			if (OnReturnPressed != null)
				OnReturnPressed.Invoke();
		}
	}

	private bool CheckErrorJsonRet(JsonObject jsonRet)
	{
		bool bError = jsonRet.GetBool("bError");
		string strError = jsonRet.GetString("strError");
		if (bError)
		{
			PluginMsgHandler.getInst().FileLogError(string.Format("NativeEditbox error {0}", strError));
		}
		return bError;
	}

	private void CreateNativeEdit()
	{
		Rect rectScreen = GetScreenRectFromRectTransform(this.objUnityText.rectTransform);

		JsonObject jsonMsg = new JsonObject();

		jsonMsg["msg"] = MSG_CREATE;

		jsonMsg["x"] = rectScreen.x / Screen.width;
		jsonMsg["y"] = rectScreen.y / Screen.height;
		jsonMsg["width"] = rectScreen.width / Screen.width;
		jsonMsg["height"] = rectScreen.height / Screen.height;
		jsonMsg["characterLimit"] = mConfig.characterLimit;

		jsonMsg["textColor_r"] = mConfig.textColor.r;
		jsonMsg["textColor_g"] = mConfig.textColor.g;
		jsonMsg["textColor_b"] = mConfig.textColor.b;
		jsonMsg["textColor_a"] = mConfig.textColor.a;
		jsonMsg["backColor_r"] = mConfig.backColor.r;
		jsonMsg["backColor_g"] = mConfig.backColor.g;
		jsonMsg["backColor_b"] = mConfig.backColor.b;
		jsonMsg["backColor_a"] = mConfig.backColor.a;
		jsonMsg["font"] = mConfig.font;
		jsonMsg["fontSize"] = mConfig.fontSize;
		jsonMsg["contentType"] = mConfig.contentType;
		jsonMsg["align"] = mConfig.align;
		jsonMsg["withDoneButton"] = this.withDoneButton;
		jsonMsg["placeHolder"] = mConfig.placeHolder;
		jsonMsg["placeHolderColor_r"] = mConfig.placeHolderColor.r;
		jsonMsg["placeHolderColor_g"] = mConfig.placeHolderColor.g;
		jsonMsg["placeHolderColor_b"] = mConfig.placeHolderColor.b;
		jsonMsg["placeHolderColor_a"] = mConfig.placeHolderColor.a;
		jsonMsg["multiline"] = mConfig.multiline;

		switch (returnKeyType)
		{
			case ReturnKeyType.Next:
				jsonMsg["return_key_type"] = "Next";
				break;

			case ReturnKeyType.Done:
				jsonMsg["return_key_type"] = "Done";
				break;

			default:
				jsonMsg["return_key_type"] = "Default";
				break;
		}

		JsonObject jsonRet = this.SendPluginMsg(jsonMsg);
		bNativeEditCreated = !this.CheckErrorJsonRet(jsonRet);

		if (!visibleOnCreate)
			SetVisible(false);

		if (focusOnCreate)
			SetFocus(true);
	}

	private void SetTextNative(string newText)
	{
		JsonObject jsonMsg = new JsonObject();
		
		jsonMsg["msg"] = MSG_SET_TEXT;
		jsonMsg["text"] = newText ?? string.Empty;

		this.SendPluginMsg(jsonMsg);
	}

	private void RemoveNative()
	{
		JsonObject jsonMsg = new JsonObject();
		
		jsonMsg["msg"] = MSG_REMOVE;
		this.SendPluginMsg(jsonMsg);
	}

	public void SetRectNative(RectTransform rectTrans)
	{
		var rectScreen = GetScreenRectFromRectTransform(rectTrans);

		var jsonMsg = new JsonObject();
		jsonMsg["msg"] = MSG_SET_RECT;
		jsonMsg["x"] = rectScreen.x / Screen.width;
		jsonMsg["y"] = rectScreen.y / Screen.height;
		jsonMsg["width"] = rectScreen.width / Screen.width;
		jsonMsg["height"] = rectScreen.height / Screen.height;
		this.SendPluginMsg(jsonMsg);

		var fontRectHeightRatio = rectScreen.height / this.objUnityText.rectTransform.rect.height;
		var fontSize = this.objUnityText.fontSize * fontRectHeightRatio;
		if (Math.Abs(this.mConfig.fontSize - fontSize) > 0.1f)
		{
			var sizeMsg = new JsonObject();
			sizeMsg["msg"] = MSG_SET_TEXTSIZE;
			sizeMsg["fontSize"] = fontSize;
			this.SendPluginMsg(sizeMsg);
			this.mConfig.fontSize = fontSize;
		}
	}

	public void SetFocus(bool bFocus)
	{
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
		if (!bNativeEditCreated)
		{
			focusOnCreate = bFocus;
			return;
		}

		JsonObject jsonMsg = new JsonObject();
		
		jsonMsg["msg"] = MSG_SET_FOCUS;
		jsonMsg["isFocus"] = bFocus;
		
		this.SendPluginMsg(jsonMsg);
#else
		if (gameObject.activeInHierarchy)
		{
			if (bFocus)
				objUnityInput.ActivateInputField();
			else
				objUnityInput.DeactivateInputField();
		}
		else
			focusOnCreate = bFocus;
#endif
	}

	public void SetVisible(bool bVisible)
	{
		if (!bNativeEditCreated)
		{
			visibleOnCreate = bVisible;
			return;
		}

		JsonObject jsonMsg = new JsonObject();
		
		jsonMsg["msg"] = MSG_SET_VISIBLE;
		jsonMsg["isVisible"] = bVisible;
		
		this.SendPluginMsg(jsonMsg);

		this.Visible = bVisible;
	}

	#if UNITY_ANDROID && !UNITY_EDITOR
	private void ForceSendKeydown_Android(string key)
	{
		JsonObject jsonMsg = new JsonObject();
		
		jsonMsg["msg"] = MSG_ANDROID_KEY_DOWN;
		jsonMsg["key"] = key;
		this.SendPluginMsg(jsonMsg);
	}

	private void UpdateForceKeyeventForAndroid()
	{
		if (Input.anyKeyDown)
		{
			if (Input.GetKeyDown(KeyCode.Backspace))
			{
				this.ForceSendKeydown_Android("backspace");
			}
			else
			{
				foreach(char c in Input.inputString)
				{
					if (c == '\n')
					{
						this.ForceSendKeydown_Android("enter");
					}
					else
					{
						this.ForceSendKeydown_Android(Input.inputString);
					}
				}
			}
		}	
	}
#endif
}
