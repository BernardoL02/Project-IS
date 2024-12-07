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


        //-------------------------------------------------------------------------------------
        //------------------------------------- Containers ------------------------------------
        //------------------------------------------------------------------------------------- 

        [HttpPost]
        [Route("api/somiod/{application}")]
        public IHttpActionResult PostContainer(string application, [FromBody] XElement containerXml)
        {
            SqlConnection conn = null;
            int affectedfRows = -1;

            if (containerXml == null)
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError("Invalid XML body.", "400"), Configuration.Formatters.XmlFormatter);
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

            Application app = this.verifyApplicationExists(application);

            if (app == null)
            {
                return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Application was not found.", "404"), Configuration.Formatters.XmlFormatter);
            }

            if (this.verifyContainerExists(container) == null)
            {
                return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Container was not found.", "404"), Configuration.Formatters.XmlFormatter);
            }

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

            if (containerXml == null)
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError("Invalid XML body.", "400"), Configuration.Formatters.XmlFormatter);
            }

            Application app = this.verifyApplicationExists(application);
            if (app == null)
            {
                return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Application was not found.", "404"), Configuration.Formatters.XmlFormatter);
            }

            Container cont = this.verifyContainerExists(container);
            if (cont == null)
            {
                return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Container was not found.", "404"), Configuration.Formatters.XmlFormatter);
            }

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

            Application app = this.verifyApplicationExists(application);
            if (app == null)
            {
                return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Application was not found.", "404"), Configuration.Formatters.XmlFormatter);
            }

            Container cont = this.verifyContainerExists(container);
            if (cont == null)
            {
                return Content(HttpStatusCode.NotFound, HandlerXML.responseError("Container was not found.", "404"), Configuration.Formatters.XmlFormatter);
            }

            // TODO Verificar se o Container Ã© filho da app

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



        //-------------------------------------------------------------------------------------
        //--------------------------------------- Record --------------------------------------
        //------------------------------------------------------------------------------------- 

        private IHttpActionResult PostRecord(string application, string container, [FromBody] XElement containerXml)
        {
            
            SqlConnection conn = null;
            int affectedfRows = 0;


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
                    Name = containerXml.Element("Name")?.Value,
                    Content = containerXml.Element("Content")?.Value
                };

                //TODO --> Validar que o XML tem o Name e o Content

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



        //-------------------------------------------------------------------------------------
        //------------------------------------ Notifications ----------------------------------
        //-------------------------------------------------------------------------------------











        //-------------------------------------------------------------------------------------
        //--------------------------------- Suport Functions ----------------------------------
        //------------------------------------------------------------------------------------- 

        [HttpPost]
        [Route("api/somiod/{application}/{container}")]
        public IHttpActionResult PostRecordOrNotification(string application, string container, [FromBody] XElement recordXml)
        {
            if (recordXml == null)
            {
                return Content(HttpStatusCode.BadRequest, HandlerXML.responseError("Invalid XML body.", "400"), Configuration.Formatters.XmlFormatter);
            }

            try
            {
                string rootName = recordXml.Name.LocalName;

                if (rootName.Equals("Record", StringComparison.OrdinalIgnoreCase))
                {
                    
                    return PostRecord(application, container, recordXml);
                }

                if (rootName.Equals("Notification", StringComparison.OrdinalIgnoreCase))
                {
                    return Content(HttpStatusCode.OK, "Notification received.");
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

