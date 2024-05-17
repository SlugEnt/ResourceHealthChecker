using SlugEnt.ResourceHealthChecker;
using SlugEnt.ResourceHealthChecker.SqlServer;

namespace SampleWebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Testing
            builder.Services.AddSingleton<HealthCheckProcessor>();

            builder.Services.AddTransient<IHealthCheckerFileSystem, HealthCheckerFileSystem>();
            builder.Services.AddHostedService<HealthCheckerBackgroundProcessor>();
            builder.Services.AddTransient<IHealthCheckerSQLServer, HealthCheckerSQLServer>();
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