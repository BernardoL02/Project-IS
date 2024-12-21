using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Publisher
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            _ = InitializeApplication();
        }

        private async Task InitializeApplication()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync("https://localhost:44322/api/somiod/Switch");

                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        XElement xml = new XElement("Application",
                            new XElement("Name", "Switch")

                        );

                        var content = new StringContent(xml.ToString(), Encoding.UTF8, "application/xml");

                        response = await client.PostAsync("https://localhost:44322/api/somiod", content);

                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine("Application 'Switch' created successfully.");
                        }
                        else
                        {
                            string errorMessage = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"Failed to create 'Switch': {response.StatusCode} - {errorMessage}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing application: {ex.Message}");
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {

            XElement xml = new XElement("Record",
                new XElement("Name", "Record1"),
                new XElement("Content", "on")
            );

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/xml");

                    Console.WriteLine($"XML Enviado: {xml}");

                    var content = new StringContent(xml.ToString(), Encoding.UTF8, "application/xml");

                    HttpResponseMessage response = await client.PostAsync("https://localhost:44322/api/somiod/Lighting/light_bulb", content);

                    if (response.IsSuccessStatusCode) 
                    {
                        MessageBox.Show("Record criado com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        string errorMessage = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Erro ao executar a operação: {response.StatusCode} - {errorMessage}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao conectar ao servidor: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {

            XElement xml = new XElement("Record",
                new XElement("Name", "Record2"),
                new XElement("Content", "off")
            );

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/xml");

                    Console.WriteLine($"XML Enviado: {xml}");

                    var content = new StringContent(xml.ToString(), Encoding.UTF8, "application/xml");

                    HttpResponseMessage response = await client.PostAsync("https://localhost:44322/api/somiod/Lighting/light_bulb", content);

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Record criado com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        string errorMessage = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Erro ao executar a operação: {response.StatusCode} - {errorMessage}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao conectar ao servidor: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
