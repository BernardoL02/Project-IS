﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Http.Results;
using System.Xml.Linq;
using SOMIOD.Models;
using static System.Net.Mime.MediaTypeNames;
using Application = SOMIOD.Models.Application;
using System.Xml;
using System.IO;
using System.Xml.Serialization;
using System.Configuration;


namespace SOMIOD.Controllers
{
    public class SOMIODController : ApiController
    {
        string strConnection = System.Configuration.ConfigurationManager.ConnectionStrings["SOMIOD.Properties.Settings.ConnectionToDB"].ConnectionString;

        //-------------------------------------------------------------------------------------
        //--------------------------------------- Locate --------------------------------------
        //------------------------------------------------------------------------------------- 
        [HttpGet]
        [Route("api/somiod")]
        public IHttpActionResult LocateApplications()
        {
            List<Application> applicationList = new List<Application>();
            IEnumerable<string> headerValues;
            string headerValue = null;
            SqlConnection conn = null;
            SqlDataReader reader = null;

            if (Request.Headers.TryGetValues("somiod-locate", out headerValues))
            {
                headerValue = headerValues.FirstOrDefault()?.ToUpper();
            }

            if (string.IsNullOrEmpty(headerValue))
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError("The header value is required (somiod-locate).", "400"), Configuration.Formatters.XmlFormatter);
            }

            if (headerValue != "APPLICATION")
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError("The header value is invalid. Expected value 'Application'", "400"), Configuration.Formatters.XmlFormatter);
            }

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand command = new SqlCommand($"SELECT name FROM {headerValue}", conn);

                reader = command.ExecuteReader();

                while (reader.Read())
                {
                    applicationList.Add(new Application
                    {
                        Name = (string)reader["Name"]
                    });
                }
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            finally
            {
                conn.Close();
                reader.Close();
            }

            return Content(System.Net.HttpStatusCode.OK, HandlerXML.responseApplications(applicationList), Configuration.Formatters.XmlFormatter);
        }


        [HttpGet]
        [Route("api/somiod/{application}")]
        public IHttpActionResult LocateContainersRecordsNotifications(string application)
        {
            IEnumerable<string> headerValues;
            string headerValue = null;
            SqlConnection conn = null;
            SqlDataReader sqlReader = null;
            List<string> NamesList = null;

            Application applicationInfo = this.verifyApplicationExists(application);

            if (applicationInfo == null)
            {
                return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Application was not found.", "404"), Configuration.Formatters.XmlFormatter);
            }

            if (Request.Headers.TryGetValues("somiod-locate", out headerValues))
            {
                headerValue = headerValues.FirstOrDefault()?.ToUpper();
            }

            if (string.IsNullOrEmpty(headerValue))
            {
                return Content(HttpStatusCode.OK, HandlerXML.responseApplication(applicationInfo), Configuration.Formatters.XmlFormatter);
            }

            try
            {
                NamesList = new List<string>();

                string query = null;
                switch (headerValue)
                {
                    case "CONTAINER":
                        query = "SELECT Container.name FROM Container JOIN Application ON Container.parent = Application.id WHERE Application.name = @application";
                        break;

                    case "RECORD":
                        query = "SELECT Record.name FROM Record JOIN container ON Record.parent = container.id JOIN application ON container.parent = application.id WHERE application.name = @application";
                        break;

                    case "NOTIFICATION":
                        query = "SELECT Notification.name FROM Notification JOIN Notification ON Notification.parent = container.id JOIN application ON container.parent = application.id WHERE application.name = @application";
                        break;

                    default:
                        return Content(HttpStatusCode.BadRequest, HandlerXML.responseError("Invalid Header. Expected values are 'Container', 'Record', or 'Notification'.", "400"), Configuration.Formatters.XmlFormatter);
                }

                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand command = new SqlCommand(query, conn);

                command.Parameters.AddWithValue("@application", application);

                sqlReader = command.ExecuteReader();

                while (sqlReader.Read())
                {
                    string name = sqlReader.GetString(0);
                    NamesList.Add(name);
                }
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            finally
            {
                if(conn != null) { conn.Close(); }
                if (sqlReader != null) { sqlReader.Close(); }
            }

            return Content(HttpStatusCode.OK, HandlerXML.responseContainers(NamesList), Configuration.Formatters.XmlFormatter);
        }



        //-------------------------------------------------------------------------------------
        //------------------------------------ Applications -----------------------------------
        //------------------------------------------------------------------------------------- 
        [HttpPost]
        [Route("api/somiod")]
        public IHttpActionResult PostApplication([FromBody] XElement applicationXml)
        {
            SqlConnection conn = null;
            int affectedfRows = -1;

            if (applicationXml == null)
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError("Invalid XML body.", "400"), Configuration.Formatters.XmlFormatter);
            }

            try
            {
                var application = new
                {
                    Name = applicationXml.Element("Name")?.Value
                };

                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand command = new SqlCommand("INSERT INTO Application(Name) VALUES (@name)", conn);
                command.Parameters.AddWithValue("@name", application.Name);

                affectedfRows = command.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627 || ex.Number == 2601)
                {
                    return Content(HttpStatusCode.Conflict, HandlerXML.responseError("Application name already exists", "409"), Configuration.Formatters.XmlFormatter);
                }

                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            finally
            {
                if (conn != null) { conn.Close(); }
            }

            if (affectedfRows > 0)
            {
                return StatusCode(HttpStatusCode.Created);
            }
            else
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("The application could not be created.", "500"), Configuration.Formatters.XmlFormatter);
            }
        }


        [HttpPatch]
        [Route("api/somiod/{application}")]
        public IHttpActionResult PatchApplication(string application, [FromBody] XElement applicationXml)
        {
            SqlConnection conn = null;
            int affectedRows = -1;

            if (applicationXml == null)
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError("Invalid XML body.", "400"), Configuration.Formatters.XmlFormatter);
            }

            if (this.verifyApplicationExists(application) == null)
            {
                return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Application was not found.", "404"), Configuration.Formatters.XmlFormatter);
            }

            string newName = applicationXml.Element("Name")?.Value;
            if (string.IsNullOrEmpty(newName))
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError("Application XML must contain a valid 'name' element.", "400"), Configuration.Formatters.XmlFormatter);
            }

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand command = new SqlCommand("UPDATE Application SET Name = @newName WHERE Name = @appName", conn);
                command.Parameters.AddWithValue("@newName", newName);
                command.Parameters.AddWithValue("@appName", application);

                affectedRows = command.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627 || ex.Number == 2601)
                {
                    return Content(HttpStatusCode.Conflict, HandlerXML.responseError("Application name already exists", "409"), Configuration.Formatters.XmlFormatter);
                }

                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            finally
            {
                if (conn != null) { conn.Close(); }
            }

            if (affectedRows > 0)
            {
                return Content(HttpStatusCode.OK, this.verifyApplicationExists(newName), Configuration.Formatters.XmlFormatter);
            }
            else
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("The application could not be updated.", "500"), Configuration.Formatters.XmlFormatter);
            }

        }


        [HttpDelete]
        [Route("api/somiod/{application}")]
        public IHttpActionResult DeleteApplication(string application)
        {
            SqlConnection conn = null;

            Application app = this.verifyApplicationExists(application);

            if (app == null)
            {
                return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Application was not found.", "404"), Configuration.Formatters.XmlFormatter);
            }

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand cmd = new SqlCommand("DELETE FROM Application WHERE Name = @name", conn);
                cmd.Parameters.AddWithValue("@name", application);
                cmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (ex.Number == 547)
                {
                    return Content(HttpStatusCode.Conflict, HandlerXML.responseError("The application cannot be deleted because there are related dependencies.", "409"), Configuration.Formatters.XmlFormatter);
                }

                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            finally
            {
                if (conn != null) { conn.Close(); }
            }

            return Content(HttpStatusCode.OK, app, Configuration.Formatters.XmlFormatter);
        }


        private Application verifyApplicationExists(string name)
        {
            SqlConnection conn = null;
            SqlDataReader sqlReader = null;
            Application application = null;

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand command = new SqlCommand("SELECT * FROM Application WHERE name = @appName", conn);
                command.Parameters.AddWithValue("@appName", name);

                sqlReader = command.ExecuteReader();

                while (sqlReader.Read())
                {
                    application = new Application
                    {
                        Id = sqlReader.GetInt32(0),
                        Name = sqlReader.GetString(1),
                        CreationDateTime = sqlReader.GetDateTime(2),
                    };
                }

                if (application != null)
                {
                    return application;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
            finally
            {
                if (conn != null) { conn.Close(); }
                if (sqlReader != null) { sqlReader.Close(); }
            }

            return null;
        }
    }

    //-------------------------------------------------------------------------------------
    //------------------------------------- Containers ------------------------------------
    //------------------------------------------------------------------------------------- 















}

