using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using ExitGames.Client.Photon;
using GorillaInfoWatch.Models;
using GorillaInfoWatch.Models.Attributes;
using GorillaInfoWatch.Models.Widgets;
using GorillaNetworking;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

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

        //public static  ConfigEntry<bool> Knockback { get; private set; }
        private static ConfigEntry<bool> Wind { get; set; }

        private void Awake()
        {
            Instance = this;

            //Knockback                       = Config.Bind("Room Utils", "NoKnockback", false, "Disable knockback");
            Wind = Config.Bind("Room Utils", "DisableWind", false, "Disable wind effects");
            //KnockbackState.KnockbackEnabled = Knockback.Value;
            WindState.WindEnabled = Wind.Value;
        }

        private void Start()
        {
            harmony = new Harmony(Constants.Guid);
            harmony.PatchAll();

            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable
                    { { Constants.HashKey, Constants.Version }, });
        }

        /*public static class KnockbackState
        {
            public static bool KnockbackEnabled { get; set; }
        }*/

        public static class WindState
        {
            public static bool WindEnabled { get; set; }
        }

        [ShowOnHomeScreen(DisplayTitle = "Room Utils")]
        internal class InfoWatchPage : InfoScreen
        {
            private static ConfigEntry<bool> disableJoinTriggers = Instance.Config.Bind("Room Utils",
                    "DisableJoinTriggers", false, "Disable join room triggers");

            private static ConfigEntry<bool> disableMapTriggers = Instance.Config.Bind("Room Utils",
                    "DisableMapTriggers", false, "Disable map transition triggers");

            private static ConfigEntry<bool> disableQuitBox =
                    Instance.Config.Bind("Room Utils", "DisableQuitBox", false, "Disable quitbox trigger");

            public override string Title => $"Room Utils : {Constants.Version}";

            public override InfoContent GetContent()
            {
                LineBuilder lines = new LineBuilder();

                lines.Add("Disconnect",  new List<Widget_Base> { new Widget_PushButton(Disconnect), });
                lines.Add("Join Random", new List<Widget_Base> { new Widget_PushButton(JoinRandom), });

                lines.Skip();

                // Get current active states on game start
                bool roomTriggersActive =
                        IsTriggerActive("Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab");

                bool mapTriggersActive =
                        IsTriggerActive("Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab");

                bool quitBoxActive =
                        IsTriggerActive("Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/QuitBox");

                lines.Add("Room Triggers",
                        new List<Widget_Base>
                        {
                                new Widget_Switch(roomTriggersActive, value =>
                                                                      {
                                                                          SetTriggerState(
                                                                                  "Environment Objects/TriggerZones_Prefab/JoinRoomTriggers_Prefab",
                                                                                  value);

                                                                          SetContent();
                                                                      }),
                        });

                lines.Add("Map Triggers",
                        new List<Widget_Base>
                        {
                                new Widget_Switch(mapTriggersActive, value =>
                                                                     {
                                                                         SetTriggerState(
                                                                                 "Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab",
                                                                                 value);

                                                                         SetContent();
                                                                     }),
                        });

                lines.Add("Quitbox",
                        new List<Widget_Base>
                        {
                                new Widget_Switch(quitBoxActive, value =>
                                                                 {
                                                                     SetTriggerState(
                                                                             "Environment Objects/TriggerZones_Prefab/ZoneTransitions_Prefab/QuitBox",
                                                                             value);

                                                                     SetContent();
                                                                 }),
                        });

                bool afkKickDisabled = PhotonNetworkController.Instance != null &&
                                       PhotonNetworkController.Instance.disableAFKKick;

                bool initialState = !afkKickDisabled;

                lines.Add("AFK Kick",
                        new List<Widget_Base>
                        {
                                new Widget_Switch(initialState, value =>
                                                                {
                                                                    if (PhotonNetworkController.Instance != null)
                                                                    {
                                                                        PhotonNetworkController.Instance
                                                                               .disableAFKKick = !value;

                                                                        Debug.Log("[ROOM UTILS - IW] AFK Kick " +
                                                                            (value ? "enabled" : "disabled")    + ".");
                                                                    }

                                                                    SetContent();
                                                                }),
                        });

                lines.Skip();

                /*lines.Add("Knockback", new List<Widget_Base>
                {
                        new Widget_Switch(!Plugin.Knockback.Value, value =>
                                                                   {
                                                                       Plugin.Knockback.Value          = !value;
                                                                       KnockbackState.KnockbackEnabled = !value;
                                                                       SetContent();
                                                                   }),
                });*/

                lines.Add("Wind", new List<Widget_Base>
                {
                        new Widget_Switch(!Wind.Value, value =>
                                                       {
                                                           Wind.Value            = !value;
                                                           WindState.WindEnabled = !value;
                                                           SetContent();
                                                       }),
                });

                return lines;
            }

            // Helper method to check if trigger GameObject is active
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
                    Debug.LogWarning("[ROOM UTILS - IW] Attempted to disconnect from room when not connected.");

                SetContent();
            }

            //private void Knockback(object[] args) => KnockbackPatch.enabled = false;

            //private void NoKnockback(object[] args) => KnockbackPatch.enabled = true;

            private async void JoinRandom(object[] args)
            {
                if (NetworkSystem.Instance.InRoom)
                    await NetworkSystem.Instance.ReturnToSinglePlayer();

                else
                    Debug.Log("Not connected to a room.");

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