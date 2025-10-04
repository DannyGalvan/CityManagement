using CityProducer.Context;
using CityProducer.Interfaces;
using CityProducer.Models;
using CityProducer.Services;
using CityProducer.Validations.EventValidators;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CityProducer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Obtain the environment current (Development, Production, etc.)
            string environment = builder.Environment.EnvironmentName;

            // Configure the ConfigurationBuilder y load the configurations del archive appsettings.json
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true) // Load base file appsettings.json
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true) // Load environment-specific file
                .AddEnvironmentVariables()
                .Build();

            // Add services to the container.
            builder.Services.AddDbContext<DataContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("Default"));
            });
            builder.Services.AddControllers();
            builder.Services.AddSingleton<IProducerService, ProducerService>();
            builder.Services.AddScoped<IValidator<Events>, CreateEventValidator>();
            builder.Services.AddScoped<IValidator<BulkEvents>, BulkEventValidator>();


            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
           

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
