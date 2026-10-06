using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.UserMessages;
using Challenges.Classes;
using System.Globalization;
using System.Reflection;

namespace Challenges.Utils
{
    public static class DynamicHandlers
    {
        private const BindingFlags InstancePublic = BindingFlags.Public | BindingFlags.Instance;

        private static readonly MethodInfo? RegisterListenerApi = FindApi("RegisterListener", 1, generic: true);
        private static readonly MethodInfo? RemoveListenerApi = FindApi("RemoveListener", 1, generic: true);
        private static readonly MethodInfo? RegisterEventApi = FindApi("RegisterEventHandler", 2, generic: true);
        private static readonly MethodInfo? DeregisterEventApi = FindApi("DeregisterEventHandler", 2, generic: true);
        private static readonly MethodInfo? HookUserMessageApi = FindApi("HookUserMessage", 3);
        private static readonly MethodInfo? UnhookUserMessageApi = FindApi("UnhookUserMessage", 3);
        private static readonly MethodInfo? AddCommandApi = FindApi("AddCommand", 3);
        private static readonly MethodInfo? RemoveCommandApi = FindApi("RemoveCommand", 2);
        private static readonly MethodInfo? AddCommandListenerApi = FindApi("AddCommandListener", 3);
        private static readonly MethodInfo? RemoveCommandListenerApi = FindApi("RemoveCommandListener", 3);

        public static void BindModuleListener(BasePlugin basePlugin, string listenerName, ClassesBlueprint module, bool register)
        {
            Type? listenerType = typeof(Listeners).GetNestedType(listenerName);
            MethodInfo? method = module.GetType().GetMethod(listenerName);
            MethodInfo? api = register ? RegisterListenerApi : RemoveListenerApi;
            if (listenerType == null || method == null || api == null)
            {
                return;
            }

            Delegate handler = Delegate.CreateDelegate(listenerType, module, method);
            _ = api.MakeGenericMethod(listenerType).Invoke(basePlugin, [handler]);
        }

        public static void BindModuleEventHandler(BasePlugin basePlugin, string eventName, ClassesBlueprint module, bool register)
        {
            Type? eventType = typeof(BasePlugin).Assembly.GetType($"CounterStrikeSharp.API.Core.{eventName}")
                ?? typeof(GameEvent).Assembly.GetType($"CounterStrikeSharp.API.Modules.Events.{eventName}");
            MethodInfo? method = module.GetType().GetMethod(eventName);
            MethodInfo? api = register ? RegisterEventApi : DeregisterEventApi;
            if (eventType == null || method == null || api == null)
            {
                return;
            }

            Type handlerType = typeof(BasePlugin).GetNestedType("GameEventHandler`1")!.MakeGenericType(eventType);
            Delegate handler = Delegate.CreateDelegate(handlerType, module, method);
            _ = api.MakeGenericMethod(eventType).Invoke(basePlugin, [handler, HookMode.Pre]);
        }

        public static void BindUserMessageHook(BasePlugin basePlugin, int messageId, ClassesBlueprint module, HookMode hookMode, bool register) =>
            BindUserMessage(basePlugin, messageId, $"HookUserMessage{messageId}", module, hookMode, register);

        public static void BindNamedUserMessageHook(BasePlugin basePlugin, string messageName, ClassesBlueprint module, HookMode hookMode, bool register)
        {
            int messageId;
            try
            {
                messageId = UserMessage.FindIdByName(messageName);
            }
            catch (NativeException)
            {
                // Cold Load() runs before the network-message table exists.
                return;
            }

            if (messageId < 0)
            {
                return;
            }

            BindUserMessage(
                basePlugin,
                messageId,
                $"HookUserMessage{ToMethodSuffix(messageName)}",
                module,
                hookMode,
                register);
        }

        public static void BindCommand(
            BasePlugin basePlugin,
            string command,
            string? description,
            string methodName,
            ClassesBlueprint module,
            bool register)
        {
            MethodInfo? method = RequireMethod(module, methodName, register, "command");
            MethodInfo? api = register ? AddCommandApi : RemoveCommandApi;
            if (method == null || api == null)
            {
                if (register && api == null)
                {
                    Console.WriteLine("[DynamicHandlers] AddCommand method not found.");
                }
                return;
            }

            // AddCommand(name, description, callback) vs RemoveCommand(name, callback)
            int handlerIndex = register ? 2 : 1;
            Delegate handler = Delegate.CreateDelegate(api.GetParameters()[handlerIndex].ParameterType, module, method);
            object?[] args = register ? [command, description!, handler] : [command, handler];
            _ = api.Invoke(basePlugin, args);
        }

        public static void BindCommandListener(BasePlugin basePlugin, string command, ClassesBlueprint module, HookMode hookMode, bool register)
        {
            MethodInfo? method = RequireMethod(module, $"CommandListener{ToMethodSuffix(command)}", register, "command listener");
            MethodInfo? api = register ? AddCommandListenerApi : RemoveCommandListenerApi;
            if (method == null || api == null)
            {
                if (register && api == null)
                {
                    Console.WriteLine("[DynamicHandlers] AddCommandListener method not found.");
                }
                return;
            }

            Delegate handler = Delegate.CreateDelegate(api.GetParameters()[1].ParameterType, module, method);
            _ = api.Invoke(basePlugin, [command, handler, hookMode]);
        }

        private static void BindUserMessage(
            BasePlugin basePlugin,
            int messageId,
            string methodName,
            ClassesBlueprint module,
            HookMode hookMode,
            bool register)
        {
            MethodInfo? method = RequireMethod(module, methodName, register, "UserMessage");
            MethodInfo? api = register ? HookUserMessageApi : UnhookUserMessageApi;
            if (method == null || api == null)
            {
                if (register && api == null)
                {
                    Console.WriteLine("[DynamicHandlers] HookUserMessage method not found.");
                }
                return;
            }

            Delegate handler = Delegate.CreateDelegate(typeof(UserMessage.UserMessageHandler), module, method);
            _ = api.Invoke(basePlugin, [messageId, handler, hookMode]);
        }

        private static MethodInfo? RequireMethod(ClassesBlueprint module, string methodName, bool register, string label)
        {
            MethodInfo? method = module.GetType().GetMethod(methodName);
            if (method == null && register)
            {
                Console.WriteLine($"[DynamicHandlers] Method not found for {label}: {methodName}");
            }
            return method;
        }

        private static MethodInfo? FindApi(string name, int parameterCount, bool generic = false) =>
            typeof(BasePlugin).GetMethods(InstancePublic)
                .FirstOrDefault(m => m.Name == name
                    && m.IsGenericMethodDefinition == generic
                    && m.GetParameters().Length == parameterCount);

        private static string ToMethodSuffix(string name) =>
            name.Length == 0
                ? name
                : char.ToUpper(name[0], CultureInfo.InvariantCulture) + name[1..];
    }
}
