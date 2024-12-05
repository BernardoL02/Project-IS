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
namespace SOMIOD.Controllers
{
    public class ApplicationController : ApiController
    {
        //Declarar a string com o nome do file da bd
        string strConnection = System.Configuration.ConfigurationManager.ConnectionStrings["SOMIOD.Properties.Settings.ConnectionToDB"].ConnectionString;


        







































        /*
        #region CRUD GET GERAL
        [HttpGet] // é preciso o somiod locate
        [Route("api/somiod/{application}")]
        public IHttpActionResult GetContainerRecordNotificationByApplicationName(string application)
        {
            IEnumerable<string> headerValues;
            string somiodLocateHeaderValue = null;


            if (Request.Headers.TryGetValues("somiod-locate", out headerValues))
            {
                somiodLocateHeaderValue = headerValues.FirstOrDefault()?.ToUpper();
            }

            if (string.IsNullOrEmpty(somiodLocateHeaderValue))
            {
                try
                {
                    List<string> NamesList = new List<string>();
                    Application applicationInfo = null;
                    using (SqlConnection conn = new SqlConnection(strConnection))
                    {
                        string query = "SELECT id,name,CreationDateTime from Application where name = @application";

                        conn.Open();

                        using (SqlCommand command = new SqlCommand(query, conn))
                        {
                            command.Parameters.AddWithValue("@application", application);

                            using (SqlDataReader sqlReader = command.ExecuteReader())
                            {
                                while (sqlReader.Read())
                                {
                                    applicationInfo = new Application
                                    {
                                        Id = sqlReader.GetInt32(0),
                                        Name = sqlReader.GetString(1),
                                        CreationDateTime = sqlReader.GetDateTime(2),
                                    };
                                }
                            }
                        }
                    }

                    return Content(System.Net.HttpStatusCode.OK, HandlerXML.responseApplication(applicationInfo), Configuration.Formatters.XmlFormatter);
                }
                catch (Exception ex)
                {

                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    return BadRequest("Error retrieving application names");
                }
            }else{

                try
                {
                    List<string> NamesList = new List<string>();

                    using (SqlConnection conn = new SqlConnection(strConnection))
                    {
                        string query = null;
                        if (somiodLocateHeaderValue == "CONTAINER")
                        {
                            query = "SELECT Container.name FROM Container JOIN Application ON Container.parent = Application.id WHERE Application.name = @application";

                        }
                        if (somiodLocateHeaderValue == "RECORD")
                        {
                            query = "SELECT Record.name FROM Record JOIN container ON Record.parent = container.id JOIN application ON container.parent = application.id WHERE application.name = @application";
                        }
                        if (somiodLocateHeaderValue == "NOTIFICATION")
                        {
                            query = "SELECT Notification.name FROM Notification JOIN Notification ON Notification.parent = container.id JOIN application ON container.parent = application.id WHERE application.name = @application";
                        }

                        conn.Open();

                        using (SqlCommand command = new SqlCommand(query, conn))
                        {
                            command.Parameters.AddWithValue("@application", application);

                            using (SqlDataReader sqlReader = command.ExecuteReader())
                            {
                                while (sqlReader.Read())
                                {
                                    string name = sqlReader.GetString(0);
                                    NamesList.Add(name);
                                }
                            }
                        }
                    }

                    XmlDocument xmlResponse = HandlerXML.responseContainers(NamesList);

                    return Content(System.Net.HttpStatusCode.OK, xmlResponse, Configuration.Formatters.XmlFormatter);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    return BadRequest("Error retrieving names");
                }
            }
        }
        #endregion


        [HttpGet]
        [Route("api/somiod")]
        public IHttpActionResult GetApplications()
        {
            IEnumerable<string> headerValues;
            List<Application> applicationList = new List<Application>();
            string headerValue = null;
            SqlConnection conn = null;

            if (Request.Headers.TryGetValues("somiod-locate", out headerValues))
            {
                headerValue = headerValues.FirstOrDefault()?.ToUpper(); // GET ao nome da tabela que vem no header
            }

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand command = new SqlCommand($"SELECT name FROM {headerValue}", conn);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        applicationList.Add(new Application
                        {
                            Name = (string)reader["Name"]
                        });
                    }
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return InternalServerError(ex);
            }
            finally
            {
                conn.Close();
            }

            if (applicationList.Count == 0)
            {
                return NotFound();
            }

            return Content(System.Net.HttpStatusCode.OK, HandlerXML.responseApplications(applicationList), Configuration.Formatters.XmlFormatter);
        }

        [HttpPost]
        [Route("api/somiod")]
        public IHttpActionResult PostApplication([FromBody] XElement applicationXml)
        {

            IEnumerable<string> headerValues;
            SqlConnection conn = null;
            int nRows;
            string headerValue = null;

            if (Request.Headers.TryGetValues("somiod-locate", out headerValues))
            {
                headerValue = headerValues.FirstOrDefault().ToUpper();
            }

            if (applicationXml == null)
            {
                return BadRequest("Invalid XML body.");
            }

            try
            {
                var application = new
                {
                    Name = applicationXml.Element("name")?.Value
                };

                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand command = new SqlCommand($"INSERT INTO {headerValue}(Name) VALUES (@name)", conn);
                command.Parameters.AddWithValue("@name", application.Name);

                nRows = command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return BadRequest("Error creating application.");
            }
            finally
            {
                if (conn != null)
                {
                    conn.Close();
                }
            }

            if (nRows > 0)
            {
                return StatusCode(HttpStatusCode.Created);
            }
            else
            {
                return BadRequest("Error creating application.");
            }
        }

        /*[HttpPut]
        [Route("api/somiod/{appName}")]
        public IHttpActionResult PutApplication(string appName, [FromBody] XElement applicationXml)
        {
            if (applicationXml == null)
            {
                return BadRequest("Invalid application data.");
            }

            IEnumerable<string> headerValues;
            string headerValue = null;
            if (Request.Headers.TryGetValues("somiod-locate", out headerValues))
            {
                headerValue = headerValues.FirstOrDefault()?.ToUpper();
            }

            if (string.IsNullOrEmpty(headerValue))
            {
                return BadRequest("Missing or invalid header value.");
            }

            string applicationName = applicationXml.Element("name")?.Value;
            if (string.IsNullOrEmpty(applicationName))
            {
                return BadRequest("Application XML must contain a valid 'name' element.");
            }

            using (SqlConnection conn = new SqlConnection(strConnection))
            {
                try
                {
                    conn.Open();

                    string updateQuery = $"UPDATE {headerValue} SET Name = @name WHERE Name = @appName";
                    using (SqlCommand updateCommand = new SqlCommand(updateQuery, conn))
                    {
                        updateCommand.Parameters.AddWithValue("@name", applicationName);
                        updateCommand.Parameters.AddWithValue("@appName", appName);

                        int affectedRows = updateCommand.ExecuteNonQuery();
                        if (affectedRows == 0)
                        {
                            return NotFound();
                        }
                    }

                    string selectQuery = $"SELECT * FROM {headerValue} WHERE Name = @name";
                    using (SqlCommand selectCommand = new SqlCommand(selectQuery, conn))
                    {
                        selectCommand.Parameters.AddWithValue("@name", applicationName);

                        using (SqlDataReader reader = selectCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                XElement updatedXml = new XElement("Application", new XElement("Name", reader["Name"]));

                                return Content(System.Net.HttpStatusCode.OK, updatedXml, Configuration.Formatters.XmlFormatter);
                            }
                        }
                    }

                    return NotFound();
                }
                catch (Exception ex)
                {
                    return InternalServerError(ex);
                }
                finally
                {
                    if (conn != null)
                    {
                        conn.Close();
                    }
                }
            }
        }

        [HttpDelete]
        [Route("api/somiod/{appName}")]
        public IHttpActionResult DeleteApplication(string appName)
        {
            IEnumerable<string> headerValues;
            string headerValue = null;
            Application app = null;
            SqlConnection conn = null;

            if (Request.Headers.TryGetValues("somiod-locate", out headerValues))
            {
                headerValue = headerValues.FirstOrDefault().ToUpper();
            }

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand selectCmd = new SqlCommand($"SELECT * FROM {headerValue} WHERE Name = @name", conn);
                selectCmd.Parameters.AddWithValue("@name", appName);

                SqlDataReader reader = selectCmd.ExecuteReader();
                if (reader.Read())
                {
                    app = new Application
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        CreationDateTime = reader.GetDateTime(2),
                    };
                }

                reader.Close();

                if (app == null)
                {
                    return BadRequest("Error deleting a application!!!");
                }

                SqlCommand deleteCmd = new SqlCommand($"DELETE FROM {headerValue} WHERE Name = @name", conn);
                deleteCmd.Parameters.AddWithValue("@name", appName);
                deleteCmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                return BadRequest("Error deleting a new application!!!");
            }
            finally
            {
                conn.Close();
            }

            return Content(System.Net.HttpStatusCode.OK, app, Configuration.Formatters.XmlFormatter);
        }*/

    }
}
