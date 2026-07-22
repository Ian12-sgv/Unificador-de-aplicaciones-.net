using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using log4net;
using log4net.Config;
using log4net.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebServicesApiNetCor.Util;

namespace WebServicesApiNetCor
{
    public class Startup
    {
        public static ILoggerRepository repository { get; set; }


        public Startup(IConfiguration configuration)//IConfiguration configuration)
        {
            try
            {
                Configuration = configuration;
                repository = LogManager.CreateRepository("NETCoreRepository");
                XmlConfigurator.Configure(repository, new FileInfo("log4net.config"));
                SDKProperties.LogRepository = repository; // Variables estáticas definidas en la biblioteca de clases
            }
            catch (Exception fail)
            {
                Console.WriteLine("Hubo error al iniciar aplicativo: " + fail.Message, "ERROR");
            }
        }


       public IConfiguration Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
           
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1",  new Microsoft.OpenApi.Models.OpenApiInfo { Title = "My API", Version = "v1" });
            });
            services.AddSingleton(_ => Configuration);
        }
       


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var log = LogManager.GetLogger(repository.Name, typeof(Startup));
            try
            {
                log.Info("EJECUTANDOSE");
                Directory.SetCurrentDirectory(env.ContentRootPath); //New
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                    c.RoutePrefix = string.Empty;
                });
            }
            else
            {
                app.UseHsts();
            }
                //Nombre App
                log.Info("APP: " + Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyTitleAttribute>().Title);
                //Versión del app
                var version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
                log.Info("VERSION: " + version);
                //Sistema Operativo Actual
                string osPlatform = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
                log.Info("SISTEMA OPERATIVO: " + osPlatform);
                log.Info("BD: " + ServidorConeccion() + @"\" + BDConeccion());
                //Ambiente
                // log.Info("CONFIGURACION: " + Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyConfigurationAttribute>().Configuration);
                //Ip usada
                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        log.Info("IP LOCAL: " + ip.ToString());
                        break;
                    }
                }      
                app.UseHttpsRedirection();

            app.UseMvc();
            }
            catch (Exception fail)
            {
                log.Info("Hubo el siguiente error:" + fail.Message);
            }
        }

        private string ServidorConeccion()
        {
            string valor = "";
            try
            {
                string[] array = Modulo.connectionString.Split(';');
                // valor = Modulo.connectionString.Substring(0, Modulo.connectionString.IndexOf(";"));
                valor = array[0].Replace("Server=", "");
            }
            catch
            {
                valor = "";
            }
            return valor;
        }

        private string BDConeccion()
        {
            string valor = "";
            try
            {
                string[] array = Modulo.connectionString.Split(';');
                //valor = Modulo.connectionString.Substring(Modulo.connectionString.IndexOf(";") + 1, Modulo.connectionString.IndexOf(";") + 1);
                valor = array[1].Replace("Database=", "");
            }
            catch
            {
                valor = "";
            }
            return valor;
        }

    }
}
