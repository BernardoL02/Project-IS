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

        public static XmlDocument responseContainers(List<string> names)
        {
            var xmlDoc = new XmlDocument();
            var applicationNode = xmlDoc.CreateElement("Application");

            foreach (var name in names)
            {
                var rootElement = xmlDoc.CreateElement("Container");
                applicationNode.AppendChild(rootElement);
                var nameElement = xmlDoc.CreateElement("Name");
                nameElement.InnerText = name;
                rootElement.AppendChild(nameElement);
                
            }

            xmlDoc.AppendChild(applicationNode);
            return xmlDoc;
        }

        public static XmlDocument responseApplication(Application application)
        {
            var xmlDoc = new XmlDocument();

            var applicationNode = xmlDoc.CreateElement("Application");

            var idNode = xmlDoc.CreateElement("Id");
            idNode.InnerText = application.Id.ToString();
            applicationNode.AppendChild(idNode);

            var nameNode = xmlDoc.CreateElement("Name");
            nameNode.InnerText = application.Name;
            applicationNode.AppendChild(nameNode);

            var creationDateNode = xmlDoc.CreateElement("CreationDateTime");
            creationDateNode.InnerText = application.CreationDateTime.ToString("o");
            applicationNode.AppendChild(creationDateNode);

            xmlDoc.AppendChild(applicationNode);

            return xmlDoc;
        }


        public static XmlDocument responseContainers(List<Container> containersList)
        {
            var xmlDoc = new XmlDocument();

            var ContainersNode = xmlDoc.CreateElement("Containers");
            xmlDoc.AppendChild(ContainersNode);

            foreach (var app in containersList)
            {
                var containerNode = xmlDoc.CreateElement("Container");

                var nameNode = xmlDoc.CreateElement("Name");
                nameNode.InnerText = app.Name;

                containerNode.AppendChild(nameNode);
                ContainersNode.AppendChild(containerNode);
            }

            return xmlDoc;
        }

        public static XmlDocument responseContainer(Container container)
        {
            var xmlDoc = new XmlDocument();

            var containerNode = xmlDoc.CreateElement("Container");

            var containerIdNode = xmlDoc.CreateElement("Id");
            containerIdNode.InnerText = container.Id.ToString();
            containerNode.AppendChild(containerIdNode);

            var containerNameNode = xmlDoc.CreateElement("Name");
            containerNameNode.InnerText = container.Name;
            containerNode.AppendChild(containerNameNode);

            var containerCreationDateNode = xmlDoc.CreateElement("CreationDateTime");
            containerCreationDateNode.InnerText = container.CreationDateTime.ToString("o");
            containerNode.AppendChild(containerCreationDateNode);

            var containerParentNode = xmlDoc.CreateElement("Parent");
            containerParentNode.InnerText = container.Parent.ToString();
            containerNode.AppendChild(containerParentNode);

            xmlDoc.AppendChild(containerNode);

            return xmlDoc;
        }









        //Response errors
        public static XmlDocument responseError(string message, string errorCode)
        {
            var xmlDoc = new XmlDocument();

            var errorNode = xmlDoc.CreateElement("Error");

            var errorMessageNode = xmlDoc.CreateElement("Message");
            errorMessageNode.InnerText = message;
            errorNode.AppendChild(errorMessageNode);

            var errorCodeNode = xmlDoc.CreateElement("ErrorCode");
            errorCodeNode.InnerText = errorCode;
            errorNode.AppendChild(errorCodeNode);

            xmlDoc.AppendChild(errorNode);

            return xmlDoc;
        }

    }
}