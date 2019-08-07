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
}