using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace Evolution.IOC
{
    public static class SerializerHelper
    {
        public static string Serialize<T>(T jsonObj) where T:class
        {
            //序列化
            DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(T));
            using (MemoryStream msObj = new MemoryStream())
            {
                //将序列化之后的Json格式数据写入流中
                js.WriteObject(msObj, jsonObj);
                msObj.Position = 0;
                using (StreamReader sr = new StreamReader(msObj, Encoding.UTF8))
                {
                    string json = sr.ReadToEnd();
                    return json;
                }
            }
        }

        public static T Deserialize<T>(string toDes) where T:class
        {
            //反序列化
            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(toDes)))
            {
                DataContractJsonSerializer deseralizer = new DataContractJsonSerializer(typeof(IOCJsonConfig));
                T model = (T)deseralizer.ReadObject(ms);
                return model;
            }
        }
    }
}
