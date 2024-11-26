﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Http;
using SOMIOD.Models;
using static System.Net.Mime.MediaTypeNames;
using Application = SOMIOD.Models.Application;

namespace SOMIOD.Controllers
{
    public class AplicationsController : ApiController
    {
        string strConnection = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\josem\source\repos\Project-IS\SOMIOD\App_Data\Database.mdf;Integrated Security=True";


        [HttpGet]
        [Route("api/applications")]
        public IEnumerable<Application> GetAllApplications()
        {
            List<Application> applications = new List<Application>();
            SqlConnection conn = null;
          
            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand command = new SqlCommand("SELECT * FROM Application ORDER BY Id", conn);

                SqlDataReader reader = command.ExecuteReader();
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

                reader.Close();
                conn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return applications;
        }


        [HttpGet]
        [Route("api/applications/{id:int}")]
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
                            CreationDateTime = DateTime.Parse(reader.GetString(2)),
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
        [Route("api/applications/{id:int}/containers")]
        public IEnumerable<Container> GetApplicationContainers(int id)
        {
            List<Container> containers = new List<Container>();
            SqlConnection conn = null;

            try
            {
                conn = new SqlConnection(strConnection);
                conn.Open();

                SqlCommand command = new SqlCommand("SELECT * FROM Containers WHERE Parent = @application_id", conn);
                command.Parameters.AddWithValue("@id", id);

                SqlDataReader reader = command.ExecuteReader();
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

            return containers;
        }

        [HttpPost]
        [Route("api/applications")]
        public IHttpActionResult PostProduct([FromBody] Application newApp)
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

                SqlCommand command = new SqlCommand("INSERT INTO Application(Name,CreationDateTime) VALUES(@name,@creationDateTime)", conn);
                command.Parameters.AddWithValue("@name", newApp.Name);
                command.Parameters.AddWithValue("@creationDateTime", newApp.CreationDateTime);

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
                return Ok(newApp);
            }
            else
            {
                return BadRequest("Error inserting a new application!!!");
            }
        }




    }
}
