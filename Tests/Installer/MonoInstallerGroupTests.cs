using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Calluna.DI.Tests
{
    public class MonoInstallerGroupTests
    {
        private GameObject _go;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        // ── injection ordering ───────────────────────────────────────────────────

        [Test]
        public void MonoInstallerGroup_InstallBindings_InjectsSubInstaller_BeforeCallingInstallBindings()
        {
            MonoInstallerGroup group = _go.AddComponent<MonoInstallerGroup>();
            SpyInstaller spy = _go.AddComponent<SpyInstaller>();
            AddToGroup(group, spy);

            ((Injectable)group).Inject(new BasicInstanceResolver());
            group.InstallBindings(new TestBinder());

            Assert.IsTrue(spy.WasInjectedBeforeInstallBindings,
                "sub-installer must be injected before its InstallBindings is called");
        }

        [Test]
        public void MonoInstallerGroup_InstallBindings_PassesGroupResolver_ToSubInstaller()
        {
            MonoInstallerGroup group = _go.AddComponent<MonoInstallerGroup>();
            SpyInstaller spy = _go.AddComponent<SpyInstaller>();
            AddToGroup(group, spy);

            BasicInstanceResolver resolver = new BasicInstanceResolver();
            ((Injectable)group).Inject(resolver);
            group.InstallBindings(new TestBinder());

            Assert.AreSame(resolver, spy.InjectedResolver,
                "sub-installer must receive the same resolver that was injected into the group");
        }

        // ── InstallBindings forwarding ───────────────────────────────────────────

        [Test]
        public void MonoInstallerGroup_InstallBindings_CallsInstallBindings_OnSubInstaller()
        {
            MonoInstallerGroup group = _go.AddComponent<MonoInstallerGroup>();
            SpyInstaller spy = _go.AddComponent<SpyInstaller>();
            AddToGroup(group, spy);

            ((Injectable)group).Inject(new BasicInstanceResolver());
            group.InstallBindings(new TestBinder());

            Assert.IsTrue(spy.WasInstallBindingsCalled);
        }

        [Test]
        public void MonoInstallerGroup_InstallBindings_CallsInstallBindings_OnAllSubInstallers()
        {
            MonoInstallerGroup group = _go.AddComponent<MonoInstallerGroup>();
            SpyInstaller spy1 = _go.AddComponent<SpyInstaller>();
            SpyInstaller spy2 = _go.AddComponent<SpyInstaller>();
            AddToGroup(group, spy1);
            AddToGroup(group, spy2);

            ((Injectable)group).Inject(new BasicInstanceResolver());
            group.InstallBindings(new TestBinder());

            Assert.IsTrue(spy1.WasInstallBindingsCalled, "first sub-installer must have InstallBindings called");
            Assert.IsTrue(spy2.WasInstallBindingsCalled, "second sub-installer must have InstallBindings called");
        }

        // ── helper ───────────────────────────────────────────────────────────────

        private static void AddToGroup(MonoInstallerGroup group, MonoInstaller installer)
        {
            FieldInfo field = typeof(MonoInstallerGroup).GetField(
                "_installers", BindingFlags.NonPublic | BindingFlags.Instance);
            var list = (List<MonoInstaller>)field.GetValue(group);
            list.Add(installer);
        }
    }
}
