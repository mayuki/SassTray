using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace SassTray
{
    [XmlRoot]
    public class SassTrayConfig
    {
        private static XmlSerializer _xmlSerializer = null;

        static SassTrayConfig()
        {
            _xmlSerializer = new XmlSerializer(typeof(SassTrayConfig));
        }

        [XmlElement]
        public String OutputPath { get; set; }

        [XmlArrayItem("Option", Type = typeof(String))]
        public String[] Options { get; set; }

        public static SassTrayConfig Load(String path)
        {
            lock (_xmlSerializer)
            {
                try
                {
                    using (var stream = File.OpenRead(path))
                    {
                        return _xmlSerializer.Deserialize(stream) as SassTrayConfig;
                    }
                }
                catch (Exception)
                {
                    return new SassTrayConfig();
                }
            }
        }

        public void Save(String path)
        {
            lock (_xmlSerializer)
            {
                using (var stream = File.Open(path, FileMode.Create))
                {
                    _xmlSerializer.Serialize(stream, this);
                }
            }
        }
    }
}
