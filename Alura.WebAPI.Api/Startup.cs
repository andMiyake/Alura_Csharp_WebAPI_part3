using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alura.ListaLeitura.Api.Formatters;
using Alura.ListaLeitura.Modelos;
using Alura.ListaLeitura.Persistencia;
using Alura.WebAPI.Api.Filtros;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Swagger;

namespace Alura.WebAPI.Api
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration config)
        {
            Configuration = config;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<LeituraContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("ListaLeitura"));
            });

            services.AddTransient<IRepository<Livro>, RepositorioBaseEF<Livro>>();

            services.AddMvc(options =>
            {
                options.OutputFormatters.Add(new LivroCsvFormatter());
                options.Filters.Add(typeof(ErrorResponseFilter));
            }).AddXmlSerializerFormatters();

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "JwtBearer";    //Esquema de autentificação padrão
                options.DefaultChallengeScheme = "JwtBearer";
            })
            .AddJwtBearer("JwtBearer", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,      //Validando criador do Token
                    ValidateAudience = true,    //Validando quem pedi o token
                    ValidateLifetime = true,    //Validando a expiração
                    ValidateIssuerSigningKey = true,    //Validando chave
                    IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("alura-webapi-authentication-valid")),   //Chave de Assinatura usada pelo Issuer, chave usada para validar no Juiz
                    ClockSkew = TimeSpan.FromMinutes(5),
                    ValidIssuer = "Alura.WebApp",
                    ValidAudience = "Postman",
                };
            });

            services.AddApiVersioning();

            //services.AddApiVersioning(options =>
            //{
            //    options.ApiVersionReader = ApiVersionReader.Combine(
            //            new QueryStringApiVersionReader("api-version"),
            //            new HeaderApiVersionReader("api-version")
            //        );
            //});

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new Info { 
                    Title = "Livros API", 
                    Description = "Documentação da API de livros", 
                    Version = "1.0" 
                });

                options.SwaggerDoc("v2", new Info { 
                    Title = "Livros API",
                    Description = "Documentação da API de livros",
                    Version = "2.0" 
                });

                options.EnableAnnotations();

                options.AddSecurityDefinition("Bearer", new ApiKeyScheme
                {
                    Name = "Authorization",
                    In = "header",
                    Type = "apiKey",
                    Description = "Autenticação Bearer via JWT"
                });

                options.AddSecurityRequirement(
                    new Dictionary<string, IEnumerable<string>>
                    {
                        { "Bearer", new string[] {} }
                    });

                options.DescribeAllEnumsAsStrings();        //Mostrando o valor dos enumerados no Swagger
                options.DescribeStringEnumsInCamelCase();   //Mostrando o valor dos enumerados no Swagger

                options.OperationFilter<AuthResponsesOperationFilter>();

                options.DocumentFilter<TagDescriptionsDocumentFilter>();
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication();

            app.UseMvc();

            app.UseSwagger();

            app.UseSwaggerUI(c => 
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Versão 1.0");
                c.SwaggerEndpoint("/swagger/v2/swagger.json", "Versão 2.0");
            });
        }
    }
}
