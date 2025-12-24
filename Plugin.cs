using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using GorillaInfoWatch.Models;
using GorillaInfoWatch.Models.Attributes;
using GorillaInfoWatch.Models.Widgets;
using GorillaNetworking;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[assembly: InfoWatchCompatible]

namespace RoomUtils
{
    [BepInPlugin(Constants.Guid, Constants.Name, Constants.Version)]
    public class Plugin : BaseUnityPlugin
    {
        private Harmony harmony;

        internal InfoWatchPage InfoWatchPageInstance;

        private static Plugin Instance { get; set; }

        private new ConfigFile Config => base.Config;

        private static ConfigEntry<bool> Wind           { get; set; }
        private static ConfigEntry<bool> DisableAFKKick { get; set; }

        private void Awake()
        {
            Instance = this;

            Wind = Config.Bind(
                    "Room Utils",
                    "DisableWind",
                    false,
                    "Disable wind effects");

            DisableAFKKick = Config.Bind(
                    "Room Utils",
                    "DisableAFKKick",
                    false,
                    "Disable AFK kick");

            WindState.WindEnabled       = !Wind.Value;
            AFKKickState.AFKKickEnabled = !DisableAFKKick.Value;
        }

        private void Start()
        {
            harmony = new Harmony(Constants.Guid);
            harmony.PatchAll();

            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable
                    { { Constants.HashKey, Constants.Version }, });

            ApplyAFKKickState();
        }

        internal static void ApplyAFKKickState()
        {
            if (PhotonNetworkController.Instance != null)
                PhotonNetworkController.Instance.disableAFKKick = !AFKKickState.AFKKickEnabled;
        }

        public static class WindState
        {
            public static bool WindEnabled { get; set; }
        }

        public static class AFKKickState
        {
            public static bool AFKKickEnabled { get; set; }
        }

        [ShowOnHomeScreen(DisplayTitle = "Room Utils")]
        internal class InfoWatchPage : InfoScreen
        {
            private static ConfigEntry<bool> disableJoinTriggers =
                    Instance.Config.Bind("Room Utils", "DisableJoinTriggers", false, "Disable join room triggers");

            private static ConfigEntry<bool> disableMapTriggers =
                    Instance.Config.Bind("Room Utils", "DisableMapTriggers", false, "Disable map transition triggers");

            private static ConfigEntry<bool> disableQuitBox =
                    Instance.Config.Bind("Room Utils", "DisableQuitBox", false, "Disable quitbox trigger");

            public override string Title => $"Room Utils : {Constants.Version}";

            public override InfoContent GetContent()
            {
                LineBuilder lines = new LineBuilder();

                lines.Add("Disconnect", new List<Widget_Base>
                {
                        new Widget_PushButton(Disconnect),
                });

                lines.Add("Join Random", new List<Widget_Base>
                {
                        new Widget_PushButton(JoinRandom),
                });

                lines.Skip();

                bool roomTriggersActive =
                        IsTriggerActive("Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab");

                bool mapTriggersActive =
                        IsTriggerActive("Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab");

                bool quitBoxActive =
                        IsTriggerActive("Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/QuitBox");

                lines.Add("Room Triggers", new List<Widget_Base>
                {
                        new Widget_Switch(roomTriggersActive, value =>
                                                              {
                                                                  SetTriggerState(
                                                                          "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab",
                                                                          value);

                                                                  SetContent();
                                                              }),
                });

                lines.Add("Map Triggers", new List<Widget_Base>
                {
                        new Widget_Switch(mapTriggersActive, value =>
                                                             {
                                                                 SetTriggerState(
                                                                         "Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab",
                                                                         value);

                                                                 SetContent();
                                                             }),
                });

                lines.Add("Quitbox", new List<Widget_Base>
                {
                        new Widget_Switch(quitBoxActive, value =>
                                                         {
                                                             SetTriggerState(
                                                                     "Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/QuitBox",
                                                                     value);

                                                             SetContent();
                                                         }),
                });

                lines.Add("AFK Kick", new List<Widget_Base>
                {
                        new Widget_Switch(
                                AFKKickState.AFKKickEnabled,
                                value =>
                                {
                                    AFKKickState.AFKKickEnabled = value;
                                    DisableAFKKick.Value        = !value;

                                    ApplyAFKKickState();

                                    Debug.Log("[ROOM UTILS - IW] AFK Kick "    +
                                              (value ? "enabled" : "disabled") + ".");

                                    SetContent();
                                }),
                });

                lines.Skip();

                lines.Add("Wind", new List<Widget_Base>
                {
                        new Widget_Switch(
                                WindState.WindEnabled,
                                value =>
                                {
                                    WindState.WindEnabled = value;
                                    Wind.Value            = !value;

                                    SetContent();
                                }),
                });

                return lines;
            }

            private bool IsTriggerActive(string objectPath)
            {
                GameObject obj = GameObject.Find(objectPath);

                return obj != null && obj.activeSelf;
            }

            private void Disconnect(object[] args)
            {
                if (NetworkSystem.Instance.InRoom)
                    PhotonNetwork.Disconnect();
                else
                    Debug.LogWarning("[ROOM UTILS - IW] Attempted to disconnect while not in room.");

                SetContent();
            }

            private async void JoinRandom(object[] args)
            {
                if (NetworkSystem.Instance.InRoom)
                    await NetworkSystem.Instance.ReturnToSinglePlayer();

                string gamemode = PhotonNetworkController.Instance.currentJoinTrigger == null
                                          ? "forest"
                                          : PhotonNetworkController.Instance.currentJoinTrigger.networkZone;

                PhotonNetworkController.Instance.AttemptToJoinPublicRoom(
                        GorillaComputer.instance.GetJoinTriggerForZone(gamemode));

                SetContent();
            }

            private void SetTriggerState(string objectPath, bool enabled)
            {
                GameObject target = GameObject.Find(objectPath);
                if (target != null)
                    target.SetActive(enabled);
            }
        }
    }
}