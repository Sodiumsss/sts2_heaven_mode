using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace HeavenMode;

[HarmonyPatch(typeof(NTopBarPortraitTip))]
internal static class Patches_TopBar
{
    private static readonly AccessTools.FieldRef<NTopBarPortraitTip, IHoverTip> HoverTipRef =
        AccessTools.FieldRefAccess<NTopBarPortraitTip, IHoverTip>("_hoverTip");

    private static readonly FieldInfo? HoverTipTitleField =
        typeof(HoverTip).GetField("<Title>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo? HoverTipDescriptionField =
        typeof(HoverTip).GetField("<Description>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NTopBarPortraitTip.Initialize))]
    private static void AfterInitialize(NTopBarPortraitTip __instance, IRunState runState)
    {
        try
        {
            if (HeavenState.SelectedOption < 1)
                return;

            if (HoverTipRef(__instance) is not HoverTip hoverTip)
                return;

            string portraitSuffix = GetPortraitSuffix(HeavenState.SelectedOption);
            if (string.IsNullOrWhiteSpace(portraitSuffix))
                return;

            var localPlayer = LocalContext.GetMe(runState);
            if (localPlayer?.Character == null)
                return;

            string characterTitle = localPlayer.Character.Title.GetFormattedText();
            string heavenDescription = AppendHeavenEntries(hoverTip.Description, GetActiveHeavenTitles());

            object boxed = hoverTip;
            HoverTipTitleField?.SetValue(boxed, $"{characterTitle} - {portraitSuffix}");
            HoverTipDescriptionField?.SetValue(boxed, heavenDescription);
            HoverTipRef(__instance) = (IHoverTip)(HoverTip)boxed;
            Log.Info($"[HeavenMode] Updated portrait hover tip for Heaven={HeavenState.SelectedOption}");
        }
        catch (Exception ex)
        {
            Log.Error($"[HeavenMode] AfterInitialize portrait tip failed: {ex}");
        }
    }

    private static string GetPortraitSuffix(int level) => level switch
    {
        1 => Loc.Get("HEAVEN_RUN_TITLE_1", "Heaven 1"),
        2 => Loc.Get("HEAVEN_RUN_TITLE_2", "Heaven 2"),
        _ => string.Empty,
    };

    private static IReadOnlyList<string> GetActiveHeavenTitles()
    {
        List<string> titles = new();
        if (HeavenState.SelectedOption >= 1)
            titles.Add(Loc.Get("HEAVEN_TITLE_1", "Human World"));
        if (HeavenState.SelectedOption >= 2)
            titles.Add(Loc.Get("HEAVEN_TITLE_2", "Hell of Tongue Pulling"));
        return titles;
    }

    private static string AppendHeavenEntries(string baseDescription, IReadOnlyList<string> heavenTitles)
    {
        string result = baseDescription ?? string.Empty;
        foreach (string heavenTitle in heavenTitles.Where(title => !string.IsNullOrWhiteSpace(title)))
        {
            string line = $" +{heavenTitle}";
            if (!result.Contains(line, StringComparison.Ordinal))
                result = string.IsNullOrEmpty(result) ? line : $"{result}\n{line}";
        }

        return result;
    }
}
