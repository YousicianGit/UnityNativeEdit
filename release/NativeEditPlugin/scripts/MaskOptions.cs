using System.Collections.Generic;

/// <summary>
/// Please see <see href="https://github.com/RedMadRobot/input-mask-android/wiki"/> for more information about
/// the masks.
/// </summary>
public class MaskOptions
{
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