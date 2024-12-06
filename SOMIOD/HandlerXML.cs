using SOMIOD.Models;
using System;
using System.Collections.Generic;
using System.Web;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Linq;


namespace SOMIOD
{
    public class HandlerXML
    {

        //XSD
        string XmlFilePath = "C:\\Users\\Tex\\Desktop\\Project-IS\\SOMIOD\\XMLValidator.xsd";
        string validationMessage = "";
        private bool isValid;

        public bool ValidateXML(XElement xmlElement)
        {
            isValid = true;
            XmlDocument doc = new XmlDocument();

            if (xmlElement == null)
            {
                isValid = false;
                validationMessage = "ERROR: XML element is null."; // Mensagem de erro caso xmlElement seja null
                return isValid; // Retorna false sem tentar continuar com o processamento
            }


            try
            {
                // Converter XElement para XmlDocument
                using (System.IO.StringReader sr = new System.IO.StringReader(xmlElement.ToString()))
                {
                    doc.LoadXml(sr.ReadToEnd());
                }

                // Adicionar o esquema XSD ao XmlDocument
                doc.Schemas.Add(null, XmlFilePath);

                // Validar o XML
                doc.Validate(ValidationEventHandler);

            }
            catch (XmlException ex)
            {
                isValid = false;
                validationMessage = $"ERROR: {ex.ToString()}";
            }
            catch (XmlSchemaValidationException ex)
            {
                isValid = false;
                validationMessage = $"SCHEMA VALIDATION ERROR: {ex.Message}";
            }
            catch (Exception ex)
            {
                isValid = false;
                validationMessage = $"Unexpected error: {ex.Message}";
            }
            return isValid;
        }

        private void ValidationEventHandler(object sender, ValidationEventArgs args)
        {
            isValid = false;
            switch (args.Severity)
            {
                case XmlSeverityType.Error:
                    validationMessage = $"ERROR: {args.Message}";
                    break;
                case XmlSeverityType.Warning:
                    validationMessage = $"WARNING: {args.Message}";
                    break;
                default:
                    break;
            }
        }



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


        public static XmlDocument responseRecord(Record record)
        {
            var xmlDoc = new XmlDocument();

            var recordNode = xmlDoc.CreateElement("Record");

            var recordIdNode = xmlDoc.CreateElement("Id");
            recordIdNode.InnerText = record.Id.ToString();
            recordNode.AppendChild(recordIdNode);

            var recordNameNode = xmlDoc.CreateElement("Name");
            recordNameNode.InnerText = record.Name;
            recordNode.AppendChild(recordNameNode);

            var recordContentNode = xmlDoc.CreateElement("Content");
            recordContentNode.InnerText = record.Content;
            recordNode.AppendChild(recordContentNode);

            var recordCreationDateNode = xmlDoc.CreateElement("CreationDateTime");
            recordCreationDateNode.InnerText = record.CreationDateTime.ToString("o");
            recordNode.AppendChild(recordCreationDateNode);

            var recordParentNode = xmlDoc.CreateElement("Parent");
            recordParentNode.InnerText = record.Parent.ToString();
            recordNode.AppendChild(recordParentNode);

            xmlDoc.AppendChild(recordNode);

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