using System.Collections.Generic;

namespace Calluna.DI.Tests
{
    // Own file, so Unity finds its script. Binds a QuitLogService named after the installer.
    public class QuitLogInstaller : MonoInstaller
    {
        public List<string> Log;
        public string Name;

        public override void InstallBindings(Binder binder)
        {
            binder.BindToNewSelf<QuitLogService>()
                .WithArgument(Log)
                .WithArgument(Name)
                .AsSingle()
                .NonLazy();
        }
    }

    public class QuitLogService : Injectable, QuitHandler, Cleanable
    {
        private List<string> _log;
        private string _name;

        public void Inject(Resolver resolver)
        {
            _log = resolver.Resolve<List<string>>();
            _name = resolver.Resolve<string>();
        }

        public void HandleQuit() => _log.Add($"quit {_name}");

        public void Clean() => _log.Add($"clean {_name}");
    }
}
