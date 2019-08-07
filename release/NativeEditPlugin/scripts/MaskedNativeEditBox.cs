using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

/// <summary>
/// Please see <see href="https://github.com/RedMadRobot/input-mask-android/wiki"/> for more information about
/// the masks.
/// </summary>
public class MaskedNativeEditBox : NativeEditBox
{
	private enum AffinityCalculationStrategy
	{
		[UsedImplicitly] Prefix = 0,
		[UsedImplicitly] WholeString = 1,
		[UsedImplicitly] Capacity = 2,
		[UsedImplicitly] ExtractedValueCapacity = 3,
	}

	[Header("Masked Text (Android Only)")]
	[SerializeField] private string primaryMask;
	[SerializeField] private List<string> affineMasks;
	[SerializeField] private AffinityCalculationStrategy affinityStrategy;
	[SerializeField] private bool useCustomPlaceholder;
	[SerializeField] private string customPlaceholder;

	protected override void AppendExtraFieldsForCreation(JsonObject jsonObject)
	{
		jsonObject["applyMask"] = true;
		jsonObject["primaryMask"] = this.primaryMask;
		jsonObject["affineMasks"] = this.affineMasks;
		jsonObject["affinityStrategy"] = (int) this.affinityStrategy;
		jsonObject["useCustomPlaceholder"] = this.useCustomPlaceholder;
		jsonObject["customPlaceholder"] = this.customPlaceholder;
	}
}