using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Attributes
{
    /// <summary>
    /// Live smokes of the round. Writes <c>insmoke</c> for any player prefix (player origin inside a smoke sphere)
    /// and the bomb keys <c>insmoke</c>, <c>smoke.distance</c>, <c>smoke.age</c> on the defuse events.
    /// The bomb origin is only looked up while a bomb key is loaded.
    /// </summary>
    internal sealed class SmokeProvider : IAttributeProvider
    {
        public static readonly AttributeSuffix[] Suffixes =
        [
            new AttributeSuffix("insmoke", "bool"),
        ];

        public static readonly AttributeEventKey[] Keys =
        [
            new AttributeEventKey("EventBombBegindefuse", "insmoke", "bool"),
            new AttributeEventKey("EventBombBegindefuse", "smoke.distance", "float"),
            new AttributeEventKey("EventBombBegindefuse", "smoke.age", "float"),
            new AttributeEventKey("EventBombDefused", "insmoke", "bool"),
            new AttributeEventKey("EventBombDefused", "smoke.distance", "float"),
            new AttributeEventKey("EventBombDefused", "smoke.age", "float"),
        ];

        private const string InSmokeKey = "insmoke";
        private const string DistanceKey = "smoke.distance";
        private const float SmokeRadiusSq = 144f * 144f;
        private const double SmokeExpireSeconds = 22.0;

        private static readonly string[] SmokeEvents = ["EventSmokegrenadeDetonate", "EventSmokegrenadeExpired"];
        private static readonly string[] BombEvents =
            ["EventSmokegrenadeDetonate", "EventSmokegrenadeExpired", "EventBombBegindefuse", "EventBombDefused"];

        private readonly List<LiveSmoke> _smokes = [];
        private string[] _watched = SmokeEvents;
        private float _bombX;
        private float _bombY;
        private float _bombZ;
        private bool _hasBombOrigin;

        public string Name => "smoke";
        public IReadOnlyList<AttributeSuffix> PlayerSuffixes => Suffixes;
        public IReadOnlyList<AttributeEventKey> EventKeys => Keys;
        public bool UsesRoster => false;
        public IReadOnlyCollection<string> WatchedEvents => _watched;

        public bool Configure(IReadOnlyList<ChallengeTask> tasks)
        {
            bool player = false;
            bool bomb = false;
            foreach (ChallengeTask task in tasks)
            {
                foreach (CompiledRule rule in task.CompiledRules)
                {
                    bomb |= AttributeKeys.IsEventKey(rule.Key, Keys);
                    player |= AttributeKeys.SuffixIndex(rule.Key, Suffixes) >= 0;
                }
            }

            _watched = bomb ? BombEvents : SmokeEvents;
            return player | bomb;
        }

        public void Observe(string eventName, GameEvent gameEvent, ProviderContext context)
        {
            switch (eventName)
            {
                case "EventSmokegrenadeDetonate":
                    EventSmokegrenadeDetonate detonate = (EventSmokegrenadeDetonate)gameEvent;
                    _smokes.Add(new LiveSmoke(detonate.Entityid, detonate.X, detonate.Y, detonate.Z, RoundClock.NowSeconds()));
                    return;
                case "EventSmokegrenadeExpired":
                    int entityId = ((EventSmokegrenadeExpired)gameEvent).Entityid;
                    for (int i = _smokes.Count - 1; i >= 0; i--)
                    {
                        if (_smokes[i].EntityId == entityId)
                        {
                            _smokes.RemoveAt(i);
                        }
                    }

                    return;
                case "EventBombBegindefuse":
                case "EventBombDefused":
                    TryCacheBombOrigin();
                    return;
            }
        }

        public void AfterEvent(string eventName, GameEvent gameEvent)
        {
        }

        public void WritePlayer(Dictionary<string, EventValue> d, string[] keys, uint suffixes, CCSPlayerController? player)
        {
            PruneSmokes(RoundClock.NowSeconds());
            ProbePlayer(player, out bool inside);
            d[keys[0]] = EventValue.Of(inside);
        }

        public void WriteEvent(string eventName, GameEvent gameEvent, Dictionary<string, EventValue> d, uint keys)
        {
            PruneSmokes(RoundClock.NowSeconds());
            TryCacheBombOrigin();
            ProbeBomb(out bool inside, out float distance, out float age);
            for (int i = 0; i < Keys.Length; i++)
            {
                if ((keys & (1u << i)) == 0)
                {
                    continue;
                }

                string key = Keys[i].Key;
                d[key] = key switch
                {
                    InSmokeKey => EventValue.Of(inside),
                    DistanceKey => EventValue.Of(distance),
                    _ => EventValue.Of(age),
                };
            }
        }

        public void Clear()
        {
            _smokes.Clear();
            _hasBombOrigin = false;
        }

        public void ForgetSlot(int slot)
        {
        }

        private void ProbeBomb(out bool inside, out float distance, out float age)
        {
            if (!_hasBombOrigin)
            {
                inside = false;
                distance = 9999f;
                age = 0f;
                return;
            }

            Probe(_bombX, _bombY, _bombZ, out inside, out distance, out age);
        }

        private void ProbePlayer(CCSPlayerController? player, out bool inside)
        {
            if (!TryOrigin(player, out float x, out float y, out float z))
            {
                inside = false;
                return;
            }

            Probe(x, y, z, out inside, out _, out _);
        }

        private void Probe(float x, float y, float z, out bool inside, out float distance, out float age)
        {
            if (_smokes.Count == 0)
            {
                inside = false;
                distance = 9999f;
                age = 0f;
                return;
            }

            double now = RoundClock.NowSeconds();
            float bestCoveringSq = float.MaxValue;
            float bestCoveringAge = 0f;
            float bestAnySq = float.MaxValue;
            float bestAnyAge = 0f;
            inside = false;

            for (int i = 0; i < _smokes.Count; i++)
            {
                LiveSmoke smoke = _smokes[i];
                float dx = x - smoke.X;
                float dy = y - smoke.Y;
                float dz = z - smoke.Z;
                float distSq = (dx * dx) + (dy * dy) + (dz * dz);
                float smokeAge = (float)Math.Max(0, now - smoke.At);

                if (distSq < bestAnySq)
                {
                    bestAnySq = distSq;
                    bestAnyAge = smokeAge;
                }

                if (distSq > SmokeRadiusSq || distSq >= bestCoveringSq)
                {
                    continue;
                }

                inside = true;
                bestCoveringSq = distSq;
                bestCoveringAge = smokeAge;
            }

            if (inside)
            {
                distance = MathF.Sqrt(bestCoveringSq);
                age = bestCoveringAge;
                return;
            }

            distance = MathF.Sqrt(bestAnySq);
            age = bestAnyAge;
        }

        private void TryCacheBombOrigin()
        {
            foreach (CPlantedC4 bomb in Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4"))
            {
                if (bomb is not { IsValid: true, AbsOrigin: { } origin })
                {
                    continue;
                }

                _bombX = origin.X;
                _bombY = origin.Y;
                _bombZ = origin.Z;
                _hasBombOrigin = true;
                return;
            }
        }

        private static bool TryOrigin(CCSPlayerController? player, out float x, out float y, out float z)
        {
            x = y = z = 0;
            CCSPlayerPawn? pawn = player is { IsValid: true } ? player.PlayerPawn?.Value : null;
            if (pawn is not { IsValid: true, AbsOrigin: { } origin })
            {
                return false;
            }

            x = origin.X;
            y = origin.Y;
            z = origin.Z;
            return true;
        }

        private void PruneSmokes(double now)
        {
            for (int i = _smokes.Count - 1; i >= 0; i--)
            {
                if (now - _smokes[i].At > SmokeExpireSeconds)
                {
                    _smokes.RemoveAt(i);
                }
            }
        }

        private readonly struct LiveSmoke(int entityId, float x, float y, float z, double at)
        {
            public int EntityId { get; } = entityId;
            public float X { get; } = x;
            public float Y { get; } = y;
            public float Z { get; } = z;
            public double At { get; } = at;
        }
    }
}
