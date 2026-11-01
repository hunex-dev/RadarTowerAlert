
using BepInEx;
using BepInEx.Configuration;
using RoR2;
using RoR2.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using static Wamp;

namespace RadarTowerAlert
{
    [BepInPlugin(
        "com.hunex.radartoweralert",
        "RadarTowerAlert",
        "1.0.0")]
    public class RadarTowerAlertPlugin : BaseUnityPlugin
    {
        private ConfigEntry<bool> enabledConfig;
        private ConfigEntry<bool> stageScanConfig;
        private ConfigEntry<bool> debugLoggingConfig;
        private ConfigEntry<bool> hotkeyEnabledConfig;
        private ConfigEntry<KeyCode> hotkeyConfig;
        private readonly string[] alertLines =
        {
            "A carrier wave with nothing to carry.",
            "An empty channel has begun to whisper.",
            "The frequency is occupied. No one is transmitting.",
            "The frequency was empty a moment ago.",
            "The interference has a pattern. You recognize it.",
            "The signal was never meant for you.",
            "Silence, on a frequency that should not exist.",
            "The static changes when observed.",
            "The static is trying to tell you something.",
            "The transmission has no point of origin."
        };
        private string pendingMessage;

        private void Awake()
        {
            enabledConfig = Config.Bind(
                "General", "Enabled", true,
                "Enable RadarTowerAlert.");

            stageScanConfig = Config.Bind(
                "General", "ScanOnStageStart", true,
                "Automatically check for Radar Towers after stage population.");

            hotkeyEnabledConfig = Config.Bind(
                "Debug", "EnableHotkey", false,
                "Enable the manual scan hotkey.");

            hotkeyConfig = Config.Bind(
                "Debug", "Hotkey", KeyCode.F8,
                "Key to manually scan for Radar Towers.");

            debugLoggingConfig = Config.Bind(
                "Debug", "DebugLogging", false,
                "Log scan results to the BepInEx console.");

            Stage.onStageStartGlobal += OnStageStart;
            SceneDirector.onPostPopulateSceneServer += OnScenePopulated;
            On.RoR2.UI.ChatBox.OnEnable += ChatBox_OnEnable;

            Logger.LogInfo("RadarTowerAlert loaded.");
        }

        private void OnDestroy()
        {
            Stage.onStageStartGlobal -= OnStageStart;
            SceneDirector.onPostPopulateSceneServer -= OnScenePopulated;
            On.RoR2.UI.ChatBox.OnEnable -= ChatBox_OnEnable;
        }

        private void Update()
        {
            if ( !enabledConfig.Value || !hotkeyEnabledConfig.Value )
                return;

            if ( Input.GetKeyDown(hotkeyConfig.Value) )
                ScanForRadarTowers(true);
        }

        private void OnStageStart(Stage stage)
        {
            if ( debugLoggingConfig.Value )
            {
                Logger.LogInfo("Stage started: " + stage.sceneDef.baseSceneName);
            }
        }

        private void OnScenePopulated(SceneDirector director)
        {
            if ( !enabledConfig.Value || !stageScanConfig.Value )
                return;

            ScanForRadarTowers(false);
        }

        private void ChatBox_OnEnable(On.RoR2.UI.ChatBox.orig_OnEnable orig, RoR2.UI.ChatBox self)
        {
            orig(self);

            if (!string.IsNullOrEmpty(pendingMessage))
            {
                Chat.AddMessage(pendingMessage);
                pendingMessage = null;
            }
        }

        private void ScanForRadarTowers(bool manual)
        {
            if ( !enabledConfig.Value )
                return;

            int matches = 0;

            GameObject[] objects =
                Resources.FindObjectsOfTypeAll<GameObject>();

            foreach ( GameObject obj in objects )
            {
                if ( (obj == null) ||
                     !obj.scene.IsValid() ||
                     !obj.activeInHierarchy )
                    continue;

                string objectName = obj.name;

                if ( (objectName.IndexOf(
                        "RadarTower",
                        StringComparison.OrdinalIgnoreCase) < 0) &&
                     (objectName.IndexOf(
                        "Radar Tower",
                        StringComparison.OrdinalIgnoreCase) < 0) )
                    continue;

                matches++;

                if ( debugLoggingConfig.Value )
                {
                    Logger.LogInfo(
                        "Radar candidate: " + objectName +
                        " (scene: " + obj.scene.name + ")");
                }
            }

            if ( debugLoggingConfig.Value )
            {
                Logger.LogInfo(
                    "Scan complete. Matches: " + matches +
                    "; Manual: " + manual);
            }

            AlertInChat(manual, (matches != 0));
        }

        private void AlertInChat(bool manual, bool found)
        {
            string baseLine = "[RadarTowerAlert] ";
            string line = null;

            if ( found )
            {
                line = alertLines[UnityEngine.Random.Range(0, alertLines.Length)];
            }
            else if ( manual )
            {
                line = "No carrier detected. The silence is deliberate.";
            }

            if ( !string.IsNullOrEmpty(line) )
            {
                if ( manual )
                {
                    Chat.AddMessage(baseLine + line);
                }
                else
                {
                    pendingMessage = baseLine + line;
                }
            }
        }
    }
}
