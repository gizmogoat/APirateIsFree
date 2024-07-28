using BepInEx;
using GorillaNetworking;
using Photon.Pun;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace APirateIsFree
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        bool unlocked;
        void Start()
        {
            HarmonyPatches.ApplyHarmonyPatches();
        }

        void Update()
        {

        }
    }
}
