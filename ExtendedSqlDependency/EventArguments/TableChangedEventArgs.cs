using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace SqlDependencyEx.EventArguments
{
    public class TableChangedEventArgs<T> : BrockerListnerEventArg<List<T>>
    {
        public TableChangedEventArgs()
        {

        }
        public TableChangedEventArgs(string notificationMessage): base(notificationMessage) { }


        protected override List<T> GetResult(XElement data, string tag)
        {
            try
            {
                var events = new XmlDeserializationEvents();
                events.OnUnknownElement = new XmlElementEventHandler((sender, e) =>
                {
                    //TODO
                    //UnknownElement Handling
                });
                var readerSettings = new XmlReaderSettings { CloseInput = true };
                var tagListElements = data.Element(tag);
                List<T> values = new List<T>();

                foreach (var item in tagListElements.Elements())
                {
                    using (XmlReader reader = XmlReader.Create(new StringReader(item.ToString()), readerSettings))
                    {
                        XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                        values.Add((T)xmlSerializer.Deserialize(reader,events));
                    }
                }

                return values;
            }
            catch (Exception ex)
            {
                throw new Exception($"Problem mapping table columns to type {nameof(T)}. " +
                    $"Make sure you pecified the different XmlAttributes to the needing class and properties");
            }
        }
    }
}
