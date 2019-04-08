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
    public interface IBrockerListnerEventArg
    {
        string NotificationMessage { get; }

        void SetMessage(string message);

        XElement Data { get; }
        
    }

    public abstract class BrockerListnerEventArg<T> : EventArgs, IBrockerListnerEventArg
    {
        protected const string INSERTED_TAG = "INSERTED";

        protected const string DELETED_TAG = "DELETED";

        public string NotificationMessage { get; private set; }

        public NotificationTypes NotificationType
        {
            get
            {
                return (Data?.Element(INSERTED_TAG)) != null
                           ? (Data?.Element(DELETED_TAG)) != null
                                 ? NotificationTypes.Update
                                 : NotificationTypes.Insert
                           : (Data?.Element(DELETED_TAG)) != null
                                 ? NotificationTypes.Delete
                                 : NotificationTypes.None;
            }
        }

        public BrockerListnerEventArg()
        {

        }
        public BrockerListnerEventArg(string message) => this.NotificationMessage = message;


        public void SetMessage(string message) => NotificationMessage = message;

        public XElement Data
        {
            get
            {
                if (string.IsNullOrWhiteSpace(NotificationMessage)) return null;

                return ReadXDocumentWithInvalidCharacters(NotificationMessage);
            }
        }

        public T OdlValues
        {
            get
            {
                if (!(Data is XElement d)) return default(T);

                return GetResult(Data, DELETED_TAG);
            }
        }

        public T NewValues
        {
            get
            {
                if (!(Data is XElement d)) return default(T);

                return GetResult(Data, INSERTED_TAG);
            }
        }

        protected abstract T GetResult(XElement data, string tag);

        /// <summary>
        /// Converts an xml string into XElement with no invalid characters check.
        /// https://paulselles.wordpress.com/2013/07/03/parsing-xml-with-invalid-characters-in-c-2/
        /// </summary>
        /// <param name="xml">The input string.</param>
        /// <returns>The result XElement.</returns>
        private static XElement ReadXDocumentWithInvalidCharacters(string xml)
        {
            XDocument xDocument = null;

            XmlReaderSettings xmlReaderSettings = new XmlReaderSettings { CheckCharacters = false };

            using (var stream = new StringReader(xml))
            using (XmlReader xmlReader = XmlReader.Create(stream, xmlReaderSettings))
            {
                // Load our XDocument
                xmlReader.MoveToContent();
                xDocument = XDocument.Load(xmlReader);
            }

            return xDocument.Root;
        }
    }
}
