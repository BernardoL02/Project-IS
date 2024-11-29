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
namespace SOMIOD.Controllers
{
    public class AplicationsController : ApiController
    {
        //Declarar a string com o nome do file da bd
        string strConnection = System.Configuration.ConfigurationManager.ConnectionStrings["SOMIOD.Properties.Settings.ConnectionToDB"].ConnectionString;

        [HttpGet]
        [Route("api/somiod")]
        public IHttpActionResult GetApplications()
        {
            IEnumerable<string> headerValues;
            List<Application> applicationList = new List<Application>();
            string headerValue = null;
            SqlConnection conn = null;

            if (Request.Headers.TryGetValues("H", out headerValues))
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

                conn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return InternalServerError(ex);
            }

            if (applicationList.Count == 0)
            {
                return NotFound();
            }

            return Content(System.Net.HttpStatusCode.OK, HandlerXML.responseApplications(applicationList), Configuration.Formatters.XmlFormatter);
        }

        [HttpGet]
        [Route("api/somiod/{Name}")]
        public IHttpActionResult GetApplication(string Name)
        {
            IEnumerable<string> headerValues;
            string headerValue = null;

            if (Request.Headers.TryGetValues("H", out headerValues))
            {
                headerValue = headerValues.FirstOrDefault()?.ToUpper();
            }

            List<Application> applicationList = new List<Application>();
            SqlConnection conn = null;

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand command = new SqlCommand($"SELECT * FROM {headerValue}", conn);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        applicationList.Add(new Application
                        {
                            Id = (int)reader["Id"],
                            Name = (string)reader["Name"],
                            CreationDateTime = (DateTime)reader["CreationDateTime"]
                        });
                    }
                }

                conn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return InternalServerError(ex);
            }

            if (applicationList.Count == 0)
            {
                return NotFound();
            }

            return Content(System.Net.HttpStatusCode.OK, HandlerXML.responseApplications(applicationList), Configuration.Formatters.XmlFormatter);
        }

        /*[HttpGet]
        [Route("api/somiod/{appName}")]
        public IHttpActionResult GetApplicationContainers(string appName)
        {
            IEnumerable<string> headerValues;
            string tableCont = null;
            int appId;

            if (Request.Headers.TryGetValues("H", out headerValues))
            {
                    tableCont =  headerValues.FirstOrDefault()?.ToUpper();
                
            }

            List<Container> containerList = new List<Container>();
            SqlConnection conn = null;
            
            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand command = new SqlCommand("SELECT id FROM Application WHERE name = @name", conn);
                command.Parameters.AddWithValue("@name", appName);

                object res = command.ExecuteScalar();

                if (res == null || !int.TryParse(res.ToString(), out appId))
                {
                    return NotFound();
                }

                SqlCommand cmd = new SqlCommand($"SELECT name FROM {tableCont} WHERE parent = @id", conn);
                cmd.Parameters.AddWithValue("@id", appId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        containerList.Add(new Container
                        {
                            Name = (string)reader["name"]
                        });
                    }
                }

                conn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
               
                conn.Close();
            }

            if (containerList.Count == 0)
            {
                return NotFound();
            }

            return Content(System.Net.HttpStatusCode.OK, HandlerXML.responseContainers(containerList), Configuration.Formatters.XmlFormatter);
        }*/

        [HttpPost]
        [Route("api/somiod")]
        public IHttpActionResult PostApplication([FromBody] XElement applicationXml)
        {

            IEnumerable<string> headerValues;
            SqlConnection conn = null;
            int nRows;
            string headerValue = null;

            if (Request.Headers.TryGetValues("H", out headerValues))
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

        [HttpPut]
        [Route("api/somiod/{appName}")]
        public IHttpActionResult PutApplication(string appName, [FromBody] XElement applicationXml)
        {
            if (applicationXml == null)
            {
                return BadRequest("Invalid application data.");
            }

            IEnumerable<string> headerValues;
            string headerValue = null;
            if (Request.Headers.TryGetValues("H", out headerValues))
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
            
            if (Request.Headers.TryGetValues("H", out headerValues))
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
        }

    }
}
