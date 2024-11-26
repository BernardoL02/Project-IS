using SOMIOD.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace SOMIOD.Controllers
{
    public class ContainersController : ApiController
    {
        string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["SOMIOD.Properties.Settings.ConnectionToDB"].ConnectionString;

        [HttpGet]
        [Route("api/containers")]
        public IEnumerable<Container> GetAllContainers()
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
        [Route("api/containers/{id:int}")]
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
        [Route("api/containers")]
        public IHttpActionResult PostContainer([FromBody] Container c)
        {
            if (c == null)
            {
                return BadRequest("You need to provide the container info");
            }

            SqlConnection conn = null;

            try
            {
                conn = new SqlConnection(connectionString);
                conn.Open();

                SqlCommand cmd = new SqlCommand("INSERT INTO Container (Name, CreationDateTime, Parent) VALUES (@name, @creationDateTime, @parent)", conn);
                cmd.Parameters.AddWithValue("@name", c.Name);
                cmd.Parameters.AddWithValue("@creationDateTime", c.CreationDateTime);
                cmd.Parameters.AddWithValue("@parent", c.Parent);
                int nrows = cmd.ExecuteNonQuery();

                if (nrows <= 0)
                {
                    return BadRequest("Error inserting a new line into the bd");
                }

            }
            catch (Exception e)
            {

                Console.WriteLine(e.Message);

            }
            finally
            {
                conn.Close();
            }

            return Ok(c);
        }

    }
}