using Evolution.IOC.Exception;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Evolution.IOC
{
    public class DefaultXmlServiceProvider : IBaseServiceProvider
    {
        private readonly XmlDocument metaData = new XmlDocument();
        public DefaultXmlServiceProvider(Stream xmlStream)
        {
            if (xmlStream == null) throw new ArgumentNullException("steam is null");
            xmlStream.Position = 0;
            using (StreamReader reader = new StreamReader(xmlStream))
            {
                ValidMetaData(() => metaData.LoadXml(reader.ReadToEnd()));
            }
        }

        public DefaultXmlServiceProvider(string xml)
        {
            if (string.IsNullOrEmpty(xml)) throw new ArgumentNullException("steam is null");
            ValidMetaData(() => metaData.LoadXml(xml));
        }

        public DefaultXmlServiceProvider(FileInfo xmlLocalPath)
        {
            if (xmlLocalPath == null) throw new ArgumentNullException("xmlLocalPath is null");
            if (!xmlLocalPath.Exists) throw new FileNotFoundException();
            if (xmlLocalPath.IsReadOnly) throw new Evolution.IOC.Exception.InvalidDataException("This fileInfo is read only.");
            ValidMetaData(()=> metaData.Load(xmlLocalPath.FullName));
        }

        private void ValidMetaData(Action validXmlAction)
        {
            try
            {
                validXmlAction();
            }
            catch (System.Exception e)
            {
                throw new Evolution.IOC.Exception.InvalidDataException($"Xml content is invalid", e);
            }
        }

        private readonly string XPath_ServicesNode = "/Evolution/IOC/Services";

        public override IRegisterInfo GetService(string interfaceFullName)
        {
            return GetServices().Where(it => string.Equals(it.InterfaceType.FullName, interfaceFullName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }

        public override IEnumerable<IRegisterInfo> GetServices()
        {
            if (metaData == null) throw new NullResultException("Current instance doesn't init metadata with xml format.");
            var properties = typeof(IRegisterInfo).GetProperties();
            var nodes = metaData.SelectSingleNode(XPath_ServicesNode);
            foreach (XmlNode node in nodes)
            {
                IRegisterInfo registerInfo = Activator.CreateInstance(typeof(ImplementInstanceInfo)) as IRegisterInfo;
                foreach (var property in properties)
                {
                    XmlAttribute attribute = node.Attributes[property.Name];
                    if (attribute == null) continue;
                    object attributeValue = null;
                    //System.Enum set value
                    if (property.PropertyType.IsEnum)
                    {
                        attributeValue = Enum.Parse(property.PropertyType, attribute.Value);
                    }
                    //System.Type set value
                    else if (property.PropertyType.IsClass && string.Equals(property.PropertyType.FullName, typeof(System.Type).FullName))
                    {
                        //找到该实现的type
                        attributeValue = base.GetRealType(attribute.Value);
                    }
                    else
                    {
                        attributeValue = Convert.ChangeType(attribute.Value, property.PropertyType);
                    }
                    registerInfo.GetType().GetProperty(property.Name).SetValue(registerInfo, attributeValue, null);
                }
                yield return registerInfo;
            }

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
