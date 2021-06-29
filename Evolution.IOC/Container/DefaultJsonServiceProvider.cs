using Evolution.IOC.Exception;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace Evolution.IOC
{
    public class IOCJsonConfig
    {
        public TopNode Evolution { get; set; }
    }

    public class TopNode
    {
        public IOC IOC { get; set; }   
    }

    public class IOC
    {
        public List<ServiceJsonObj> Services { get; set; }
    }

    public class ServiceJsonObj
    {
        public int InjectionWay { get; set; }
        public int LifeTime { get; set; }
        public string ImplementClassType { get; set; }
        public string InterfaceType { get; set; }
        public object[] ParameterValues { get; set; }
    }

    public class DefaultJsonServiceProvider : IBaseServiceProvider
    {
        private readonly IEnumerable<IRegisterInfo> metaDataJson;
        public DefaultJsonServiceProvider(FileInfo jsonLocalPath) : this(ValidAndGetStream(jsonLocalPath)) { }
        public DefaultJsonServiceProvider(Stream jsonStream) : this(ValidAndGetJson(jsonStream)) { }
        public DefaultJsonServiceProvider(string json)
        {
            if (string.IsNullOrEmpty(json)) throw new ArgumentNullException("steam is null");
            var buffer = Encoding.UTF8.GetBytes(json);
            var jsonConfig = SerializerHelper.Deserialize<IOCJsonConfig>(json);
            metaDataJson =  ConvertToRegisterInfos(jsonConfig.Evolution.IOC.Services);
        }

        private IEnumerable<IRegisterInfo> ConvertToRegisterInfos(List<ServiceJsonObj> services)
        {
            foreach (var service in services)
            {
                yield return new ImplementInstanceInfo()
                {
                    ImplementClassType = GetRealType(service.ImplementClassType),
                    InterfaceType = GetRealType(service.InterfaceType),
                    InjectionWay = (InjectionWay)service.InjectionWay,
                    LifeTime = (LifeTime)service.LifeTime,
                };
            }
        }

        private static Stream ValidAndGetStream(FileInfo jsonLocalPath)
        {
            if (jsonLocalPath == null) throw new ArgumentNullException("xmlLocalPath is null");
            if (!jsonLocalPath.Exists) throw new FileNotFoundException();
            if (jsonLocalPath.IsReadOnly) throw new Evolution.IOC.Exception.InvalidDataException("This fileInfo is read only.");
            return jsonLocalPath.OpenRead();
        }

        private static string ValidAndGetJson(Stream jsonStream)
        {
            if (jsonStream == null) throw new ArgumentNullException("steam is null");
            jsonStream.Position = 0;
            using (StreamReader reader = new StreamReader(jsonStream))
            {
                string json = reader.ReadToEnd();
                return json;
            }
        }

        private List<IRegisterInfo> ValidMetaData(Func<List<IRegisterInfo>> validAction)
        {
            List<IRegisterInfo> json = null;
            try
            {
                json = validAction();
            }
            catch (System.Exception e)
            {
                throw new Evolution.IOC.Exception.InvalidDataException($"Xml content is invalid", e);
            }
            if (json==null) throw new NullResultException();
            return json;
        }

        private readonly string XPath_ServicesNode = "/Evolution/IOC/Services";

        public override IRegisterInfo GetService(string interfaceFullName)
        {
            return GetServices().Where(it => string.Equals(it.InterfaceType.FullName, interfaceFullName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }

        public override IEnumerable<IRegisterInfo> GetServices()
        {
            if (metaDataJson == null) throw new NullResultException("Current instance doesn't init metadata with json format.");
            return metaDataJson;
            //var properties = typeof(IRegisterInfo).GetProperties();
            //var nodes = metaData.SelectSingleNode(XPath_ServicesNode);
            //foreach (XmlNode node in nodes)
            //{
            //    IRegisterInfo registerInfo = Activator.CreateInstance(typeof(ImplementInstanceInfo)) as IRegisterInfo;
            //    foreach (var property in properties)
            //    {
            //        XmlAttribute attribute = node.Attributes[property.Name];
            //        if (attribute == null) continue;
            //        object attributeValue = null;
            //        //System.Enum set value
            //        if (property.PropertyType.IsEnum)
            //        {
            //            attributeValue = Enum.Parse(property.PropertyType, attribute.Value);
            //        }
            //        //System.Type set value
            //        else if (property.PropertyType.IsClass && string.Equals(property.PropertyType.FullName, typeof(System.Type).FullName))
            //        {
            //            //找到该实现的type
            //            attributeValue = base.GetRealType(attribute.Value);
            //        }
            //        else
            //        {
            //            attributeValue = Convert.ChangeType(attribute.Value, property.PropertyType);
            //        }
            //        registerInfo.GetType().GetProperty(property.Name).SetValue(registerInfo, attributeValue, null);
            //    }
            //    yield return registerInfo;
            //}

        }

        private static IEnumerable<string> GetPropertyNames()
        {
            var properties = typeof(IRegisterInfo).GetProperties();
            foreach (var property in properties)
            {
                yield return property.Name;
            }
        }
    }
}
