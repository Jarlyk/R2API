using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace R2API {
    [R2APISubmodule]
    public static class ResourcesAPI {
        private static readonly Dictionary<string, IResourcesProvider> Providers = new Dictionary<string, IResourcesProvider>();

        [R2APISubmoduleInit(Stage = InitStage.SetHooks)]
        internal static void InitHooks() {
            var methodLoad = typeof(Resources).GetMethod("Load", BindingFlags.Static | BindingFlags.Public, null,
                                                         new[] {typeof(string), typeof(Type)}, null);
            MonoMod.RuntimeDetour.HookGen.HookEndpointManager.Modify(methodLoad, (Action<ILContext>)OnResourcesLoad);

            var methodLoadAsync = typeof(Resources).GetMethod("LoadAsyncInternal", BindingFlags.Static | BindingFlags.NonPublic);
            MonoMod.RuntimeDetour.HookGen.HookEndpointManager.Modify(methodLoadAsync, (Action<ILContext>)OnResourcesLoadAsync);

            var methodLoadAll = typeof(Resources).GetMethod("LoadAll", BindingFlags.Static | BindingFlags.Public, null,
                                                            new[] {typeof(string), typeof(Type)}, null);
            MonoMod.RuntimeDetour.HookGen.HookEndpointManager.Modify(methodLoadAll, (Action<ILContext>)OnResourcesLoadAll);
        }

        public static void AddProvider(IResourcesProvider provider) {
            Providers.Add(provider.ModPrefix, provider);
        }

        private static void OnResourcesLoad(ILContext il) {
            var c = new ILCursor(il);
            var orig = c.DefineLabel();
            CheckForModPath(c, orig);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<string, Type, Object>>(ModResourcesLoad);
            c.Emit(OpCodes.Ret);
            c.MarkLabel(orig);
        }

        private static Object ModResourcesLoad(string path, Type type) {
            var split = path.Split(':');
            if (!Providers.TryGetValue(split[0], out var provider)) {
                throw new FormatException($"Modded resource paths must be of the form '@ModName:Path/To/Asset.ext'; provided path was '{path}'");
            }

            return provider.Load(path, type);
        }

        private static void OnResourcesLoadAsync(ILContext il) {
            var c = new ILCursor(il);
            var orig = c.DefineLabel();
            CheckForModPath(c, orig);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<string, Type, ResourceRequest>>(ModResourcesLoadAsync);
            c.Emit(OpCodes.Ret);
            c.MarkLabel(orig);
        }

        private static ResourceRequest ModResourcesLoadAsync(string path, Type type) {
            var split = path.Split(':');
            if (!Providers.TryGetValue(split[0], out var provider)) {
                throw new FormatException($"Modded resource paths must be of the form '@ModName:Path/To/Asset.ext'; provided path was '{path}'");
            }

            return provider.LoadAsync(path, type);
        }

        private static void OnResourcesLoadAll(ILContext il) {
            var c = new ILCursor(il);
            var orig = c.DefineLabel();
            CheckForModPath(c, orig);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<string, Type, Object[]>>(ModResourcesLoadAll);
            c.Emit(OpCodes.Ret);
            c.MarkLabel(orig);
        }

        private static Object[] ModResourcesLoadAll(string path, Type type) {
            var split = path.Split(':');
            if (!Providers.TryGetValue(split[0], out var provider)) {
                throw new FormatException($"Modded resource paths must be of the form '@ModName:Path/To/Asset.ext'; provided path was '{path}'");
            }

            return provider.LoadAll(type);
        }

        private static void CheckForModPath(ILCursor c, ILLabel orig)
        {
            //If null or empty, skip to regular handler
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Call, typeof(string).GetMethod("IsNullOrEmpty", BindingFlags.Static | BindingFlags.Public));
            c.Emit(OpCodes.Brtrue, orig);

            //If it doesn't start with '@', it's not a mod path
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldc_I4_0);
            c.Emit(OpCodes.Callvirt, typeof(string).GetProperty("Item").GetGetMethod());
            c.Emit(OpCodes.Ldc_I4, (int) '@');
            c.Emit(OpCodes.Bne_Un, orig);
        }
    }
}
