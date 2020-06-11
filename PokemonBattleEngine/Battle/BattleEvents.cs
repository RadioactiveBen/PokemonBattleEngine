﻿using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngine.Packets;
using Kermalis.PokemonBattleEngine.Utils;
using System;
using System.Linq;

namespace Kermalis.PokemonBattleEngine.Battle
{
    public sealed partial class PBEBattle
    {
        public delegate void BattleEvent(PBEBattle battle, IPBEPacket packet);
        public event BattleEvent OnNewEvent;

        private void Broadcast(IPBEPacket packet)
        {
            Events.Add(packet);
            OnNewEvent?.Invoke(this, packet);
        }

        private void BroadcastAbility(PBEPokemon abilityOwner, PBEPokemon pokemon2, PBEAbility ability, PBEAbilityAction abilityAction)
        {
            abilityOwner.Ability = ability;
            abilityOwner.KnownAbility = ability;
            Broadcast(new PBEAbilityPacket(abilityOwner, pokemon2, ability, abilityAction));
        }
        private void BroadcastAbilityReplaced(PBEPokemon abilityOwner, PBEAbility newAbility)
        {
            PBEAbility? oldAbility = newAbility == PBEAbility.None ? (PBEAbility?)null : abilityOwner.Ability; // Gastro Acid does not reveal previous ability
            abilityOwner.Ability = newAbility;
            abilityOwner.KnownAbility = newAbility;
            Broadcast(new PBEAbilityReplacedPacket(abilityOwner, oldAbility, newAbility));
        }
        private void BroadcastBattleStatus(PBEBattleStatus battleStatus, PBEBattleStatusAction battleStatusAction)
        {
            switch (battleStatusAction)
            {
                case PBEBattleStatusAction.Added: BattleStatus |= battleStatus; break;
                case PBEBattleStatusAction.Cleared:
                case PBEBattleStatusAction.Ended: BattleStatus &= ~battleStatus; break;
                default: throw new ArgumentOutOfRangeException(nameof(battleStatusAction));
            }
            Broadcast(new PBEBattleStatusPacket(battleStatus, battleStatusAction));
        }
        private void BroadcastHaze()
        {
            Broadcast(new PBEHazePacket());
        }
        private void BroadcastIllusion(PBEPokemon pokemon)
        {
            Broadcast(new PBEIllusionPacket(pokemon));
        }
        private void BroadcastItem(PBEPokemon itemHolder, PBEPokemon pokemon2, PBEItem item, PBEItemAction itemAction)
        {
            switch (itemAction)
            {
                case PBEItemAction.Consumed:
                {
                    itemHolder.Item = PBEItem.None;
                    itemHolder.KnownItem = PBEItem.None;
                    break;
                }
                default:
                {
                    itemHolder.Item = item;
                    itemHolder.KnownItem = item;
                    break;
                }
            }
            Broadcast(new PBEItemPacket(itemHolder, pokemon2, item, itemAction));
        }
        private void BroadcastMoveCrit(PBEPokemon victim)
        {
            Broadcast(new PBEMoveCritPacket(victim));
        }
        private void BroadcastMoveLock_ChoiceItem(PBEPokemon moveUser, PBEMove lockedMove)
        {
            moveUser.ChoiceLockedMove = lockedMove;
            Broadcast(new PBEMoveLockPacket(moveUser, PBEMoveLockType.ChoiceItem, lockedMove));
        }
        private void BroadcastMoveLock_Temporary(PBEPokemon moveUser, PBEMove lockedMove, PBETurnTarget lockedTargets)
        {
            moveUser.TempLockedMove = lockedMove;
            moveUser.TempLockedTargets = lockedTargets;
            Broadcast(new PBEMoveLockPacket(moveUser, PBEMoveLockType.Temporary, lockedMove, lockedTargets));
        }
        private void BroadcastMoveMissed(PBEPokemon moveUser, PBEPokemon pokemon2)
        {
            Broadcast(new PBEMoveMissedPacket(moveUser, pokemon2));
        }
        private void BroadcastMovePPChanged(PBEPokemon moveUser, PBEMove move, int amountReduced)
        {
            Broadcast(new PBEMovePPChangedPacket(moveUser, move, amountReduced));
        }
        private void BroadcastMoveResult(PBEPokemon moveUser, PBEPokemon pokemon2, PBEResult result)
        {
            Broadcast(new PBEMoveResultPacket(moveUser, pokemon2, result));
        }
        private void BroadcastMoveUsed(PBEPokemon moveUser, PBEMove move)
        {
            bool reveal;
            if (!_calledFromOtherMove && moveUser.Moves.Contains(move) && !moveUser.KnownMoves.Contains(move))
            {
                moveUser.KnownMoves[PBEMove.MAX].Move = move;
                reveal = true;
            }
            else
            {
                reveal = false;
            }
            Broadcast(new PBEMoveUsedPacket(moveUser, move, reveal));
        }
        private void BroadcastPkmnFainted(PBEPokemon pokemon, PBEFieldPosition oldPosition)
        {
            Broadcast(new PBEPkmnFaintedPacket(pokemon, oldPosition));
        }
        private void BroadcastPkmnFormChanged(PBEPokemon pokemon, PBEForm newForm, PBEAbility newAbility, PBEAbility newKnownAbility, bool isRevertForm)
        {
            pokemon.Ability = newAbility;
            pokemon.KnownAbility = newKnownAbility;
            pokemon.Form = newForm;
            pokemon.KnownForm = newForm;
            if (isRevertForm)
            {
                pokemon.RevertForm = newForm;
                pokemon.RevertAbility = newAbility;
            }
            pokemon.SetStats(false);
            var pData = PBEPokemonData.GetData(pokemon.Species, newForm);
            PBEType type1 = pData.Type1;
            pokemon.Type1 = type1;
            pokemon.KnownType1 = type1;
            PBEType type2 = pData.Type2;
            pokemon.Type2 = type2;
            pokemon.KnownType2 = type2;
            double weight = pData.Weight; // TODO: Is weight updated here? Bulbapedia claims in Autotomize's page that it is not
            pokemon.Weight = weight;
            pokemon.KnownWeight = weight;
            Broadcast(new PBEPkmnFormChangedPacket(pokemon, isRevertForm));
            // BUG: PBEStatus2.PowerTrick is not cleared when changing form in any game
#if BUGFIX
            if (pokemon.Status2.HasFlag(PBEStatus2.PowerTrick))
            {
                BroadcastStatus2(pokemon, pokemon, PBEStatus2.PowerTrick, PBEStatusAction.Ended);
            }
#endif
        }
        private void BroadcastPkmnHPChanged(PBEPokemon pokemon, ushort oldHP, double oldHPPercentage)
        {
            Broadcast(new PBEPkmnHPChangedPacket(pokemon, oldHP, oldHPPercentage));
        }
        private void BroadcastPkmnStatChanged(PBEPokemon pokemon, PBEStat stat, sbyte oldValue, sbyte newValue)
        {
            Broadcast(new PBEPkmnStatChangedPacket(pokemon, stat, oldValue, newValue));
        }
        private void BroadcastPkmnSwitchIn(PBETeam team, PBEPkmnSwitchInPacket.PBESwitchInInfo[] switchIns, PBEPokemon forcedByPokemon = null)
        {
            Broadcast(new PBEPkmnSwitchInPacket(team, switchIns, forcedByPokemon));
        }
        private void BroadcastPkmnSwitchOut(PBEPokemon pokemon, PBEPokemon disguisedAsPokemon, PBEFieldPosition oldPosition, PBEPokemon forcedByPokemon = null)
        {
            Broadcast(new PBEPkmnSwitchOutPacket(pokemon, disguisedAsPokemon, oldPosition, forcedByPokemon));
        }
        private void BroadcastPsychUp(PBEPokemon user, PBEPokemon target)
        {
            Broadcast(new PBEPsychUpPacket(user, target));
        }

        private void BroadcastDraggedOut(PBEPokemon pokemon)
        {
            Broadcast(new PBESpecialMessagePacket(PBESpecialMessage.DraggedOut, pokemon));
        }
        private void BroadcastEndure(PBEPokemon pokemon)
        {
            Broadcast(new PBESpecialMessagePacket(PBESpecialMessage.Endure, pokemon));
        }
        private void BroadcastHPDrained(PBEPokemon pokemon)
        {
            Broadcast(new PBESpecialMessagePacket(PBESpecialMessage.HPDrained, pokemon));
        }
        private void BroadcastMagnitude(byte magnitude)
        {
            Broadcast(new PBESpecialMessagePacket(PBESpecialMessage.Magnitude, magnitude));
        }
        private void BroadcastMultiHit(byte numHits)
        {
            Broadcast(new PBESpecialMessagePacket(PBESpecialMessage.MultiHit, numHits));
        }
        private void BroadcastNothingHappened()
        {
            Broadcast(new PBESpecialMessagePacket(PBESpecialMessage.NothingHappened));
        }
        private void BroadcastOneHitKnockout()
        {
            Broadcast(new PBESpecialMessagePacket(PBESpecialMessage.OneHitKnockout));
        }
        private void BroadcastPainSplit(PBEPokemon user, PBEPokemon target)
        {
            Broadcast(new PBESpecialMessagePacket(PBESpecialMessage.PainSplit, user, target));
        }
        private void BroadcastRecoil(PBEPokemon pokemon)
        {
            Broadcast(new PBESpecialMessagePacket(PBESpecialMessage.Recoil, pokemon));
        }
        private void BroadcastStruggle(PBEPokemon pokemon)
        {
            Broadcast(new PBESpecialMessagePacket(PBESpecialMessage.Struggle, pokemon));
        }

        private void BroadcastStatus1(PBEPokemon status1Receiver, PBEPokemon pokemon2, PBEStatus1 status1, PBEStatusAction statusAction)
        {
            Broadcast(new PBEStatus1Packet(status1Receiver, pokemon2, status1, statusAction));
        }
        private void BroadcastStatus2(PBEPokemon status2Receiver, PBEPokemon pokemon2, PBEStatus2 status2, PBEStatusAction statusAction)
        {
            switch (statusAction)
            {
                case PBEStatusAction.Added:
                case PBEStatusAction.Announced:
                case PBEStatusAction.CausedImmobility:
                case PBEStatusAction.Damage: status2Receiver.Status2 |= status2; status2Receiver.KnownStatus2 |= status2; break;
                case PBEStatusAction.Cleared:
                case PBEStatusAction.Ended: status2Receiver.Status2 &= ~status2; status2Receiver.KnownStatus2 &= ~status2; break;
                default: throw new ArgumentOutOfRangeException(nameof(statusAction));
            }
            Broadcast(new PBEStatus2Packet(status2Receiver, pokemon2, status2, statusAction));
        }
        private void BroadcastTeamStatus(PBETeam team, PBETeamStatus teamStatus, PBETeamStatusAction teamStatusAction, PBEPokemon damageVictim = null)
        {
            switch (teamStatusAction)
            {
                case PBETeamStatusAction.Added:
                case PBETeamStatusAction.Damage: team.TeamStatus |= teamStatus; break;
                case PBETeamStatusAction.Cleared:
                case PBETeamStatusAction.Ended: team.TeamStatus &= ~teamStatus; break;
                default: throw new ArgumentOutOfRangeException(nameof(teamStatusAction));
            }
            Broadcast(new PBETeamStatusPacket(team, teamStatus, teamStatusAction, damageVictim));
        }
        private void BroadcastTransform(PBEPokemon user, PBEPokemon target)
        {
            Broadcast(new PBETransformPacket(user, target));
        }
        private void BroadcastTypeChanged(PBEPokemon pokemon, PBEType type1, PBEType type2)
        {
            pokemon.Type1 = type1;
            pokemon.KnownType1 = type1;
            pokemon.Type2 = type2;
            pokemon.KnownType2 = type2;
            Broadcast(new PBETypeChangedPacket(pokemon, type1, type2));
        }
        private void BroadcastWeather(PBEWeather weather, PBEWeatherAction weatherAction, PBEPokemon damageVictim = null)
        {
            Broadcast(new PBEWeatherPacket(weather, weatherAction, damageVictim));
        }
        private void BroadcastActionsRequest(PBETeam team)
        {
            Broadcast(new PBEActionsRequestPacket(team));
        }
        private void BroadcastAutoCenter(PBEPokemon pokemon1, PBEFieldPosition pokemon1OldPosition, PBEPokemon pokemon2, PBEFieldPosition pokemon2OldPosition)
        {
            Broadcast(new PBEAutoCenterPacket(pokemon1, pokemon1OldPosition, pokemon2, pokemon2OldPosition));
        }
        private void BroadcastTeam(PBETeam team)
        {
            Broadcast(new PBETeamPacket(team));
        }
        private void BroadcastSwitchInRequest(PBETeam team)
        {
            Broadcast(new PBESwitchInRequestPacket(team));
        }
        private void BroadcastTurnBegan()
        {
            Broadcast(new PBETurnBeganPacket(TurnNumber));
        }
        private void BroadcastWinner(PBETeam winningTeam)
        {
            Broadcast(new PBEWinnerPacket(winningTeam));
        }


        /// <summary>Writes battle events to <see cref="Console.Out"/> in English.</summary>
        /// <param name="battle">The battle that <paramref name="packet"/> belongs to.</param>
        /// <param name="packet">The battle event packet.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="battle"/> or <paramref name="packet"/> are null.</exception>
        public static void ConsoleBattleEventHandler(PBEBattle battle, IPBEPacket packet)
        {
            if (battle == null)
            {
                throw new ArgumentNullException(nameof(battle));
            }
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }
            if (battle.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(battle));
            }

            string NameForTrainer(PBEPokemon pkmn)
            {
                return pkmn == null ? string.Empty : $"{pkmn.Team.TrainerName}'s {pkmn.KnownNickname}";
            }

            switch (packet)
            {
                case PBEAbilityPacket ap:
                {
                    PBEPokemon abilityOwner = ap.AbilityOwnerTeam.TryGetPokemon(ap.AbilityOwner),
                            pokemon2 = ap.Pokemon2Team.TryGetPokemon(ap.Pokemon2);
                    string message;
                    switch (ap.Ability)
                    {
                        case PBEAbility.AirLock:
                        case PBEAbility.CloudNine:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Weather: message = "{0}'s {2} causes the effects of weather to disappear!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.Anticipation:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Announced: message = "{0}'s {2} made it shudder!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.BadDreams:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Damage: message = "{1} is tormented by {0}'s {2}!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.ClearBody:
                        case PBEAbility.WhiteSmoke:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Stats: message = "{0}'s {2} prevents stat reduction!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.ColorChange:
                        case PBEAbility.FlowerGift:
                        case PBEAbility.Forecast:
                        case PBEAbility.Imposter:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.ChangedAppearance: message = "{0}'s {2} activated!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.CuteCharm:
                        case PBEAbility.EffectSpore:
                        case PBEAbility.FlameBody:
                        case PBEAbility.Healer:
                        case PBEAbility.PoisonPoint:
                        case PBEAbility.ShedSkin:
                        case PBEAbility.Static:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.ChangedStatus: message = "{0}'s {2} activated!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.Download:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Stats: message = "{0}'s {2} activated!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.Drizzle:
                        case PBEAbility.Drought:
                        case PBEAbility.SandStream:
                        case PBEAbility.SnowWarning:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Weather: message = "{0}'s {2} activated!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.IceBody:
                        case PBEAbility.RainDish:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.RestoredHP: message = "{0}'s {2} activated!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.Illusion:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.ChangedAppearance: return;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                        }
                        case PBEAbility.Immunity:
                        case PBEAbility.Insomnia:
                        case PBEAbility.Limber:
                        case PBEAbility.MagmaArmor:
                        case PBEAbility.Oblivious:
                        case PBEAbility.OwnTempo:
                        case PBEAbility.VitalSpirit:
                        case PBEAbility.WaterVeil:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.ChangedStatus:
                                case PBEAbilityAction.PreventedStatus: message = "{0}'s {2} activated!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.IronBarbs:
                        case PBEAbility.Justified:
                        case PBEAbility.Levitate:
                        case PBEAbility.Mummy:
                        case PBEAbility.Rattled:
                        case PBEAbility.RoughSkin:
                        case PBEAbility.SolarPower:
                        case PBEAbility.Sturdy:
                        case PBEAbility.WeakArmor:
                        case PBEAbility.WonderGuard:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Damage: message = "{0}'s {2} activated!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.LeafGuard:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.PreventedStatus: message = "{0}'s {2} activated!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.LiquidOoze:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Damage: message = "{1} sucked up the liquid ooze!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.MoldBreaker:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Announced: message = "{0} breaks the mold!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.Moody:
                        case PBEAbility.SpeedBoost:
                        case PBEAbility.Steadfast:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Stats: message = "{0}'s {2} activated!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.SlowStart:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Announced: message = "{0} can't get it going!"; break;
                                case PBEAbilityAction.SlowStart_Ended: message = "{0} finally got its act together!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.Teravolt:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Announced: message = "{0} is radiating a bursting aura!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        case PBEAbility.Turboblaze:
                        {
                            switch (ap.AbilityAction)
                            {
                                case PBEAbilityAction.Announced: message = "{0} is radiating a blazing aura!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ap.AbilityAction));
                            }
                            break;
                        }
                        default: throw new ArgumentOutOfRangeException(nameof(ap.Ability));
                    }
                    Console.WriteLine(message, NameForTrainer(abilityOwner), NameForTrainer(pokemon2), PBELocalizedString.GetAbilityName(ap.Ability).English);
                    break;
                }
                case PBEAbilityReplacedPacket arp:
                {
                    PBEPokemon abilityOwner = arp.AbilityOwnerTeam.TryGetPokemon(arp.AbilityOwner);
                    string message;
                    switch (arp.NewAbility)
                    {
                        case PBEAbility.None: message = "{0}'s {1} was suppressed!"; break;
                        default: message = "{0}'s {1} was changed to {2}!"; break;
                    }
                    Console.WriteLine(message, NameForTrainer(abilityOwner), arp.OldAbility.HasValue ? PBELocalizedString.GetAbilityName(arp.OldAbility.Value).English : "Ability", PBELocalizedString.GetAbilityName(arp.NewAbility).English);
                    break;
                }
                case PBEBattleStatusPacket bsp:
                {
                    string message;
                    switch (bsp.BattleStatus)
                    {
                        case PBEBattleStatus.TrickRoom:
                        {
                            switch (bsp.BattleStatusAction)
                            {
                                case PBEBattleStatusAction.Added: message = "The dimensions were twisted!"; break;
                                case PBEBattleStatusAction.Cleared:
                                case PBEBattleStatusAction.Ended: message = "The twisted dimensions returned to normal!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(bsp.BattleStatusAction));
                            }
                            break;
                        }
                        default: throw new ArgumentOutOfRangeException(nameof(bsp.BattleStatus));
                    }
                    Console.WriteLine(message);
                    break;
                }
                case PBEHazePacket _:
                {
                    Console.WriteLine("All stat changes were eliminated!");
                    break;
                }
                case PBEItemPacket ip:
                {
                    PBEPokemon itemHolder = ip.ItemHolderTeam.TryGetPokemon(ip.ItemHolder),
                            pokemon2 = ip.Pokemon2Team.TryGetPokemon(ip.Pokemon2);
                    string message;
                    switch (ip.Item)
                    {
                        case PBEItem.AguavBerry:
                        case PBEItem.BerryJuice:
                        case PBEItem.FigyBerry:
                        case PBEItem.IapapaBerry:
                        case PBEItem.MagoBerry:
                        case PBEItem.OranBerry:
                        case PBEItem.SitrusBerry:
                        case PBEItem.WikiBerry:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.Consumed: message = "{0} restored its health using its {2}!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        case PBEItem.ApicotBerry:
                        case PBEItem.GanlonBerry:
                        case PBEItem.LiechiBerry:
                        case PBEItem.PetayaBerry:
                        case PBEItem.SalacBerry:
                        case PBEItem.StarfBerry:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.Consumed: message = "{0} used its {2}!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        case PBEItem.BugGem:
                        case PBEItem.DarkGem:
                        case PBEItem.DragonGem:
                        case PBEItem.ElectricGem:
                        case PBEItem.FightingGem:
                        case PBEItem.FireGem:
                        case PBEItem.FlyingGem:
                        case PBEItem.GhostGem:
                        case PBEItem.GrassGem:
                        case PBEItem.GroundGem:
                        case PBEItem.IceGem:
                        case PBEItem.NormalGem:
                        case PBEItem.PoisonGem:
                        case PBEItem.PsychicGem:
                        case PBEItem.RockGem:
                        case PBEItem.SteelGem:
                        case PBEItem.WaterGem:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.Consumed: message = "The {2} strengthened {0}'s power!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        case PBEItem.BlackSludge:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.Damage: message = "{0} is hurt by its {2}!"; break;
                                case PBEItemAction.RestoredHP: message = "{0} restored a little HP using its {2}!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        case PBEItem.DestinyKnot:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.ChangedStatus: message = "{0}'s {2} activated!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        case PBEItem.FlameOrb:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.ChangedStatus: message = "{0} was burned by its {2}!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        case PBEItem.FocusBand:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.Damage: message = "{0} hung on using its {2}!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        case PBEItem.FocusSash:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.Consumed: message = "{0} hung on using its {2}!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        case PBEItem.Leftovers:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.RestoredHP: message = "{0} restored a little HP using its {2}!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        case PBEItem.LifeOrb:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.Damage: message = "{0} is hurt by its {2}!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        case PBEItem.PowerHerb:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.Consumed: message = "{0} became fully charged due to its {2}!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        case PBEItem.RockyHelmet:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.Damage: message = "{1} was hurt by the {2}!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        case PBEItem.ToxicOrb:
                        {
                            switch (ip.ItemAction)
                            {
                                case PBEItemAction.ChangedStatus: message = "{0} was badly poisoned by its {2}!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(ip.ItemAction));
                            }
                            break;
                        }
                        default: throw new ArgumentOutOfRangeException(nameof(ip.Item));
                    }
                    Console.WriteLine(message, NameForTrainer(itemHolder), NameForTrainer(pokemon2), PBELocalizedString.GetItemName(ip.Item).English);
                    break;
                }
                case PBEMoveCritPacket mcp:
                {
                    PBEPokemon victim = mcp.VictimTeam.TryGetPokemon(mcp.Victim);
                    Console.WriteLine("A critical hit on {0}!", NameForTrainer(victim));
                    break;
                }
                case PBEMoveMissedPacket mmp:
                {
                    PBEPokemon moveUser = mmp.MoveUserTeam.TryGetPokemon(mmp.MoveUser),
                            pokemon2 = mmp.Pokemon2Team.TryGetPokemon(mmp.Pokemon2);
                    Console.WriteLine("{0}'s attack missed {1}!", NameForTrainer(moveUser), NameForTrainer(pokemon2));
                    break;
                }
                case PBEMovePPChangedPacket mpcp:
                {
                    PBEPokemon moveUser = mpcp.MoveUserTeam.TryGetPokemon(mpcp.MoveUser);
                    Console.WriteLine("{0}'s {1} {3} {2} PP!", NameForTrainer(moveUser), PBELocalizedString.GetMoveName(mpcp.Move).English, Math.Abs(mpcp.AmountReduced), mpcp.AmountReduced >= 0 ? "lost" : "gained");
                    break;
                }
                case PBEMoveResultPacket mrp:
                {
                    PBEPokemon moveUser = mrp.MoveUserTeam.TryGetPokemon(mrp.MoveUser),
                            pokemon2 = mrp.Pokemon2Team.TryGetPokemon(mrp.Pokemon2);
                    string message;
                    switch (mrp.Result)
                    {
                        case PBEResult.Ineffective_Ability: message = "{1} is protected by its Ability!"; break;
                        case PBEResult.Ineffective_Gender: message = "It doesn't affect {1}..."; break;
                        case PBEResult.Ineffective_Level: message = "{1} is protected by its level!"; break;
                        case PBEResult.Ineffective_MagnetRise: message = $"{{1}} is protected by {PBELocalizedString.GetMoveName(PBEMove.MagnetRise).English}!"; break;
                        case PBEResult.Ineffective_Safeguard: message = $"{{1}} is protected by {PBELocalizedString.GetMoveName(PBEMove.Safeguard).English}!"; break;
                        case PBEResult.Ineffective_Stat:
                        case PBEResult.Ineffective_Status:
                        case PBEResult.InvalidConditions: message = "But it failed!"; break;
                        case PBEResult.Ineffective_Substitute: message = $"{{1}} is protected by {PBELocalizedString.GetMoveName(PBEMove.Substitute).English}!"; break;
                        case PBEResult.Ineffective_Type: message = "{1} is protected by its Type!"; break;
                        case PBEResult.NoTarget: message = "But there was no target..."; break;
                        case PBEResult.NotVeryEffective_Type: message = "It's not very effective on {1}..."; break;
                        case PBEResult.SuperEffective_Type: message = "It's super effective on {1}!"; break;
                        default: throw new ArgumentOutOfRangeException(nameof(mrp.Result));
                    }
                    Console.WriteLine(message, NameForTrainer(moveUser), NameForTrainer(pokemon2));
                    break;
                }
                case PBEMoveUsedPacket mup:
                {
                    PBEPokemon moveUser = mup.MoveUserTeam.TryGetPokemon(mup.MoveUser);
                    Console.WriteLine("{0} used {1}!", NameForTrainer(moveUser), PBELocalizedString.GetMoveName(mup.Move).English);
                    break;
                }
                case PBEPkmnFaintedPacket pfap:
                {
                    PBEPokemon pokemon;
                    if (pfap.PokemonId == byte.MaxValue)
                    {
                        pokemon = pfap.PokemonTeam.TryGetPokemon(pfap.PokemonPosition);
                    }
                    else
                    {
                        pokemon = pfap.PokemonTeam.TryGetPokemon(pfap.PokemonId);
                    }
                    Console.WriteLine("{0} fainted!", NameForTrainer(pokemon));
                    break;
                }
                case PBEPkmnFormChangedPacket pfcp:
                {
                    PBEPokemon pokemon = pfcp.PokemonTeam.TryGetPokemon(pfcp.Pokemon);
                    Console.WriteLine("{0}'s new form is {1}!", NameForTrainer(pokemon), PBELocalizedString.GetFormName(pokemon.Species, pfcp.NewForm).English);
                    break;
                }
                case PBEPkmnHPChangedPacket phcp:
                {
                    PBEPokemon pokemon = phcp.PokemonTeam.TryGetPokemon(phcp.Pokemon);
                    int change = phcp.NewHP - phcp.OldHP;
                    int absChange = Math.Abs(change);
                    double percentageChange = phcp.NewHPPercentage - phcp.OldHPPercentage;
                    double absPercentageChange = Math.Abs(percentageChange);
                    if (pokemon.HP == 0 && pokemon.MaxHP == 0)
                    {
                        Console.WriteLine("{0} {1} {2:P2} of its HP!", NameForTrainer(pokemon), percentageChange <= 0 ? "lost" : "restored", absPercentageChange);
                    }
                    else
                    {
                        Console.WriteLine("{0} {1} {2} ({3:P2}) HP!", NameForTrainer(pokemon), change <= 0 ? "lost" : "restored", absChange, absPercentageChange);
                    }
                    break;
                }
                case PBEPkmnStatChangedPacket pscp:
                {
                    PBEPokemon pokemon = pscp.PokemonTeam.TryGetPokemon(pscp.Pokemon);
                    string statName, message;
                    switch (pscp.Stat)
                    {
                        case PBEStat.Accuracy: statName = "Accuracy"; break;
                        case PBEStat.Attack: statName = "Attack"; break;
                        case PBEStat.Defense: statName = "Defense"; break;
                        case PBEStat.Evasion: statName = "Evasion"; break;
                        case PBEStat.SpAttack: statName = "Special Attack"; break;
                        case PBEStat.SpDefense: statName = "Special Defense"; break;
                        case PBEStat.Speed: statName = "Speed"; break;
                        default: throw new ArgumentOutOfRangeException(nameof(pscp.Stat));
                    }
                    int change = pscp.NewValue - pscp.OldValue;
                    switch (change)
                    {
                        case -2: message = "harshly fell"; break;
                        case -1: message = "fell"; break;
                        case +1: message = "rose"; break;
                        case +2: message = "rose sharply"; break;
                        default:
                        {
                            if (change == 0 && pscp.NewValue == -battle.Settings.MaxStatChange)
                            {
                                message = "won't go lower";
                            }
                            else if (change == 0 && pscp.NewValue == battle.Settings.MaxStatChange)
                            {
                                message = "won't go higher";
                            }
                            else if (change <= -3)
                            {
                                message = "severely fell";
                            }
                            else if (change >= +3)
                            {
                                message = "rose drastically";
                            }
                            else
                            {
                                throw new ArgumentOutOfRangeException();
                            }
                            break;
                        }
                    }
                    Console.WriteLine("{0}'s {1} {2}!", NameForTrainer(pokemon), statName, message);
                    break;
                }
                case PBEPkmnSwitchInPacket psip:
                {
                    if (!psip.Forced)
                    {
                        Console.WriteLine("{1} sent out {0}!", psip.SwitchIns.Select(s => s.Nickname).ToArray().Andify(), psip.Team.TrainerName);
                    }
                    break;
                }
                case PBEPkmnSwitchOutPacket psop:
                {
                    if (!psop.Forced)
                    {
                        PBEPokemon disguisedAsPokemon = psop.DisguisedAsPokemonId != byte.MaxValue ? psop.PokemonTeam.TryGetPokemon(psop.DisguisedAsPokemonId) : psop.PokemonTeam.TryGetPokemon(psop.PokemonPosition);
                        Console.WriteLine("{1} withdrew {0}!", disguisedAsPokemon.Nickname, psop.PokemonTeam.TrainerName);
                    }
                    break;
                }
                case PBEPsychUpPacket pup:
                {
                    PBEPokemon user = pup.UserTeam.TryGetPokemon(pup.User),
                            target = pup.TargetTeam.TryGetPokemon(pup.Target);
                    Console.WriteLine("{0} copied {1}'s stat changes!", NameForTrainer(user), NameForTrainer(target));
                    break;
                }
                case PBESpecialMessagePacket smp:
                {
                    string message;
                    switch (smp.Message)
                    {
                        case PBESpecialMessage.DraggedOut:
                        {
                            message = string.Format("{0} was dragged out!", NameForTrainer(((PBETeam)smp.Params[1]).TryGetPokemon((PBEFieldPosition)smp.Params[0])));
                            break;
                        }
                        case PBESpecialMessage.Endure:
                        {
                            message = string.Format("{0} endured the hit!", NameForTrainer(((PBETeam)smp.Params[1]).TryGetPokemon((PBEFieldPosition)smp.Params[0])));
                            break;
                        }
                        case PBESpecialMessage.HPDrained:
                        {
                            message = string.Format("{0} had its energy drained!", NameForTrainer(((PBETeam)smp.Params[1]).TryGetPokemon((PBEFieldPosition)smp.Params[0])));
                            break;
                        }
                        case PBESpecialMessage.Magnitude:
                        {
                            message = string.Format("Magnitude {0}!", (byte)smp.Params[0]);
                            break;
                        }
                        case PBESpecialMessage.MultiHit:
                        {
                            message = string.Format("Hit {0} time(s)!", (byte)smp.Params[0]);
                            break;
                        }
                        case PBESpecialMessage.NothingHappened:
                        {
                            message = "But nothing happened!";
                            break;
                        }
                        case PBESpecialMessage.OneHitKnockout:
                        {
                            message = "It's a one-hit KO!";
                            break;
                        }
                        case PBESpecialMessage.PainSplit:
                        {
                            message = string.Format("{0} and {1} shared pain!", NameForTrainer(((PBETeam)smp.Params[1]).TryGetPokemon((PBEFieldPosition)smp.Params[0])), NameForTrainer(((PBETeam)smp.Params[3]).TryGetPokemon((PBEFieldPosition)smp.Params[2])));
                            break;
                        }
                        case PBESpecialMessage.Recoil:
                        {
                            message = string.Format("{0} is damaged by recoil!", NameForTrainer(((PBETeam)smp.Params[1]).TryGetPokemon((PBEFieldPosition)smp.Params[0])));
                            break;
                        }
                        case PBESpecialMessage.Struggle:
                        {
                            message = string.Format("{0} has no moves left!", NameForTrainer(((PBETeam)smp.Params[1]).TryGetPokemon((PBEFieldPosition)smp.Params[0])));
                            break;
                        }
                        default: throw new ArgumentOutOfRangeException(nameof(smp.Message));
                    }
                    Console.WriteLine(message);
                    break;
                }
                case PBEStatus1Packet s1p:
                {
                    PBEPokemon status1Receiver = s1p.Status1ReceiverTeam.TryGetPokemon(s1p.Status1Receiver);
                    string message;
                    switch (s1p.Status1)
                    {
                        case PBEStatus1.Asleep:
                        {
                            switch (s1p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} fell asleep!"; break;
                                case PBEStatusAction.CausedImmobility: message = "{0} is fast asleep."; break;
                                case PBEStatusAction.Cleared:
                                case PBEStatusAction.Ended: message = "{0} woke up!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s1p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus1.BadlyPoisoned:
                        {
                            switch (s1p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} was badly poisoned!"; break;
                                case PBEStatusAction.Cleared: message = "{0} was cured of its poisoning."; break;
                                case PBEStatusAction.Damage: message = "{0} was hurt by poison!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s1p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus1.Burned:
                        {
                            switch (s1p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} was burned!"; break;
                                case PBEStatusAction.Cleared: message = "{0}'s burn was healed."; break;
                                case PBEStatusAction.Damage: message = "{0} was hurt by its burn!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s1p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus1.Frozen:
                        {
                            switch (s1p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} was frozen solid!"; break;
                                case PBEStatusAction.CausedImmobility: message = "{0} is frozen solid!"; break;
                                case PBEStatusAction.Cleared:
                                case PBEStatusAction.Ended: message = "{0} thawed out!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s1p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus1.Paralyzed:
                        {
                            switch (s1p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} is paralyzed! It may be unable to move!"; break;
                                case PBEStatusAction.CausedImmobility: message = "{0} is paralyzed! It can't move!"; break;
                                case PBEStatusAction.Cleared: message = "{0} was cured of paralysis."; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s1p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus1.Poisoned:
                        {
                            switch (s1p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} was poisoned!"; break;
                                case PBEStatusAction.Cleared: message = "{0} was cured of its poisoning."; break;
                                case PBEStatusAction.Damage: message = "{0} was hurt by poison!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s1p.StatusAction));
                            }
                            break;
                        }
                        default: throw new ArgumentOutOfRangeException(nameof(s1p.Status1));
                    }
                    Console.WriteLine(message, NameForTrainer(status1Receiver));
                    break;
                }
                case PBEStatus2Packet s2p:
                {
                    PBEPokemon status2Receiver = s2p.Status2ReceiverTeam.TryGetPokemon(s2p.Status2Receiver),
                            pokemon2 = s2p.Pokemon2Team.TryGetPokemon(s2p.Pokemon2);
                    string message;
                    switch (s2p.Status2)
                    {
                        case PBEStatus2.Airborne:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} flew up high!"; break;
                                case PBEStatusAction.Ended: return;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.Confused:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} became confused!"; break;
                                case PBEStatusAction.Announced: message = "{0} is confused!"; break;
                                case PBEStatusAction.Cleared:
                                case PBEStatusAction.Ended: message = "{0} snapped out of its confusion."; break;
                                case PBEStatusAction.Damage: message = "It hurt itself in its confusion!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.Cursed:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{1} cut its own HP and laid a curse on {0}!"; break;
                                case PBEStatusAction.Damage: message = "{0} is afflicted by the curse!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.Disguised:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Ended: message = "{0}'s illusion wore off!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.Flinching:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.CausedImmobility: message = "{0} flinched and couldn't move!"; break;
                                case PBEStatusAction.Ended: return;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.HelpingHand:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{1} is ready to help {0}!"; break;
                                case PBEStatusAction.Ended: return;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.Infatuated:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} fell in love with {1}!"; break;
                                case PBEStatusAction.Announced: message = "{0} is in love with {1}!"; break;
                                case PBEStatusAction.CausedImmobility: message = "{0} is immobilized by love!"; break;
                                case PBEStatusAction.Cleared:
                                case PBEStatusAction.Ended: message = "{0} got over its infatuation."; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.LeechSeed:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} was seeded!"; break;
                                case PBEStatusAction.Damage: message = "{0}'s health is sapped by Leech Seed!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.LockOn:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} took aim at {1}!"; break;
                                case PBEStatusAction.Ended: return;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.MagnetRise:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} levitated with electromagnetism!"; break;
                                case PBEStatusAction.Ended: message = "{0}'s electromagnetism wore off!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.MiracleEye:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} was identified!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.PowerTrick:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} switched its Attack and Defense!"; break;
                                case PBEStatusAction.Ended: return;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.Protected:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added:
                                case PBEStatusAction.Damage: message = "{0} protected itself!"; break;
                                case PBEStatusAction.Cleared: message = "{1} broke through {0}'s protection!"; break;
                                case PBEStatusAction.Ended: return;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.Pumped:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} is getting pumped!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.ShadowForce:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} vanished instantly!"; break;
                                case PBEStatusAction.Ended: return;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.Substitute:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} put in a substitute!"; break;
                                case PBEStatusAction.Damage: message = "The substitute took damage for {0}!"; break;
                                case PBEStatusAction.Ended: message = "{0}'s substitute faded!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.Transformed:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} transformed into {1}!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.Underground:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} burrowed its way under the ground!"; break;
                                case PBEStatusAction.Ended: return;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        case PBEStatus2.Underwater:
                        {
                            switch (s2p.StatusAction)
                            {
                                case PBEStatusAction.Added: message = "{0} hid underwater!"; break;
                                case PBEStatusAction.Ended: return;
                                default: throw new ArgumentOutOfRangeException(nameof(s2p.StatusAction));
                            }
                            break;
                        }
                        default: throw new ArgumentOutOfRangeException(nameof(s2p.Status2));
                    }
                    Console.WriteLine(message, NameForTrainer(status2Receiver), NameForTrainer(pokemon2));
                    break;
                }
                case PBETeamStatusPacket tsp:
                {
                    PBEPokemon damageVictim = tsp.DamageVictim.HasValue ? tsp.Team.TryGetPokemon(tsp.DamageVictim.Value) : null;
                    string message;
                    switch (tsp.TeamStatus)
                    {
                        case PBETeamStatus.LightScreen:
                        {
                            switch (tsp.TeamStatusAction)
                            {
                                case PBETeamStatusAction.Added: message = "Light Screen raised {0}'s team's Special Defense!"; break;
                                case PBETeamStatusAction.Cleared:
                                case PBETeamStatusAction.Ended: message = "{0}'s team's Light Screen wore off!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(tsp.TeamStatusAction));
                            }
                            break;
                        }
                        case PBETeamStatus.LuckyChant:
                        {
                            switch (tsp.TeamStatusAction)
                            {
                                case PBETeamStatusAction.Added: message = "The Lucky Chant shielded {0}'s team from critical hits!"; break;
                                case PBETeamStatusAction.Ended: message = "{0}'s team's Lucky Chant wore off!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(tsp.TeamStatusAction));
                            }
                            break;
                        }
                        case PBETeamStatus.Reflect:
                        {
                            switch (tsp.TeamStatusAction)
                            {
                                case PBETeamStatusAction.Added: message = "Reflect raised {0}'s team's Defense!"; break;
                                case PBETeamStatusAction.Cleared:
                                case PBETeamStatusAction.Ended: message = "{0}'s team's Reflect wore off!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(tsp.TeamStatusAction));
                            }
                            break;
                        }
                        case PBETeamStatus.Safeguard:
                        {
                            switch (tsp.TeamStatusAction)
                            {
                                case PBETeamStatusAction.Added: message = "{0}'s team became cloaked in a mystical veil!"; break;
                                case PBETeamStatusAction.Ended: message = "{0}'s team is no longer protected by Safeguard!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(tsp.TeamStatusAction));
                            }
                            break;
                        }
                        case PBETeamStatus.Spikes:
                        {
                            switch (tsp.TeamStatusAction)
                            {
                                case PBETeamStatusAction.Added: message = "Spikes were scattered all around the feet of {0}'s team!"; break;
                                //case PBETeamStatusAction.Cleared: message = "The spikes disappeared from around {0}'s team's feet!"; break;
                                case PBETeamStatusAction.Damage: message = "{1} is hurt by the spikes!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(tsp.TeamStatusAction));
                            }
                            break;
                        }
                        case PBETeamStatus.StealthRock:
                        {
                            switch (tsp.TeamStatusAction)
                            {
                                case PBETeamStatusAction.Added: message = "Pointed stones float in the air around {0}'s team!"; break;
                                //case PBETeamStatusAction.Cleared: message = "The pointed stones disappeared from around {0}'s team!"; break;
                                case PBETeamStatusAction.Damage: message = "Pointed stones dug into {1}!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(tsp.TeamStatusAction));
                            }
                            break;
                        }
                        case PBETeamStatus.Tailwind:
                        {
                            switch (tsp.TeamStatusAction)
                            {
                                case PBETeamStatusAction.Added: message = "The tailwind blew from behind {0}'s team!"; break;
                                case PBETeamStatusAction.Ended: message = "{0}'s team's tailwind petered out!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(tsp.TeamStatusAction));
                            }
                            break;
                        }
                        case PBETeamStatus.ToxicSpikes:
                        {
                            switch (tsp.TeamStatusAction)
                            {
                                case PBETeamStatusAction.Added: message = "Poison spikes were scattered all around {0}'s team's feet!"; break;
                                case PBETeamStatusAction.Cleared: message = "The poison spikes disappeared from around {0}'s team's feet!"; break;
                                default: throw new ArgumentOutOfRangeException(nameof(tsp.TeamStatusAction));
                            }
                            break;
                        }
                        case PBETeamStatus.WideGuard:
                        {
                            switch (tsp.TeamStatusAction)
                            {
                                case PBETeamStatusAction.Added: message = "Wide Guard protected {0}'s team!"; break;
                                case PBETeamStatusAction.Cleared: message = "{0}'s team's Wide Guard was destroyed!"; break;
                                case PBETeamStatusAction.Damage: message = "Wide Guard protected {1}!"; break;
                                case PBETeamStatusAction.Ended: return;
                                default: throw new ArgumentOutOfRangeException(nameof(tsp.TeamStatusAction));
                            }
                            break;
                        }
                        default: throw new ArgumentOutOfRangeException(nameof(tsp.TeamStatus));
                    }
                    Console.WriteLine(message, tsp.Team.TrainerName, NameForTrainer(damageVictim));
                    break;
                }
                case PBETypeChangedPacket tcp:
                {
                    PBEPokemon pokemon = tcp.PokemonTeam.TryGetPokemon(tcp.Pokemon);
                    PBEType type1 = tcp.Type1;
                    PBEType type2 = tcp.Type2;
                    string type1Str = PBELocalizedString.GetTypeName(type1).English;
                    Console.WriteLine("{0} transformed into the {1}", NameForTrainer(pokemon), type2 == PBEType.None ? $"{type1Str} type!" : $"{type1Str} and {PBELocalizedString.GetTypeName(type2).English} types!");
                    break;
                }
                case PBEWeatherPacket wp:
                {
                    PBEPokemon damageVictim = wp.DamageVictim.HasValue ? wp.DamageVictimTeam.TryGetPokemon(wp.DamageVictim.Value) : null;
                    string message;
                    switch (wp.Weather)
                    {
                        case PBEWeather.Hailstorm:
                        {
                            switch (wp.WeatherAction)
                            {
                                case PBEWeatherAction.Added: message = "It started to hail!"; break;
                                case PBEWeatherAction.CausedDamage: message = "{0} is buffeted by the hail!"; break;
                                case PBEWeatherAction.Ended: message = "The hail stopped."; break;
                                default: throw new ArgumentOutOfRangeException(nameof(wp.WeatherAction));
                            }
                            break;
                        }
                        case PBEWeather.HarshSunlight:
                        {
                            switch (wp.WeatherAction)
                            {
                                case PBEWeatherAction.Added: message = "The sunlight turned harsh!"; break;
                                case PBEWeatherAction.Ended: message = "The sunlight faded."; break;
                                default: throw new ArgumentOutOfRangeException(nameof(wp.WeatherAction));
                            }
                            break;
                        }
                        case PBEWeather.Rain:
                        {
                            switch (wp.WeatherAction)
                            {
                                case PBEWeatherAction.Added: message = "It started to rain!"; break;
                                case PBEWeatherAction.Ended: message = "The rain stopped."; break;
                                default: throw new ArgumentOutOfRangeException(nameof(wp.WeatherAction));
                            }
                            break;
                        }
                        case PBEWeather.Sandstorm:
                        {
                            switch (wp.WeatherAction)
                            {
                                case PBEWeatherAction.Added: message = "A sandstorm kicked up!"; break;
                                case PBEWeatherAction.CausedDamage: message = "{0} is buffeted by the sandstorm!"; break;
                                case PBEWeatherAction.Ended: message = "The sandstorm subsided."; break;
                                default: throw new ArgumentOutOfRangeException(nameof(wp.WeatherAction));
                            }
                            break;
                        }
                        default: throw new ArgumentOutOfRangeException(nameof(wp.Weather));
                    }
                    Console.WriteLine(message, NameForTrainer(damageVictim));
                    break;
                }
                case PBEActionsRequestPacket arp:
                {
                    Console.WriteLine("{0} must submit actions for {1} Pokémon.", arp.Team.TrainerName, arp.Pokemon.Count);
                    break;
                }
                case PBEAutoCenterPacket _:
                {
                    Console.WriteLine("The battlers shifted to the center!");
                    break;
                }
                case PBESwitchInRequestPacket sirp:
                {
                    Console.WriteLine("{0} must send in {1} Pokémon.", sirp.Team.TrainerName, sirp.Amount);
                    break;
                }
                case PBETurnBeganPacket tbp:
                {
                    Console.WriteLine("Turn {0} is starting.", tbp.TurnNumber);
                    break;
                }
                case PBEWinnerPacket win:
                {
                    Console.WriteLine("{0} defeated {1}!", win.WinningTeam.TrainerName, win.WinningTeam.OpposingTeam.TrainerName);
                    break;
                }
            }
        }
    }
}
