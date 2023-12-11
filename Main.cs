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
        public static PlayPhase p = PlayPhase.NONE;
        
        public void Start()
        {
            Console.Out.Write(("[Old Jailor] has loaded!"));
        }
    }

    [HarmonyPatch(typeof(RoleCardPanel), "HandleOnMyIdentityChanged")]
    public class player
    {
        static void Postfix(PlayerIdentityData playerIdentityData, ref RoleCardPanel __instance)
        {
            if (Service.Game.Sim.simulation.myIdentity.Data.role==Role.JAILOR)
            {
                ensureButtons.reload = true;
                if (ModSettings.GetBool("Safe Mode")) return;
                __instance.specialAbilityPanel.Hide();
            }
        }
    }
    
    [HarmonyPatch(typeof(RoleCardPanel), "ValidateSpecialAbilityPanel")]
    public class player2
    {
        static void Postfix(ref RoleCardPanel __instance)
        {
            if (Service.Game.Sim.simulation.myIdentity.Data.role==Role.JAILOR&&!ModSettings.GetBool("Safe Mode"))
            {
                 ensureButtons.reload = true;
                if (ModSettings.GetBool("Safe Mode")) return;
                __instance.specialAbilityPanel.Hide();
            }
        }
    }
    [HarmonyPatch(typeof(TosAbilityPanelListItem),"Update")]
    public class ensureButtons
    {
        public static bool reload = false;

        public static void Postfix(ref TosAbilityPanelListItem __instance)
        {
            if (!reload) return;
            bool canJail = true;
            int lastTarget = AddJailButton.lastTarget;
            int lastTargetFresh = AddJailButton.lastTargetFresh;
            PlayPhase phase = Service.Game.Sim.info.gameInfo.Data.playPhase;
            if (canJail && __instance.playerRole !=Role.JAILOR && phase !=PlayPhase.NIGHT && phase !=PlayPhase.NIGHT_END_CINEMATIC &&
                phase !=PlayPhase.NIGHT_WRAP_UP && phase!=PlayPhase.WHO_DIED_AND_HOW&& phase!=PlayPhase.POST_TRIAL_WHO_DIED_AND_HOW
                &&__instance.characterPosition!=lastTarget&& phase != PlayPhase.DAY&&phase!=PlayPhase.FIRST_DAY
               )
            {
                if (!ModStates.IsLoaded("alchlcsystm.recolors"))
                { 
                    __instance.choice2Sprite.sprite = LoadEmbeddedResources.LoadSprite("OldJailor.resources.jail.png");
                }
                __instance.choice2Text.text = "Jail";
                __instance.choice2ButtonCanvasGroup.EnableRenderingAndInteraction();
                if (__instance.characterPosition == lastTargetFresh)
                {
                    __instance.choice2Button.Select();
                }

                if (!__instance.halo.activeSelf)
                {
                    __instance.choice2Button.gameObject.SetActive(true);
                }
            }

            reload = false;
        }

    }
    
    [HarmonyPatch(typeof(TosAbilityPanelListItem),"HandlePlayPhaseChanged")]
    public class AddJailButton
    {
        public static bool canJail = false;
        public static int lastTarget = -1;
        public static int lastTargetFresh = -1;
        static void Postfix(PlayPhaseState playPhase, ref TosAbilityPanelListItem __instance)
        {
            PlayPhase phase = playPhase.playPhase;
            if (phase == PlayPhase.FIRST_DAY || phase == PlayPhase.NONE)
            {
                lastTarget = -1;
                lastTargetFresh = -1;
            }

            if (Service.Game.Sim.simulation.myIdentity.Data.role == Role.JAILOR)
            {
                canJail = true;
            }
            
            if (Service.Game.Sim.info.roleCardObservation.Data.specialAbilityRemaining == 0 ||
                !Service.Game.Sim.info.myDiscussionPlayer.Data.alive)
            {
                canJail = false;
            }
            
            Console.Out.Write("canJail: "+canJail);
            if (canJail&& Service.Game.Sim.simulation.myIdentity.Data.role==Role.JAILOR && __instance.playerRole !=Role.JAILOR && phase !=PlayPhase.NIGHT && phase !=PlayPhase.NIGHT_END_CINEMATIC &&
                 phase !=PlayPhase.NIGHT_WRAP_UP && phase!=PlayPhase.WHO_DIED_AND_HOW&& phase!=PlayPhase.POST_TRIAL_WHO_DIED_AND_HOW
                 &&__instance.characterPosition!=lastTarget&& phase != PlayPhase.DAY&&phase!=PlayPhase.FIRST_DAY
                 )
            {
                if (!ModStates.IsLoaded("alchlcsystm.recolors"))
                { 
                    __instance.choice2Sprite.sprite = LoadEmbeddedResources.LoadSprite("OldJailor.resources.jail.png");
                }
                __instance.choice2Text.text = "Jail";
                __instance.choice2ButtonCanvasGroup.EnableRenderingAndInteraction();
                if (__instance.characterPosition == lastTargetFresh)
                {
                    __instance.choice2Button.Select();
                }

                if (!__instance.halo.activeSelf)
                {
                    __instance.choice2Button.gameObject.SetActive(true);
                }
            }

            if (phase == PlayPhase.NIGHT)
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
            if (Service.Game.Sim.simulation.myIdentity.Data.role!= Role.JAILOR) return true;
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