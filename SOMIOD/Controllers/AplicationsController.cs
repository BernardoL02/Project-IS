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
        string strConnection = System.Configuration.ConfigurationManager.ConnectionStrings["SOMIOD.Properties.Settings.ConnectionToDB"].ConnectionString;

        [HttpGet]
        [Route("api/somiod")]
        public IHttpActionResult GetApplications()
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
        [Route("api/somiod/{name}")]
        public IHttpActionResult GetContainers(string name)
        {
            IEnumerable<string> headerValues;
            string headerValue = null;

            if (Request.Headers.TryGetValues("H", out headerValues))
            {
                headerValue = headerValues.FirstOrDefault().ToUpper();
            }

            Application application = null;
            SqlConnection conn = null;
            
            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand command = new SqlCommand($"SELECT * FROM {headerValue} WHERE name = @name", conn);
                command.Parameters.AddWithValue("@name", name);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        application = new Application
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            CreationDateTime = reader.GetDateTime(2),
                        };
                    }
                }

                conn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (application == null)
            {
                return NotFound();
            }

            return Ok(application);
        }


        [HttpGet]
        [Route("api/somiod/applications/{id:int}/containers")]
        public IEnumerable<Container> GetApplicationContainers(int id)
        {
            List<Container> containers = new List<Container>();
            SqlConnection conn = null;
            SqlDataReader reader = null;

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand command = new SqlCommand("SELECT * FROM Container WHERE Parent = @application_id", conn);
                command.Parameters.AddWithValue("@application_id", id);

                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Container container = new Container
                    {
                        Id = (int)reader["Id"],
                        Name = (string)reader["Name"],
                        CreationDateTime = (DateTime)reader["CreationDateTime"],
                        Parent = (int)reader["Parent"]
                    };

                    containers.Add(container);
                }

                reader.Close();
                conn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                reader.Close();
                conn.Close();
            }

            return containers;
        }

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
        [Route("api/somiod/applications/{id:int}")]
        public IHttpActionResult PutApplication(int id, [FromBody] Application app)
        {
            SqlConnection conn = null;
            int afectedRows;

            if (app == null || string.IsNullOrEmpty(app.Name) || app.CreationDateTime == null)
            {
                return BadRequest("Invalid application data.");
            }

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand command = new SqlCommand("UPDATE Application SET Name = @name, CreationDateTime = @creationDateTime WHERE Id = @id", conn);
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@name", app.Name);
                command.Parameters.AddWithValue("@creationDateTime", app.CreationDateTime);

                afectedRows = command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {               
                return BadRequest("Error updating a new application!!!");
            }
            finally
            {
                conn.Close();
            }

            if (afectedRows > 0)
            {
                return GetApplications();
            }
            else
            {
                return BadRequest("Error updating a new application!!!");
            }
        }


        [HttpDelete]
        [Route("api/somiod/applications/{id:int}")]
        public IHttpActionResult DeleteApplication(int id)
        {
            Application app = null;
            SqlConnection conn = null;

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand selectCmd = new SqlCommand("SELECT * FROM Application WHERE id = @id", conn);
                selectCmd.Parameters.AddWithValue("@id", id);

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

                SqlCommand deleteCmd = new SqlCommand("DELETE FROM Application WHERE id = @id", conn);
                deleteCmd.Parameters.AddWithValue("@id", id);
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

            return Ok(app);
        }

    }
}
