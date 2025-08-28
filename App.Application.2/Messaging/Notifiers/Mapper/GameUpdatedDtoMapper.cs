using App.Domain._2.Competition;
using App.Domain._2.Game;
using JumperId = App.Domain._2.Game.JumperId;

namespace App.Application._2.Messaging.Notifiers.Mapper;

using System.Collections.Generic;
using System.Linq;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Reflection;

public static class GameUpdatedDtoMapper
    {
        private const int SchemaVersion = 1;

        // ---------- Helpers: unwrap F# single-case VOs ----------
        private static Guid Unwrap(GameId x) => x.Item;
        private static Guid Unwrap(PlayerId x) => x.Item;
        private static Guid Unwrap(JumperId x) => x.Item;
        private static Guid Unwrap(App.Domain._2.Competition.JumperId x) => x.Item;
        private static Guid Unwrap(App.Domain._2.Competition.HillId x) => x.Item;

        // ---------- Helpers: F# Option ----------
        private static T? ToNullable<T>(FSharpOption<T> opt) where T : struct
            => FSharpOption<T>.get_IsSome(opt) ? opt.Value : null;

        private static T? ToRefOrNull<T>(FSharpOption<T> opt) where T : class
            => FSharpOption<T>.get_IsSome(opt) ? opt.Value : null;

        // ---------- Helpers: F# Union reflection ----------
        private static (string Case, object[] Fields) DeconstructUnion(object union)
        {
            var t = union.GetType();
            var (uci, fields) = (
                FSharpValue.GetUnionFields(union, t,null).Item1,
                FSharpValue.GetUnionFields(union, t, null).Item2
            );
            return (uci.Name, fields);
        }

        // ---------- Public API ----------
        public static GameUpdatedDto FromDomain(App.Domain._2.Game.Game game, string changeType = "Snapshot")
        {
            var header = MapHeader(game);
            var (statusStr, preDraft, draft, mainComp, brk, ended) = MapStatus(game);

            return new GameUpdatedDto(
                Unwrap(game.Id_),
                SchemaVersion,
                statusStr,
                changeType,
                header,
                preDraft,
                draft,
                mainComp,
                brk,
                ended
            );
        }

        // ---------- Header ----------
        private static GameHeaderDto MapHeader(App.Domain._2.Game.Game game)
        {
            var players =
                PlayersModule.toList(game.Players)
                .Select(p => new PlayerDto(Unwrap(p.Id), PlayerModule.NickModule.value(p.Nick)))
                .ToList();

            var jumpers =
                JumpersModule.toList(game.Jumpers)
                .Select(j => new JumperDto(Unwrap(j.Id)))
                .ToList();

            Guid? hillId =
                game.Hill.Match(
                    some: hill => Unwrap(hill.Id),
                    none: () => (Guid?)null
                );

            return new GameHeaderDto(hillId, players, jumpers);
        }

        // ---------- Status dispatcher ----------
        private static (string, PreDraftDto?, DraftDto?, CompetitionDto?, BreakDto?, EndedDto?)
            MapStatus(App.Domain._2.Game.Game game)
        {
            var (caseName, fields) = DeconstructUnion(game.Status);

            switch (caseName)
            {
                case "PreDraft":
                {
                    var pre = (App.Domain._2.Game.PreDraftStatus)fields[0];
                    return ("PreDraft", MapPreDraft(pre), null, null, null, null);
                }

                case "Draft":
                {
                    var draft = (App.Domain._2.Game.Draft)fields[0];
                    return ("Draft", null, MapDraft(game, draft), null, null, null);
                }

                case "MainCompetition":
                {
                    var comp = (Competition)fields[0];
                    return ("MainCompetition", null, null, MapCompetitionLight(comp), null, null);
                }

                case "Ended":
                {
                    // W bieżącej wersji Game nie przechowuje rankingu do DTO — tylko polityka
                    return ("Ended", null, null, null, null, MapEnded(game));
                }

                case "Break":
                {
                    var nextTag = (App.Domain._2.Game.StatusTag)fields[0];
                    return ("Break", null, null, null, MapBreak(nextTag), null);
                }

                default:
                    throw new InvalidOperationException($"Unknown Game.Status case '{caseName}'.");
            }
        }

        // ---------- PreDraft ----------
        private static PreDraftDto MapPreDraft(App.Domain._2.Game.PreDraftStatus pre)
        {
            var (caseName, fields) = DeconstructUnion(pre);

            if (caseName == "Running")
            {
                // Running(Index: PreDraftCompetitionIndex * Competition: Competition.Competition)
                var indexVo = (App.Domain._2.Game.PreDraftCompetitionIndex)fields[0];
                var comp = (Competition)fields[1];
                var idx = PreDraftCompetitionIndexModule.value(indexVo);
                return new PreDraftDto("Running", idx, MapCompetitionLight(comp));
            }

            if (caseName == "Break")
            {
                var indexVo = (App.Domain._2.Game.PreDraftCompetitionIndex)fields[0];
                var idx = PreDraftCompetitionIndexModule.value(indexVo);
                return new PreDraftDto("Break", idx, null);
            }

            throw new InvalidOperationException($"Unknown PreDraftStatus case '{caseName}'.");
        }

        // ---------- Draft ----------
        private static DraftDto MapDraft(App.Domain._2.Game.Game game, App.Domain._2.Game.Draft draft)
        {
            Guid? currentPlayerId =
                draft.CurrentPlayer.Match(
                    some: p => Unwrap(p),
                    none: () => (Guid?)null
                );

            var players = PlayersModule.toList(game.Players);

            var picks = players
                .Select(pl =>
                {
                    var picked =
                        draft.PicksOf(pl.Id)
                             .Match(
                                 some: lst => lst.Select(Unwrap).ToList(),
                                 none:  () => new List<Guid>()
                             )
                             .AsReadOnly();

                    return new PlayerPicksDto(Unwrap(pl.Id), picked);
                })
                .ToList()
                .AsReadOnly();

            return new DraftDto(currentPlayerId, draft.Ended, picks);
        }

        // ---------- Competition (light) ----------
        private static CompetitionDto MapCompetitionLight(Competition comp)
        {
            var status = comp.GetStatusTag switch
            {
                var v when v.Equals(CompetitionModule.StatusTag.NotStartedTag)      => "NotStarted",
                var v when v.Equals(CompetitionModule.StatusTag.RoundInProgressTag) => "RoundInProgress",
                var v when v.Equals(CompetitionModule.StatusTag.SuspendedTag)       => "Suspended",
                var v when v.Equals(CompetitionModule.StatusTag.CancelledTag)       => "Cancelled",
                var v when v.Equals(CompetitionModule.StatusTag.EndedTag)           => "Ended",
                _ => "Unknown"
            };

            Guid? nextJumperId =
                comp.NextJumper.Match(
                    some: j => Unwrap(j.Id),
                    none: () => (Guid?)null
                );

            var gate = MapGate(comp.GateState);
            return new CompetitionDto(status, nextJumperId, gate);
        }

        private static GateDto MapGate(FSharpOption<GateState> gsOpt)
        {
            if (!FSharpOption<GateState>.get_IsSome(gsOpt))
                return new GateDto(0, 0, null);

            var gs = gsOpt.Value;

            var starting = GateModule.value(gs.Starting);
            var current  = GateModule.value(gs.CurrentJury);

            var coachReduction = gs.CoachChange.Match(
                some: ch =>
                {
                    var (chCase, chFields) = DeconstructUnion(ch);
                    return chCase switch
                    {
                        "Reduction" => (int?)(int)(uint)chFields[0],
                        _           => null
                    };
                },
                none: () => (int?)null
            );

            return new GateDto(starting, current, coachReduction);
        }

        // ---------- Break / Ended ----------
        private static BreakDto MapBreak(App.Domain._2.Game.StatusTag nextTag)
        {
            var next = nextTag switch
            {
                var v when v.IsPreDraftTag        => "PreDraft",
                var v when v.IsDraftTag           => "Draft",
                var v when v.IsMainCompetitionTag => "MainCompetition",
                var v when v.IsEndedTag           => "Ended",
                _                                 => "Unknown"
            };

            return new BreakDto(next);
        }

        private static EndedDto MapEnded(App.Domain._2.Game.Game game)
        {
            var policy = game.Settings.RankingPolicy switch
            {
                var v when v.Equals(Domain._2.Game.RankingPolicy.Classic)=> "Classic",
                var v when v.Equals(Domain._2.Game.RankingPolicy.PodiumAtAllCosts)=> "PodiumAtAllCosts",
                _ => "Classic"
            };

            return new EndedDto(policy);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Little Option helpers (ergonomic Match over F# Option)
    // ─────────────────────────────────────────────────────────────────────────────
    internal static class FSharpOptionExt
    {
        public static TOut Match<T, TOut>(
            this FSharpOption<T> opt,
            Func<T, TOut> 
                some,
            Func<TOut> none)
            => FSharpOption<T>.get_IsSome(opt) ? some(opt.Value) : none();
    }