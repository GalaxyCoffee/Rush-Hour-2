﻿using Harmony;
using RushHour2.Core.Reporting;
using RushHour2.Patches.AI;
using RushHour2.Patches.Simulation;
using RushHour2.Patches.UI;
using System;
using System.Linq;
using System.Threading;
using RushHour2.Localisation.Language;
using RushHour2.Core.Info;
using RushHour2.Core.Settings;

namespace RushHour2.Patches.HarmonyLocal
{
    public static class Patcher
    {
        private static readonly int REPATCH_TRY_TIMES = 5;
        private static readonly TimeSpan REPATCH_WAIT_TIME = TimeSpan.FromMilliseconds(100);

        public static bool Patch(IPatchable patchable)
        {
            var patchableName = patchable?.GetType()?.Name;
            var repatchTries = 1;

            while (repatchTries <= REPATCH_TRY_TIMES)
            {
                try
                {
                    var original = patchable.BaseMethod;
                    var prefix = patchable.Prefix;
                    var postfix = patchable.Postfix;

                    try
                    {
                        if (original != null && (prefix != null || postfix != null))
                        {
                            var originalInstanceString = $"{original.Name}({string.Join(", ", original.GetParameters().Select(parameter => parameter.ParameterType.Name).ToArray())})";
                            var prefixInstanceString = prefix != null ? $"{prefix.Name}({string.Join(", ", prefix.GetParameters().Select(parameter => parameter.ParameterType.Name).ToArray())})" : "";
                            var postfixInstanceString = postfix != null ? $"{postfix.Name}({string.Join(", ", postfix.GetParameters().Select(parameter => parameter.ParameterType.Name).ToArray())})" : "";

                            LoggingWrapper.Log(LoggingWrapper.LogArea.File, LoggingWrapper.LogType.Message, $"Attempting to patch {originalInstanceString} to prefix: {prefixInstanceString} or postfix: {postfixInstanceString} (try {repatchTries})");

                            HarmonyInstanceHolder.Instance.Patch(original, new HarmonyMethod(prefix), new HarmonyMethod(postfix));

                            LoggingWrapper.Log(LoggingWrapper.LogArea.File, LoggingWrapper.LogType.Message, $"Patched {originalInstanceString}");

                            return true;
                        }
                        else
                        {
                            LoggingWrapper.Log(LoggingWrapper.LogArea.Hidden, LoggingWrapper.LogType.Error, $"Couldn't patch {patchableName} onto {original?.Name ?? "null"}!");
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingWrapper.Log(LoggingWrapper.LogArea.Hidden, LoggingWrapper.LogType.Error, $"Couldn't patch {patchableName} onto {original?.Name ?? "null"}!");
                        LoggingWrapper.Log(LoggingWrapper.LogArea.Hidden, ex);
                    }
                }
                catch (Exception ex)
                {
                    LoggingWrapper.Log(LoggingWrapper.LogArea.Hidden, LoggingWrapper.LogType.Error, $"Patchable {patchableName ?? "unknown"} is invalid!");
                    LoggingWrapper.Log(LoggingWrapper.LogArea.Hidden, ex);
                }

                ++repatchTries;

                Thread.Sleep(REPATCH_WAIT_TIME);
            }

            return false;
        }

        public static bool OptionalPatch(IPatchable patchable, ref bool featureToggle)
        {
            featureToggle = Patch(patchable) && false;

            return featureToggle;
        }

        public static bool PatchAll()
        {
            var patched = true;

            patched = patched && Patch(new ResidentAI_UpdateLocation());
            patched = patched && Patch(new TouristAI_UpdateLocation());
            patched = patched && Patch(new SimulationManager_Update());
            patched = patched && Patch(new NewUIDateTimeWrapper_Check());
            patched = patched && Patch(new NewInfoPanel_Update());
            patched = patched && Patch(new NewCommercialBuildingAI_SimulationStepActive());
            patched = patched && Patch(new NewBuildingAI_CalculateUnspawnPosition());

            PatchOptional();

            return patched;
        }

        private static void PatchOptional()
        {
            var patched = true;

            patched = patched && OptionalPatch(new NewCommonBuildingAI_GetColor(), ref FeatureToggles.LightingModificationsActive);

            if (!patched)
            {
                MessageBoxWrapper.Show(MessageBoxWrapper.MessageType.Warning, string.Format(LocalisationHolder.Translate(LocalisationHolder.CurrentLocalisation.Incompatibility_Title), Details.BaseModName), LocalisationHolder.Translate(LocalisationHolder.CurrentLocalisation.Incompatibility_Description));
            }
        }
    }
}
