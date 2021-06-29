# Evolution.IOC
DotNet standard IOC lightweight framework.

# Support feature:
1. Support dependency injection
2. Support life cycle control, thread safety
   2.1 Process level shared instance objects
   2.2 Thread level shared instance objects
   2.3 Create new objects every time
   2.4 Custom period
3. Support configuration file and configuration interface extension
4. Support multiple injection modes:
   4.1 Constructor injection (conventional injection method)
   4.2 Method injection (not recommended, and the principle of constructor injection is the same, but there is one more useless method)
   4.3 Property injection (recommended)
   4.4 Intelligent mode injection (dumb): no need for feature attachment
   4.5 Compatibility mode injection (recommended): Scan all attached features: constructor/property/method
   
# Code Simple:
            //1.Support dependency injection
            using (IContainer container = IOCContainer.Instance)
            {
                Console.WriteLine("Simple case:");
                IRegisterInfo service = container.AddService<IMyService, MyService>();
                var instance = container.Resolve<IMyService>();
                instance.Run();
                Console.WriteLine("\n");
            }
            //2.Support life cycle control, thread safety
            using (IContainer container = IOCContainer.Instance)
            {
                Console.WriteLine("Lifetime AddSingleton:");
                //	2.1 Share instance in process
                IRegisterInfo service = container.AddSingleton<IMyService, MyService>();
                TestMultiThread(container);
                Console.WriteLine("\n");
            }
            using (IContainer container = IOCContainer.Instance)
            {
                Console.WriteLine("Lifetime AddScoped:");
                //	2.2 Share instance in one process
                IRegisterInfo service = container.AddScoped<IMyService, MyService>();
                TestMultiThread(container);
                Console.WriteLine("\n");
            }
            using (IContainer container = IOCContainer.Instance)
            {
                Console.WriteLine("Lifetime AddTransient:");
                //	2.3 Create new object each time
                IRegisterInfo service = container.AddTransient<IMyService, MyService>();
                TestMultiThread(container);
                Console.WriteLine("\n");
            }
            using (IContainer container = IOCContainer.Instance)
            {
                //	2.4 Custom lifetime cycle
                Console.WriteLine("Lifetime custom(this is Transient):");
                IRegisterInfo service = container.AddService<IMyService, MyService>(lifeTime: LifeTime.Transient);
                TestMultiThread(container);
                Console.WriteLine("\n");
            }
            //3.Support configuration file and configuration interface extension
            using (IContainer container = IOCContainer.Instance)
            {
                //	3.1 Xml，
                Console.WriteLine("IOC config for xml.");
                container.SyncXmlConfig(xmlPath);
                TestMultiThread(container);
                Console.WriteLine("\n");
            }
            using (IContainer container = IOCContainer.Instance)
            {
                //	3.2 json， 
                Console.WriteLine("IOC config for json.");
                container.SyncJsonConfig(jsonPath);
                TestMultiThread(container);
                Console.WriteLine("\n");
            }
            using (IContainer container = IOCContainer.Instance)
            {
                //	3.3 提供接口，以支持自定义扩展
                Console.WriteLine("IOC config for custom.");
                IBaseServiceProvider serviceProvider = new DefaultXmlServiceProvider(new FileInfo(xmlPath));
                container.Sync(serviceProvider);
                TestMultiThread(container);
                Console.WriteLine("\n");
            }
            //4.Support multiple injection modes:
            using (IContainer container = IOCContainer.Instance)
            {
                Console.WriteLine("InjectionWay Constructor:");
                //	4.1 Constructor injection (conventional injection method)
                var service1 = container.AddScoped<IMyService, MyService_Constructor>();
                service1.UseInjectionWay(() => InjectionWay.Constructor);
                container.AddTransient<IMySingleTonService, MySingleTonService>();

                var instance = container.Resolve<IMyService>();
                instance.Run();
                Console.WriteLine("\n");
            }
            using (IContainer container = IOCContainer.Instance)
            {
                Console.WriteLine("InjectionWay Method:");
                //	4.2 Method injection (not recommended, and the principle of constructor injection is the same, but there is one more useless method)
                IRegisterInfo service = container.AddScoped<IMyService, MyService_Method>();
                service.UseInjectionWay(() => InjectionWay.Method);
                container.AddTransient<IMySingleTonService, MySingleTonService>();

                var instance = container.Resolve<IMyService>();
                instance.Run();
                Console.WriteLine("\n");
            }
            using (IContainer container = IOCContainer.Instance)
            {
                Console.WriteLine("InjectionWay Property:");
                //	4.3 Property injection (recommended)
                IRegisterInfo service = container.AddScoped<IMyService, MyService_Property>();
                service.UseInjectionWay(() => InjectionWay.Property);
                container.AddTransient<IMySingleTonService, MySingleTonService>();

                var instance = container.Resolve<IMyService>();
                instance.Run();
                Console.WriteLine("\n");
            }
            using (IContainer container = IOCContainer.Instance)
            {
                Console.WriteLine("InjectionWay Smart:");
                //	4.4 Intelligent mode injection (dumb): no need for feature attachment
                IRegisterInfo service = container.AddScoped<IMyService, MyService_Smart>();
                service.UseInjectionWay(() => InjectionWay.Smart);
                container.AddTransient<IMySingleTonService, MySingleTonService>();

                var instance = container.Resolve<IMyService>();
                instance.Run();
                Console.WriteLine("\n");
            }
            using (IContainer container = IOCContainer.Instance)
            {
                Console.WriteLine("InjectionWay All:");
                //	4.5 Compatibility mode injection (recommended): Scan all attached features: constructor/property/method
                IRegisterInfo service = container.AddScoped<IMyService, MyService_CoverAll>();
                service.UseInjectionWay(() => InjectionWay.All);
                container.AddTransient<IMySingleTonService, MySingleTonService>();

                var instance = container.Resolve<IMyService>();
                instance.Run();
            }
