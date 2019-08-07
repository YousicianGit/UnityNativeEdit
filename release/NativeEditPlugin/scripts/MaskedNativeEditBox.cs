using System;
using UnityEngine.UI;

/// <summary>
/// Formats the value depending on the given mask options.
/// </summary>
/// <remarks>
/// Works only on Android, iOS is not _yet_ supported.
/// </remarks>
public class MaskedNativeEditBox : NativeEditBox
{
	private const string ApplyMaskKey = "applyMask";
	private const string PrimaryMaskKey = "primaryMask";
	private const string AffineMasksKey = "affineMasks";
	private const string AffinityStrategyKey = "affinityStrategy";
	private const string UseCustomPlaceholderKey = "useCustomPlaceholder";
	private const string CustomPlaceholderKey = "customPlaceholder";

	public MaskOptions MaskOptions { get; private set; }
	public string ExtractedText { get; private set; }

	/// <summary>
	/// Event to call whenever the value changes. Will be called with the extracted value.
	/// To get formatted values, listen <see cref="InputField.onValueChanged"/> event.
	/// </summary>
	public event Action<string> OnValueChanged;

	/// <summary>
	/// Event to call when the editing has ended. Will be called with the extracted value.
	/// To get formatted values, listen <see cref="InputField.onEndEdit"/> event.
	/// </summary>
	public event Action<string> OnEndEdit;

	private bool shouldApplyMask = true;

	/// <summary>
	/// Please see <see href="https://github.com/RedMadRobot/input-mask-android/wiki"/> for more information about
	/// the masks.
	/// </summary>
	public void SetMask(MaskOptions maskOptions)
	{
		this.MaskOptions = maskOptions;
	}

	public void ApplyMask(bool to)
	{
		this.shouldApplyMask = to;
	}

	protected override void AppendExtraFieldsForCreation(JsonObject jsonObject)
	{
		jsonObject[ApplyMaskKey] = this.shouldApplyMask;
		jsonObject[PrimaryMaskKey] = this.MaskOptions.primaryMask;
		jsonObject[AffineMasksKey] = this.MaskOptions.affineMasks;
		jsonObject[AffinityStrategyKey] = (int)this.MaskOptions.affinityStrategy;
		jsonObject[UseCustomPlaceholderKey] = this.MaskOptions.useCustomPlaceholder;
		jsonObject[CustomPlaceholderKey] = this.MaskOptions.customPlaceholder;
	}

	protected override void HandlePluginMessage(JsonObject jsonMsg)
	{
		base.HandlePluginMessage(jsonMsg);

		var msg = jsonMsg.GetString("msg");
		if (msg.Equals(MSG_TEXT_CHANGE))
		{
			this.ExtractedText = jsonMsg.GetString("extractedText");
			if (this.OnValueChanged != null)
				this.OnValueChanged(this.ExtractedText);
		}
		else if (msg.Equals(MSG_TEXT_END_EDIT))
		{
			this.ExtractedText = jsonMsg.GetString("extractedText");
			if (this.OnEndEdit != null)
				this.OnEndEdit(this.ExtractedText);
		}
	}
}