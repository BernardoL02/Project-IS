using SOMIOD.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;

namespace SOMIOD
{
    public class HandlerXML
    {
        //Responses
        public static XmlDocument responseApplications(List<Application> applicationList)
        {
            var xmlDoc = new XmlDocument();

            var applicationsNode = xmlDoc.CreateElement("Applications");
            xmlDoc.AppendChild(applicationsNode);

            foreach (var app in applicationList)
            {
                var applicationNode = xmlDoc.CreateElement("Application");

                var nameNode = xmlDoc.CreateElement("Name");
                nameNode.InnerText = app.Name;

                applicationNode.AppendChild(nameNode);
                applicationsNode.AppendChild(applicationNode);
            }

            return xmlDoc;
        }
    }
}