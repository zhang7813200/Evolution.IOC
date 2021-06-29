using System;
using System.Collections.Generic;
using System.Text;

namespace Evolution.IOC
{
    public interface ISyncConfig
    {
        void SyncXmlConfig(string xmlConfigLocalPath);
        void SyncJsonConfig(string jsonConfigLocalPath);
        void Sync(IBaseServiceProvider serviceProvider);
    }

    public interface IContainer: ISyncConfig, IDisposable
    {
        IRegisterInfo AddService<TInterface, TImplement>(LifeTime lifeTime = LifeTime.Transient) where TInterface : class where TImplement : class, TInterface;
        IRegisterInfo AddSingleton<TInterface, TImplement>() where TInterface : class where TImplement : class, TInterface;
        IRegisterInfo AddScoped<TInterface, TImplement>() where TInterface : class where TImplement : class, TInterface;
        IRegisterInfo AddTransient<TInterface, TImplement>() where TInterface : class where TImplement : class, TInterface;
        TInterface Resolve<TInterface>() where TInterface : class;
    }
}
