﻿using Kermalis.PokemonBattleEngine.Battle;
using Kermalis.PokemonBattleEngine.Data;
using Kermalis.PokemonBattleEngineClient.Infrastructure;
using ReactiveUI;
using System;
using System.Reactive;

namespace Kermalis.PokemonBattleEngineClient.Models
{
    public sealed class PokemonInfo
    {
        public ReactiveCommand<Unit, Unit> SelectPokemonCommand { get; }
        public bool Enabled { get; }

        public PBEBattlePokemon Pokemon { get; }
        public string Description => Utils.CustomPokemonToString(Pokemon, false);

        public PokemonInfo(PBEBattlePokemon pkmn, bool locked, Action<PBEBattlePokemon> clickAction)
        {
            Pokemon = pkmn;
            Enabled = !locked && pkmn.FieldPosition == PBEFieldPosition.None && pkmn.HP > 0;
            SelectPokemonCommand = ReactiveCommand.Create(() => clickAction(pkmn));
        }
    }
}
