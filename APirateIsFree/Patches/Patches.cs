using GorillaNetworking;
using HarmonyLib;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

namespace APirateIsFree.Patches
{
    [HarmonyPatch(typeof(VRRig))]
    [HarmonyPatch("RequestCosmetics", MethodType.Normal)]
    internal class VRRigPatch
    {
        private static bool Prefix(ref VRRig __instance, ref PhotonMessageInfo info)
        {
            __instance.IncrementRPC(info, "RequestCosmetics");
            if (__instance.photonView.IsMine && CosmeticsController.hasInstance)
            {
                string[] array = CosmeticsController.instance.currentWornSet.ToDisplayNameArray();
                string[] array2 = CosmeticsController.instance.tryOnSet.ToDisplayNameArray();

                var tempList = new List<string>(array);
                var modifiedList = new List<string>(array);
                foreach (var item in tempList)
                {
                    if (AllowedPatch.badItems.Contains(item))
                    {
                        Debug.Log($"Stripping non-owned cosmetic {item} from response");

                        modifiedList[modifiedList.ToArray().IndexOfRef(item)] = "NOTHING";
                    }
                }
                var newarr = modifiedList.ToArray();
                Debug.Log(newarr.ToJson());
                __instance.photonView.RPC("UpdateCosmeticsWithTryon", info.Sender, newarr, array2);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(VRRig))]
    [HarmonyPatch("IsItemAllowed", MethodType.Normal)]
    internal class AllowedPatch
    {
        public static List<string> badItems = new List<string>();

        private static void Postfix(ref string itemName, ref VRRig __instance, ref bool __result)
        {
            if (!__instance.isOfflineVRRig) return;
            if (!__result)
            {
                badItems.Add(itemName);
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(CosmeticsController.CosmeticSet))]
    [HarmonyPatch("LoadFromPlayerPreferences", MethodType.Normal)]
    internal class UnlockPatch
    {
        private static void Postfix()
        {
            foreach (CosmeticsController.CosmeticItem item in CosmeticsController.instance.allCosmetics)
            {
                CosmeticsController.instance.UnlockItem(item.itemName);
                CosmeticsController.instance.UpdateWardrobeModelsAndButtons();
                CosmeticsController.instance.UpdateWornCosmetics();
                CosmeticsController.instance.UpdateMyCosmetics();
            }
        }
    }
}
