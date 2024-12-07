using System;
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
using System.Net.Http;
using System.Threading;


namespace SOMIOD.Controllers
{
    public class SOMIODController : ApiController
    {
        string strConnection = System.Configuration.ConfigurationManager.ConnectionStrings["SOMIOD.Properties.Settings.ConnectionToDB"].ConnectionString;


        #region Locate

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
                //Retornar aplicação específica 
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
        #endregion


        #region Applications

        //-------------------------------------------------------------------------------------
        //------------------------------------ Applications -----------------------------------
        //------------------------------------------------------------------------------------- 
        [HttpPost]
        [Route("api/somiod")]
        public IHttpActionResult PostApplication([FromBody] XElement applicationXml)
        {
            SqlConnection conn = null;
            int affectedfRows = -1;

            HandlerXML handlerXML = new HandlerXML();
            string validationMessage = handlerXML.ValidateXML(applicationXml);

            if (!validationMessage.Equals("Valid"))
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError($"Invalid XML: {validationMessage}", "400"), Configuration.Formatters.XmlFormatter);
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

            HandlerXML handlerXML = new HandlerXML();
            string validationMessage = handlerXML.ValidateXML(applicationXml);

            if (!validationMessage.Equals("Valid"))
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError($"Invalid XML: {validationMessage}", "400"), Configuration.Formatters.XmlFormatter);
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

        #endregion


        #region Containers
        //-------------------------------------------------------------------------------------
        //------------------------------------- Containers ------------------------------------
        //------------------------------------------------------------------------------------- 

        [HttpPost]
        [Route("api/somiod/{application}")]
        public IHttpActionResult PostContainer(string application, [FromBody] XElement containerXml)
        {
            SqlConnection conn = null;
            int affectedfRows = -1;

            HandlerXML handlerXML = new HandlerXML();
            string validationMessage = handlerXML.ValidateXML(containerXml);

            if (!validationMessage.Equals("Valid"))
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError($"Invalid XML: {validationMessage}", "400"), Configuration.Formatters.XmlFormatter);
            }

            Application app = this.verifyApplicationExists(application);

            if (app == null)
            {
                return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Application was not found.", "404"), Configuration.Formatters.XmlFormatter);
            }

            try
            {
                var container = new
                {
                    Name = containerXml.Element("Name")?.Value
                };

                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand command = new SqlCommand("INSERT INTO Container(Name,Parent) VALUES (@name,@parantId)", conn);
                command.Parameters.AddWithValue("@name", container.Name);
                command.Parameters.AddWithValue("@parantId", app.Id);

                affectedfRows = command.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627 || ex.Number == 2601)
                {
                    return Content(HttpStatusCode.Conflict, HandlerXML.responseError("Container name already exists", "409"), Configuration.Formatters.XmlFormatter);
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

        [HttpGet]
        [Route("api/somiod/{application}/{container}")]
        public IHttpActionResult GetContainer(string application, string container)
        {
            SqlConnection conn = null;
            SqlDataReader sqlReader = null;

            Container containerToGet = null;

            IHttpActionResult status = verifyParentOfContainer(application, container);

            if (status.ExecuteAsync(CancellationToken.None).Result.StatusCode != HttpStatusCode.OK)
            {
                return status;
            }

            Application app = this.verifyApplicationExists(application);

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand cmd = new SqlCommand("SELECT * FROM Container WHERE name = @name AND Parent = @parantId", conn);
                cmd.Parameters.AddWithValue("@name", container);
                cmd.Parameters.AddWithValue("@parantId", app.Id);

                sqlReader = cmd.ExecuteReader();

                while (sqlReader.Read())
                {
                    containerToGet = new Container
                    {
                        Id = (int)sqlReader["Id"],
                        Name = (string)sqlReader["Name"],
                        CreationDateTime = (DateTime)sqlReader["CreationDateTime"],
                        Parent = (int)sqlReader["Parent"]
                    };
                }
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            finally
            {
                if (conn != null) { conn.Close(); }
                if (sqlReader != null) { sqlReader.Close(); }
            }

            if (containerToGet == null)
            {
                return Content(HttpStatusCode.NotFound,HandlerXML.responseError("Container does not belong to the specified application.", "404"),Configuration.Formatters.XmlFormatter);
            }

            return Content(HttpStatusCode.OK, HandlerXML.responseContainer(containerToGet), Configuration.Formatters.XmlFormatter);
        }


        [HttpPatch]
        [Route("api/somiod/{application}/{container}")]
        public IHttpActionResult PatchContainer(string application, string container, [FromBody] XElement containerXml)
        {
            SqlConnection conn = null;
            int affectedRows = -1;

            HandlerXML handlerXML = new HandlerXML();
            string validationMessage = handlerXML.ValidateXML(containerXml);

            if (!validationMessage.Equals("Valid"))
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError($"Invalid XML: {validationMessage}", "400"), Configuration.Formatters.XmlFormatter);
            }

            IHttpActionResult status = verifyParentOfContainer(application, container);

            if (status.ExecuteAsync(CancellationToken.None).Result.StatusCode != HttpStatusCode.OK)
            {
                return status;
            }

            Application app = this.verifyApplicationExists(application);
            Container cont = this.verifyContainerExists(container);
       
            //Verificar valores no xml
            string newName = containerXml.Element("Name")?.Value;
            string newParentId = containerXml.Element("Parent")?.Value;
            if (string.IsNullOrEmpty(newName) && string.IsNullOrEmpty(newParentId))
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError("The container XML must contain at least a 'Name' or 'Parent' element.", "400"), Configuration.Formatters.XmlFormatter);
            }

            if(string.IsNullOrEmpty(newName))
            {
                newName = cont.Name;
            }

            if (string.IsNullOrEmpty(newParentId))
            {
                newParentId = (cont.Parent).ToString();
            }

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand command = new SqlCommand("UPDATE Container SET Name = @newName, Parent = @newParentId WHERE Parent = @parantId AND name = @containerName", conn);
                command.Parameters.AddWithValue("@newName", newName);
                command.Parameters.AddWithValue("@newParentId", int.Parse(newParentId));
                command.Parameters.AddWithValue("@parantId", app.Id);
                command.Parameters.AddWithValue("@containerName", container);

                affectedRows = command.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627 || ex.Number == 2601)
                {
                    return Content(HttpStatusCode.Conflict, HandlerXML.responseError("Container name already exists", "409"), Configuration.Formatters.XmlFormatter);
                }
                else if (ex.Number == 547)
                {
                    return Content(HttpStatusCode.BadRequest, HandlerXML.responseError("Invalid Parent ID (Application Not Found).", "400"), Configuration.Formatters.XmlFormatter);
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
                return Content(HttpStatusCode.OK, this.verifyContainerExists(newName), Configuration.Formatters.XmlFormatter);
            }
            else
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("The container could not be updated.", "500"), Configuration.Formatters.XmlFormatter);
            }
        }


        [HttpDelete]
        [Route("api/somiod/{application}/{container}")]
        public IHttpActionResult DeleteContainer(string application, string container)
        {
            SqlConnection conn = null;

            IHttpActionResult status = verifyParentOfContainer(application, container);

            if (status.ExecuteAsync(CancellationToken.None).Result.StatusCode != HttpStatusCode.OK)
            {
                return status;
            }

            Application app = this.verifyApplicationExists(application);
            Container cont = this.verifyContainerExists(container);

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand cmd = new SqlCommand("DELETE FROM Container WHERE Name = @name AND Parent = @parantId", conn);
                cmd.Parameters.AddWithValue("@name", container);
                cmd.Parameters.AddWithValue("@parantId", app.Id);
                cmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (ex.Number == 547)
                {
                    return Content(HttpStatusCode.Conflict, HandlerXML.responseError("The container cannot be deleted because there are related dependencies.", "409"), Configuration.Formatters.XmlFormatter);
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

            return Content(HttpStatusCode.OK, cont, Configuration.Formatters.XmlFormatter);
        }
        #endregion


        #region Record

        //-------------------------------------------------------------------------------------
        //--------------------------------------- Record --------------------------------------
        //------------------------------------------------------------------------------------- 

        private IHttpActionResult PostRecord(string application, string container, [FromBody] XElement recordXml)
        {
            
            SqlConnection conn = null;
            int affectedfRows = 0;

            HandlerXML handlerXML = new HandlerXML();
            string validationMessage = handlerXML.ValidateXML(recordXml);

            if (!validationMessage.Equals("Valid"))
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError($"Invalid XML: {validationMessage}", "400"), Configuration.Formatters.XmlFormatter);
            }

            IHttpActionResult status = verifyParentOfContainer(application, container);

            if (status.ExecuteAsync(CancellationToken.None).Result.StatusCode != HttpStatusCode.OK)
            {
                return status;
            }

            Container cont = verifyContainerExists(container);
            try
            {
                var record = new
                {
                    Name = recordXml.Element("Name")?.Value,
                    Content = recordXml.Element("Content")?.Value
                };

                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand command = new SqlCommand("INSERT INTO Record(Name,Content,Parent) VALUES (@name,@content,@parentId)", conn);
                command.Parameters.AddWithValue("@name", record.Name);
                command.Parameters.AddWithValue("@content", record.Content);
                command.Parameters.AddWithValue("@parentId", cont.Id);

                affectedfRows = command.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627 || ex.Number == 2601)
                {
                    return Content(HttpStatusCode.Conflict, HandlerXML.responseError("Record name already exists", "409"), Configuration.Formatters.XmlFormatter);
                }
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
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("The record could not be created.", "500"), Configuration.Formatters.XmlFormatter);
            }
        }


        [HttpGet]
        [Route("api/somiod/{application}/{container}/record/{name}")]
        public IHttpActionResult GetRecord(string application, string container, string name)
        {

            SqlConnection conn = null;
            SqlDataReader sqlReader = null;

            Record record = null;


            IHttpActionResult status = verifyParentOfContainer(application, container);

            if (status.ExecuteAsync(CancellationToken.None).Result.StatusCode != HttpStatusCode.OK)
            {
                return status;
            }

            IHttpActionResult ischildren = verifyParentOfRecordAndNotification(container, "Record", name); ;

            if (ischildren.ExecuteAsync(CancellationToken.None).Result.StatusCode != HttpStatusCode.OK)
            {
                return ischildren;
            }
           

            Container cont = this.verifyContainerExists(container);

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand cmd = new SqlCommand("SELECT * FROM Record WHERE name = @name AND Parent = @parantId", conn);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@parantId", cont.Id);

                sqlReader = cmd.ExecuteReader();

                while (sqlReader.Read())
                {
                    record = new Record
                    {
                        Id = (int)sqlReader["Id"],
                        Name = (string)sqlReader["Name"],
                        Content = (string)sqlReader["Content"],
                        CreationDateTime = (DateTime)sqlReader["CreationDateTime"],
                        Parent = (int)sqlReader["Parent"]
                    };
                }
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            finally
            {
                if (conn != null) { conn.Close(); }
                if (sqlReader != null) { sqlReader.Close(); }
            }

            if (record == null)
            {
                return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Record was not found.", "404"), Configuration.Formatters.XmlFormatter);
            }
            return Content(HttpStatusCode.OK, HandlerXML.responseRecord(record), Configuration.Formatters.XmlFormatter);

        }


        [HttpDelete]
        [Route("api/somiod/{application}/{container}/record/{name}")]
        public IHttpActionResult DeleteRecord(string application, string container, string name)
        {
            SqlConnection conn = null;

            IHttpActionResult status = verifyParentOfContainer(application, container);

            if (status.ExecuteAsync(CancellationToken.None).Result.StatusCode != HttpStatusCode.OK)
            {
                return status;
            }

            IHttpActionResult ischildren = verifyParentOfRecordAndNotification(container, "Record", name); ;

            if (ischildren.ExecuteAsync(CancellationToken.None).Result.StatusCode != HttpStatusCode.OK)
            {
                return ischildren;
            }

            Container cont = this.verifyContainerExists(container);
            Record record = this.verifyRecordExists(name);

            if (record == null)
            {
                return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Record was not found.", "404"), Configuration.Formatters.XmlFormatter);
            }

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand cmd = new SqlCommand("DELETE FROM Record WHERE Name = @name AND Parent = @parantId", conn);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@parantId", cont.Id);
                cmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            { 
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

            return Content(HttpStatusCode.OK, record, Configuration.Formatters.XmlFormatter);
        }
        #endregion


        #region Notifications
        //-------------------------------------------------------------------------------------
        //------------------------------------ Notifications ----------------------------------
        //-------------------------------------------------------------------------------------

        private IHttpActionResult PostNotification(string application, string container, [FromBody] XElement notificationXml)
        {
            SqlConnection conn = null;
            int affectedfRows = 0;

            HandlerXML handlerXML = new HandlerXML();
            string validationMessage = handlerXML.ValidateXML(notificationXml);

            if (!validationMessage.Equals("Valid"))
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError($"Invalid XML: {validationMessage}", "400"), Configuration.Formatters.XmlFormatter);
            }

            IHttpActionResult status = verifyParentOfContainer(application, container);
            if (status.ExecuteAsync(CancellationToken.None).Result.StatusCode != HttpStatusCode.OK)
            {
                return status;
            }

            Container cont = verifyContainerExists(container);
            try
            {
                var notification = new
                {
                    Name = notificationXml.Element("Name").Value,
                    Event = notificationXml.Element("Event").Value,
                    Endpoint = notificationXml.Element("Endpoint").Value,
                    Enabled = notificationXml.Element("Enabled").Value
                };

                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand command = new SqlCommand("INSERT INTO Notification(Name,Parent,Event,Endpoint,Enabled) " +
                                                                 "VALUES (@name,@parentId,@event,@endpoint,@enabled)", conn);

                command.Parameters.AddWithValue("@name", notification.Name);
                command.Parameters.AddWithValue("@parentId", cont.Id);
                command.Parameters.AddWithValue("@event", notification.Event);
                command.Parameters.AddWithValue("@endpoint", notification.Endpoint);
                command.Parameters.AddWithValue("@enabled", notification.Enabled);

                affectedfRows = command.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627 || ex.Number == 2601)
                {
                    return Content(HttpStatusCode.Conflict, HandlerXML.responseError("Notification name already exists", "409"), Configuration.Formatters.XmlFormatter);
                }
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
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("The notification could not be created.", "500"), Configuration.Formatters.XmlFormatter);
            }
        }


        [HttpGet]
        [Route("api/somiod/{application}/{container}/notification/{name}")]
        public IHttpActionResult GetNotification(string application, string container, string name)
        {
            SqlConnection conn = null;
            SqlDataReader sqlReader = null;

            Notification notification = null;

            IHttpActionResult status = verifyParentOfContainer(application, container);

            if (status.ExecuteAsync(CancellationToken.None).Result.StatusCode != HttpStatusCode.OK)
            {
                return status;
            }

            IHttpActionResult ischildren = verifyParentOfRecordAndNotification(container, "Notification", name);

            if (ischildren.ExecuteAsync(CancellationToken.None).Result.StatusCode != HttpStatusCode.OK)
            {
                return ischildren;
            }

            Container cont = this.verifyContainerExists(container);

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand cmd = new SqlCommand("SELECT * FROM Notification WHERE name = @name AND Parent = @parantId", conn);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@parantId", cont.Id);

                sqlReader = cmd.ExecuteReader();

                while (sqlReader.Read())
                {
                    notification = new Notification
                    {
                        Id = (int)sqlReader["Id"],
                        Name = (string)sqlReader["Name"],
                        Event = (int)sqlReader["Event"],
                        Endpoint = (string)sqlReader["Endpoint"],
                        Enabled = (bool)sqlReader["Enabled"],
                        Parent = (int)sqlReader["Parent"],
                        CreationDateTime = (DateTime)sqlReader["CreationDateTime"]
                    };
                }
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
            finally
            {
                if (conn != null) { conn.Close(); }
                if (sqlReader != null) { sqlReader.Close(); }
            }

            if (notification == null)
            {
                return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Notification was not found.", "404"), Configuration.Formatters.XmlFormatter);
            }

            return Content(HttpStatusCode.OK, HandlerXML.responseNotification(notification), Configuration.Formatters.XmlFormatter);
        }


        [HttpDelete]
        [Route("api/somiod/{application}/{container}/notification/{name}")]
        public IHttpActionResult DeleteNotification(string application, string container, string name)
        {
            SqlConnection conn = null;

            IHttpActionResult status = verifyParentOfContainer(application, container);

            if (status.ExecuteAsync(CancellationToken.None).Result.StatusCode != HttpStatusCode.OK)
            {
                return status;
            }

            Container cont = this.verifyContainerExists(container);
            Notification notification = this.verifyNotifcationExists(name);

            if (notification == null)
            {
                return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Notification was not found.", "404"), Configuration.Formatters.XmlFormatter);
            }

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand cmd = new SqlCommand("DELETE FROM Notification WHERE Name = @name AND Parent = @parantId", conn);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@parantId", cont.Id);

                cmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
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

            return Content(HttpStatusCode.OK, notification, Configuration.Formatters.XmlFormatter);
        }

        #endregion







        //-------------------------------------------------------------------------------------
        //--------------------------------- Suport Functions ----------------------------------
        //------------------------------------------------------------------------------------- 

        [HttpPost]
        [Route("api/somiod/{application}/{container}")]
        public IHttpActionResult PostRecordOrNotification(string application, string container, [FromBody] XElement elementXML)
        {
            HandlerXML handlerXML = new HandlerXML();
            string validationMessage = handlerXML.ValidateXML(elementXML);

            if (!validationMessage.Equals("Valid"))
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError($"Invalid XML: {validationMessage}", "400"), Configuration.Formatters.XmlFormatter);
            }

            IHttpActionResult status = verifyParentOfContainer(application, container);

            if (status.ExecuteAsync(CancellationToken.None).Result.StatusCode != HttpStatusCode.OK)
            {
                return status;
            }

            try
            {
                string rootName = elementXML.Name.LocalName;

                if (rootName.Equals("Record", StringComparison.OrdinalIgnoreCase))
                { 
                    return PostRecord(application, container, elementXML);
                }

                if (rootName.Equals("Notification", StringComparison.OrdinalIgnoreCase))
                {
                    return PostNotification(application, container, elementXML);
                }

                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError($"Unexpected root element: {rootName}. Expected 'Record' or 'Notification'.", "400"), Configuration.Formatters.XmlFormatter);

            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError("An error occurred while processing your request. Please try again later.", "500"), Configuration.Formatters.XmlFormatter);
            }
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


        private Container verifyContainerExists(string name)
        {
            SqlConnection conn = null;
            SqlDataReader sqlReader = null;
            Container container = null;

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand command = new SqlCommand("SELECT * FROM Container WHERE name = @containerName", conn);
                command.Parameters.AddWithValue("@containerName", name);

                sqlReader = command.ExecuteReader();

                while (sqlReader.Read())
                {
                    container = new Container
                    {
                        Id = (int)sqlReader["Id"],
                        Name = (string)sqlReader["Name"],
                        CreationDateTime = (DateTime)sqlReader["CreationDateTime"],
                        Parent = (int)sqlReader["Parent"]
                    };
                }

                if (container != null)
                {
                    return container;
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

        private Record verifyRecordExists(string name)
        {
            SqlConnection conn = null;
            SqlDataReader sqlReader = null;
            Record record = null;

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand command = new SqlCommand("SELECT * FROM Record WHERE name = @recordName", conn);
                command.Parameters.AddWithValue("@recordName", name);

                sqlReader = command.ExecuteReader();

                while (sqlReader.Read())
                {
                    record = new Record
                    {
                        Id = (int)sqlReader["Id"],
                        Name = (string)sqlReader["Name"],
                        Content = (string)sqlReader["Content"],
                        CreationDateTime = (DateTime)sqlReader["CreationDateTime"],
                        Parent = (int)sqlReader["Parent"]
                    };
                }

                if (record != null)
                {
                    return record;
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

        private Notification verifyNotifcationExists(string name)
        {
            SqlConnection conn = null;
            SqlDataReader sqlReader = null;
            Notification notification = null;

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand command = new SqlCommand("SELECT * FROM Notification WHERE name = @notificationName", conn);
                command.Parameters.AddWithValue("@notificationName", name);

                sqlReader = command.ExecuteReader();

                while (sqlReader.Read())
                {
                    notification = new Notification
                    {
                        Id = (int)sqlReader["Id"],
                        Name = (string)sqlReader["Name"],
                        Event = (int)sqlReader["Event"],
                        Endpoint = (string)sqlReader["Endpoint"],
                        Enabled = (bool)sqlReader["Enabled"],
                        Parent = (int)sqlReader["Parent"],
                        CreationDateTime = (DateTime)sqlReader["CreationDateTime"]
                    };
                }

                if (notification != null)
                {
                    return notification;
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


        private IHttpActionResult verifyParentOfContainer(string application, string container)
        {
            SqlConnection conn = null;
            SqlDataReader sqlReader = null;

            Container ischildren = null;

            Application app = this.verifyApplicationExists(application);

            if (app == null)
            {
                return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Application was not found.", "404"), Configuration.Formatters.XmlFormatter);
            }

            if (this.verifyContainerExists(container) == null)
            {
                return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Container was not found.", "404"), Configuration.Formatters.XmlFormatter);
            }

            conn = new SqlConnection(strConnection);
            conn.Open();

            SqlCommand cmd = new SqlCommand("SELECT * FROM Container WHERE name = @name AND Parent = @parantId", conn);
            cmd.Parameters.AddWithValue("@name", container);
            cmd.Parameters.AddWithValue("@parantId", app.Id);

            sqlReader = cmd.ExecuteReader();

            while (sqlReader.Read())
            {
                ischildren= new Container
                {
                    Id = (int)sqlReader["Id"],
                    Name = (string)sqlReader["Name"],
                    CreationDateTime = (DateTime)sqlReader["CreationDateTime"],
                    Parent = (int)sqlReader["Parent"]
                };
            }

            if (ischildren != null)
            {
                return Content(HttpStatusCode.OK,"OK");
            }

            return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Container does not belong to the specified application.", "404"), Configuration.Formatters.XmlFormatter);
        }

        private IHttpActionResult verifyParentOfRecordAndNotification(string container, string resource, string name)
        {
            SqlConnection conn = null;
            SqlDataReader sqlReader = null;
            object ischildren = null;

            try
            {
                Container cont = this.verifyContainerExists(container);
                if (cont == null)
                {
                    return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Container was not found.", "404"), Configuration.Formatters.XmlFormatter);
                }

                if (resource.Equals("Record", StringComparison.OrdinalIgnoreCase))
                {
                    if (this.verifyRecordExists(name) == null)
                    {
                        return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Record was not found.", "404"), Configuration.Formatters.XmlFormatter);
                    }
                }
                else if (resource.Equals("Notification", StringComparison.OrdinalIgnoreCase))
                {
                    if (this.verifyNotifcationExists(name) == null)
                    {
                        return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Notification was not found.", "404"), Configuration.Formatters.XmlFormatter);
                    }
                }
          

                conn = new SqlConnection(strConnection);
                conn.Open();

                string query = $"SELECT * FROM {resource} WHERE Name = @name AND Parent = @parentId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@parentId", cont.Id);

                sqlReader = cmd.ExecuteReader();

                if (sqlReader.Read())
                {
                    if (resource.Equals("Record", StringComparison.OrdinalIgnoreCase))
                    {
                        ischildren = new Record
                        {
                            Id = (int)sqlReader["Id"],
                            Name = (string)sqlReader["Name"],
                            Content = (string)sqlReader["Content"],
                            CreationDateTime = (DateTime)sqlReader["CreationDateTime"],
                            Parent = (int)sqlReader["Parent"]
                        };
                    }
                    else if (resource.Equals("Notification", StringComparison.OrdinalIgnoreCase))
                    {
                        ischildren = new Notification
                        {
                            Id = (int)sqlReader["Id"],
                            Name = (string)sqlReader["Name"],
                            Event = (int)sqlReader["Event"],
                            Endpoint = (string)sqlReader["Endpoint"],
                            Enabled = (bool)sqlReader["Enabled"],
                            CreationDateTime = (DateTime)sqlReader["CreationDateTime"],
                            Parent = (int)sqlReader["Parent"]
                        };
                    }
                }

                if (ischildren != null)
                {
                    return Content(HttpStatusCode.OK, "OK");
                }
                else
                {
                    return Content(HttpStatusCode.NotFound, HandlerXML.responseError($"{resource} does not belong to the specified container.", "404"), Configuration.Formatters.XmlFormatter);
                }
            }
            catch (SqlException ex)
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError($"SQL Error: {ex.Message}", "500"), Configuration.Formatters.XmlFormatter);
            }
            catch (Exception ex)
            {
                return Content(HttpStatusCode.InternalServerError, HandlerXML.responseError($"Unexpected Error: {ex.Message}", "500"), Configuration.Formatters.XmlFormatter);
            }
            finally
            {
                sqlReader?.Close();
                conn?.Close();
            }

        }


        [HttpPost]
        [Route("api/testXSD/somiod")]
        public IHttpActionResult testXsd([FromBody] XElement recordXml)
        {
            HandlerXML handlerXML = new HandlerXML();

            string msgIsValid =  handlerXML.ValidateXML(recordXml);

            if (msgIsValid == "Valid")
            {
                return Content(HttpStatusCode.OK, "");
            }

            return Content(HttpStatusCode.BadRequest, HandlerXML.responseError(msgIsValid, "400"), Configuration.Formatters.XmlFormatter);
        }


    }
}

