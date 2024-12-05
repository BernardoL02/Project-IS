using SOMIOD.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Http;
using System.Xml.Linq;
using Container = SOMIOD.Models.Container;

namespace SOMIOD.Controllers
{

    public class ContainersController : ApiController
    {
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["SOMIOD.Properties.Settings.ConnectionToDB"].ConnectionString;

        /*
       [HttpGet]
        [Route("api/somiod/{appName}")]
        public IEnumerable<Container> GetRecords()
        {
            List<Container> containers = new List<Container>();
            SqlConnection conn = null;

            try
            {
                conn = new SqlConnection(connectionString);
                conn.Open();

                SqlCommand cmd = new SqlCommand("SELECT * FROM Container ORDER BY Id", conn);
                SqlDataReader reader = cmd.ExecuteReader();

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
            catch (Exception)
            {
                if (conn.State == System.Data.ConnectionState.Open)
                {

                    conn.Close();
                    throw;
                }
            }
            return containers;
        }

        
       [HttpGet]
       [Route("api/somiod/containers/{id:int}")]
       public IHttpActionResult GetContainer(int id)
       {
           Container container = null;
           SqlConnection conn = null;

           try
           {
               conn = new SqlConnection(connectionString);
               conn.Open();

               SqlCommand cmd = new SqlCommand("SELECT * FROM Container WHERE id = @id ORDER BY id", conn);
               cmd.Parameters.AddWithValue("@id", id);
               SqlDataReader reader = cmd.ExecuteReader();

               while (reader.Read())
               {

                   container = new Container
                   {
                       Id = (int)reader["Id"],
                       Name = (string)reader["Name"],
                       CreationDateTime = (DateTime)reader["CreationDateTime"],
                       Parent = (int)reader["Parent"]
                   };

               }
               reader.Close();
               conn.Close();
           }
           catch (Exception)
           {
               if (conn.State == System.Data.ConnectionState.Open)
               {

                   conn.Close();
                   throw;
               }

           }
           return Ok(container);
       }

        
        [HttpPost]
        [Route("api/somiod/{appName}")]
        public IHttpActionResult PostContainer(string appName, [FromBody] XElement applicationXml)
        {
            IEnumerable<string> headerValues;
            SqlConnection conn = null;
            string headerValue = null;
            int nrows = 0;
            string containerName = applicationXml.Element("name")?.Value;

            if (Request.Headers.TryGetValues("somiod-locate", out headerValues))
            {
                headerValue = headerValues.FirstOrDefault()?.ToUpper();
            }

            if (string.IsNullOrEmpty(containerName))
            {
                return BadRequest("Application XML must contain a valid 'name' element.");
            }

            try
            {
                conn = new SqlConnection(connectionString);
                conn.Open();

                // First SQL command to select the application Id
                using (SqlCommand sqlCommand = new SqlCommand("SELECT Id FROM Application WHERE Name = @name", conn))
                {
                    sqlCommand.Parameters.AddWithValue("@name", appName);

                    using (SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {

                            int appId = reader.GetInt32(0);
                            reader.Close(); 
                            using (SqlCommand cmd = new SqlCommand($"INSERT INTO {headerValue} (Name, Parent) VALUES (@name, @parent)", conn))
                            {
                                cmd.Parameters.AddWithValue("@name", containerName);
                                cmd.Parameters.AddWithValue("@parent", appId);

                                nrows = cmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            reader.Close();
                            return NotFound(); 
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return BadRequest("Error creating a new container!!!");
            }
            finally
            {
                if (conn != null)
                {
                    conn.Close();
                }
            }

            if (nrows > 0)
            {
                return StatusCode(HttpStatusCode.Created); 
            }
            else
            {
                return BadRequest("Error creating a new container!!!");
            }
        }

        

        [HttpPut]
        [Route("api/somiod/containers/{id:int}")]
        public IHttpActionResult PutApplication(int id, [FromBody] Container container)
        {
            SqlConnection conn = null;
            int afectedRows;

            if (container == null)
            {
                return BadRequest("Invalid application data.");
            }

            try
            {
                conn = new SqlConnection(connectionString);
                conn.Open();

                SqlCommand command = new SqlCommand("UPDATE Container SET Name = @name, CreationDateTime = @creationDateTime, Parent = @parent WHERE Id = @id", conn);
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@name", container.Name);
                command.Parameters.AddWithValue("@creationDateTime", container.CreationDateTime);
                command.Parameters.AddWithValue("@parent", container.Parent);


                afectedRows = command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return BadRequest("Error updating a new container!!!");
            }
            finally
            {
                conn.Close();
            }

            if (afectedRows > 0)
            {
                //GetContainer(id);
                return null;
            }
            else
            {
                return BadRequest("Error updating a new container!!!");
            }
        }

        [HttpDelete]
        [Route("api/somiod/containers/{id:int}")]
        public IHttpActionResult DeleteApplication(int id)
        {
            Container container = null;
            SqlConnection conn = null;

            try
            {
                conn = new SqlConnection(connectionString);
                conn.Open();

                SqlCommand selectCmd = new SqlCommand("SELECT * FROM Container WHERE id = @id", conn);
                selectCmd.Parameters.AddWithValue("@id", id);

                SqlDataReader reader = selectCmd.ExecuteReader();
                if (reader.Read())
                {
                    container = new Container
                    {
                        Id = (int)reader["Id"],
                        Name = (string)reader["Name"],
                        CreationDateTime = (DateTime)reader["CreationDateTime"],
                        Parent = (int)reader["Parent"]
                    };
                }

                reader.Close();

                if (container == null)
                {
                    return BadRequest("Error deleting a container!!!");
                }

                SqlCommand deleteCmd = new SqlCommand("DELETE FROM Container WHERE id = @id", conn);
                deleteCmd.Parameters.AddWithValue("@id", id);
                deleteCmd.ExecuteNonQuery();

                conn.Close();
            }
            catch (Exception)
            {
                if (conn.State == System.Data.ConnectionState.Open)
                {
                    conn.Close();
                }
                throw;
            }

            return Ok(container);
        }
        */
    }
}