using System.Collections.Generic;

/// <summary>
/// Please see <see href="https://github.com/RedMadRobot/input-mask-android/wiki"/> for more information about
/// the masks.
/// </summary>
public class MaskOptions
{
	/// <summary>
	/// Strategy to calculate affinity value. For more information,
	/// see <see href="https://github.com/RedMadRobot/input-mask-android/wiki"/>
	/// </summary>
	/// <remarks>
	/// Integer value of this enum will be sent to the native side where it's
	/// converted from int to related native enum value again.
	/// On Android, EditBox.java does this conversion.
	/// </remarks>
	public enum AffinityCalculationStrategy
	{
		Prefix = 0,
		WholeString = 1,
		Capacity = 2,
		ExtractedValueCapacity = 3,
	}

	public string primaryMask { get; }
	public IReadOnlyList<string> affineMasks { get; }
	public AffinityCalculationStrategy affinityStrategy { get; }
	public bool useCustomPlaceholder { get; }
	public string customPlaceholder { get; }

	public MaskOptions(
		string primaryMask,
		IReadOnlyList<string> affineMasks = null,
		AffinityCalculationStrategy affinityStrategy = AffinityCalculationStrategy.Prefix,
		bool useCustomPlaceholder = false,
		string customPlaceholder = null)
	{
		this.primaryMask = primaryMask;
		this.affineMasks = affineMasks;
		this.affinityStrategy = affinityStrategy;
		this.useCustomPlaceholder = useCustomPlaceholder;
		this.customPlaceholder = customPlaceholder;
	}
}