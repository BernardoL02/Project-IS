using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Xml.Linq;

namespace SOMIOD.Models
{
    public class Record
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public DateTime CreationDateTime { get; set; }
        public int Parent { get; set; }

        public override string ToString()
        {
            var notificationXml = new XElement("Record",
                new XElement("Name", Name),
                new XElement("Content", Content),
                new XElement("CreationDateTime", CreationDateTime),
                new XElement("Parent", Parent)
            );

            return notificationXml.ToString();
        }

    }
}