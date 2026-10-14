using System.Reflection;

namespace Challenges.Tests
{
    /// <summary>
    /// <see cref="PlayerState"/> objects live in the archive for the plugin's life, across map changes, so they must
    /// not hold objects of a past map (C2.1).
    /// </summary>
    public sealed class PlayerStateTests
    {
        private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Type[] MapScoped = [typeof(RunningSchedule), typeof(ChallengeDefinition), typeof(ChallengeTask)];

        [Fact]
        public void Tracker_keeps_the_schedule_key_only()
        {
            FieldInfo key = typeof(PlayerState).GetField(nameof(PlayerState.TrackerScheduleKey))!;
            Assert.Equal(typeof(string), key.FieldType);
            Assert.Empty(MapScopedFields(typeof(PlayerState), []));
        }

        /// <summary>Fields of <paramref name="type"/> and the plugin types it holds that can reach a map-scoped object.</summary>
        private static List<string> MapScopedFields(Type type, HashSet<Type> seen)
        {
            List<string> found = [];
            if (!seen.Add(type))
            {
                return found;
            }

            foreach (FieldInfo field in type.GetFields(InstanceFields))
            {
                foreach (Type held in Held(field.FieldType))
                {
                    if (Array.Exists(MapScoped, scoped => scoped.IsAssignableFrom(held)) || held == typeof(object))
                    {
                        found.Add($"{type.Name}.{field.Name}");
                    }
                    else if (held.Assembly == typeof(PlayerState).Assembly && !held.IsEnum)
                    {
                        found.AddRange(MapScopedFields(held, seen));
                    }
                }
            }

            return found;
        }

        /// <summary>The type itself plus element and generic argument types, recursively.</summary>
        private static IEnumerable<Type> Held(Type type)
        {
            yield return type;
            if (type.GetElementType() is { } element)
            {
                foreach (Type inner in Held(element))
                {
                    yield return inner;
                }
            }

            foreach (Type argument in type.IsGenericType ? type.GetGenericArguments() : [])
            {
                foreach (Type inner in Held(argument))
                {
                    yield return inner;
                }
            }
        }
    }
}
