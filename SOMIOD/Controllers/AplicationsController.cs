﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Web;
using System.Web.Http;
using System.Web.Http.Results;
using SOMIOD.Models;
using static System.Net.Mime.MediaTypeNames;
using Application = SOMIOD.Models.Application;

namespace SOMIOD.Controllers
{
    public class AplicationsController : ApiController
    {
        string strConnection = System.Configuration.ConfigurationManager.ConnectionStrings["SOMIOD.Properties.Settings.ConnectionToDB"].ConnectionString;


        [HttpGet]
        [Route("api/somiod/applications")]
        public IEnumerable<Application> GetAllApplications()
        {
            List<Application> applications = new List<Application>();
            SqlConnection conn = null;
            SqlDataReader reader = null;

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand command = new SqlCommand("SELECT * FROM Application ORDER BY Id", conn);

                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Application application = new Application
                    {
                        Id = (int)reader["Id"],
                        Name = (string)reader["Name"],
                        CreationDateTime = (DateTime)reader["CreationDateTime"]
                    };

                    applications.Add(application);
                }
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

            return applications;
        }


        [HttpGet]
        [Route("api/somiod/applications/{id:int}")]
        public IHttpActionResult GetApplication(int id)
        {
            Application application = null;
            SqlConnection conn = null;

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand command = new SqlCommand("SELECT * FROM Application WHERE id = @id", conn);
                command.Parameters.AddWithValue("@id", id);

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
        [Route("api/somiod/applications")]
        public IHttpActionResult PostApplication([FromBody] Application newApp)
        {
            SqlConnection conn = null;
            int afectedRows;

            if (newApp == null || string.IsNullOrEmpty(newApp.Name))
            {
                return BadRequest("Invalid application data.");
            }

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand command = new SqlCommand("INSERT INTO Application (Name) VALUES(@name)", conn);
                command.Parameters.AddWithValue("@name", newApp.Name);

                afectedRows = command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return BadRequest("Error inserting a new application!!!");
            }
            finally
            {
                conn.Close();
            }

            if (afectedRows > 0)
            {
                return StatusCode(HttpStatusCode.Created);
            }
            else
            {
                return BadRequest("Error inserting a new application!!!");
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
                return GetApplication(app.Id);
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
