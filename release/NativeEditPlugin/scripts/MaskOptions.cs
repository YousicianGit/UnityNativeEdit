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

	public string PrimaryMask { get; }
	public IReadOnlyList<string> AffineMasks { get; }
	public AffinityCalculationStrategy AffinityStrategy { get; }
	public bool UseCustomPlaceholder { get; }
	public string CustomPlaceholder { get; }
	public string ExtraCharactersForDigits { get; }

	/// <summary>
	/// Creates new mask options.
	/// </summary>
	/// <param name="primaryMask">Primary mask to use.</param>
	/// <param name="affineMasks">Affine masks to select depending on the given affinity strategy.</param>
	/// <param name="affinityStrategy">Strategy to follow while selecting an affine mask.</param>
	/// <param name="useCustomPlaceholder">Will be used to overwrite the placeholder of given mask.</param>
	/// <param name="customPlaceholder">Placeholder to show instead of the default placeholder of given mask.</param>
	/// <param name="extraCharactersForDigits">Can be used to set an extra accepted character set when
	/// the content type is numbers only.</param>
	/// <remarks>
	/// If the content type is numbers only and there's a separation character in the mask (e.g space, dash etc.)
	/// <see cref="ExtraCharactersForDigits"/> should be set to accept these characters. Otherwise an exception
	/// will be thrown.
	/// </remarks>
	public MaskOptions(
		string primaryMask,
		IReadOnlyList<string> affineMasks = null,
		AffinityCalculationStrategy affinityStrategy = AffinityCalculationStrategy.Prefix,
		bool useCustomPlaceholder = false,
		string customPlaceholder = null,
		string extraCharactersForDigits = null)
	{
		this.PrimaryMask = primaryMask;
		this.AffineMasks = affineMasks;
		this.AffinityStrategy = affinityStrategy;
		this.UseCustomPlaceholder = useCustomPlaceholder;
		this.CustomPlaceholder = customPlaceholder;
		this.ExtraCharactersForDigits = extraCharactersForDigits;
	}
}