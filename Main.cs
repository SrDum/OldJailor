using System;
using Game.Characters;
using Game.Interface;
using Game.Interface.Customization;
using SML;
using UnityEngine;
using HarmonyLib;
using Home.Shared.Enums;
using Server.Shared.Info;
using Server.Shared.Messages;
using Server.Shared.State;
using Services;
using UnityEngine.UIElements;
using UnityEngine.Events;
using Service = Services.Service;

namespace SkinStealer
{
    [Mod.SalemMod]
    public class Main
    {

        public static PlayPhase phase = PlayPhase.FIRST_DISCUSSION;
        
        public void Start()
        {
            Console.Out.Write(("[Old Jailor] has loaded!"));
        }
    }

    [HarmonyPatch(typeof(RoleCardPanel), "HandleOnMyIdentityChanged")]
    public class player
    {
        public static Role role = Service.Game.Sim.simulation.myIdentity.Data.role;
        static void Prefix(PlayerIdentityData playerIdentityData, ref RoleCardPanel __instance)
        {
            if (playerIdentityData.role == Role.JAILOR)
            { 
                __instance.specialAbilityPanel.Hide();
            }
        }
        static void Postfix(PlayerIdentityData playerIdentityData, ref RoleCardPanel __instance)
        {
            if (playerIdentityData.role == Role.JAILOR)
            {
                __instance.specialAbilityPanel.Hide();
            }
        }
    }
    
    [HarmonyPatch(typeof(TosAbilityPanelListItem),"HandlePlayPhaseChanged")]
    public class AddJailButton
    {
        public static bool canJail = true;
        public static int lastTarget = -1;
        public static int lastTargetFresh = -1;
        static void Postfix(PlayPhaseState playPhase, ref TosAbilityPanelListItem __instance)
        {
            Main.phase = playPhase.playPhase;
            if (Service.Game.Sim.info.roleCardObservation.Data.specialAbilityRemaining==0)
            {
                canJail = false;
            }

            if (Main.phase == PlayPhase.FIRST_DAY)
            {
                lastTarget = -1;
                lastTargetFresh = -1;
                canJail = true;
            }
            if (canJail&& player.role==Role.JAILOR && __instance.playerRole !=Role.JAILOR && Main.phase !=PlayPhase.NIGHT && Main.phase !=PlayPhase.NIGHT_END_CINEMATIC &&
                 Main.phase !=PlayPhase.NIGHT_WRAP_UP && Main.phase!=PlayPhase.WHO_DIED_AND_HOW&& Main.phase!=PlayPhase.POST_TRIAL_WHO_DIED_AND_HOW
                 &&__instance.characterPosition!=lastTarget&& Main.phase != PlayPhase.DAY&&Main.phase!=PlayPhase.FIRST_DAY
                 )
            {
                
                __instance.choice2Sprite.sprite = LoadEmbeddedResources.LoadSprite("OldJailor.resources.jail.png");
                __instance.choice2Text.text = "Jail";
                __instance.choice2ButtonCanvasGroup.EnableRenderingAndInteraction();
                if (__instance.characterPosition == lastTargetFresh)
                {
                    __instance.choice2Button.Select();
                }
                __instance.choice2Button.gameObject.SetActive(true);
            }

            if (Main.phase == PlayPhase.NIGHT)
            {
                lastTarget = lastTargetFresh;
            }
        }
    }

    [HarmonyPatch(typeof(TosAbilityPanelListItem), "OnClickChoice2")]
    public class Cleanup
    {
        static bool Prefix(ref TosAbilityPanelListItem __instance)
        {
            if (player.role != Role.JAILOR) return true;
                MenuChoiceMessage message = new MenuChoiceMessage();
                message.choiceType = MenuChoiceType.SpecialAbility;
                message.choiceMode = MenuChoiceMode.TargetPosition;
                if (!__instance.choice2Button.selected)
                {
                    message.choiceMode = MenuChoiceMode.Cancel;
                    AddJailButton.lastTargetFresh = -1;
                }
                AddJailButton.lastTargetFresh = __instance.characterPosition;
                message.targetIndex = __instance.characterPosition;
                Service.Game.Network.Send((GameMessage) message);
                return false;
        }
    }
    
}