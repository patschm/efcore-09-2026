using ACME.DataLayer.Interfaces;
using ACME.DataLayer.Repository.SqlServer;
using ACME.Frontend.WPF.Core.Interfaces;
using ACME.Frontend.WPF.UserControls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Windows;

namespace ACME.Frontend.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public IServiceProvider?  ServiceProvider { get; private set; }
    public IConfiguration?  Configuration { get; private set; }
    protected override void OnStartup(StartupEventArgs e)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, true);
        Configuration = builder.Build();

        var serviceCollection = new  ServiceCollection();
        ConfigureServices(serviceCollection);
        ServiceProvider = serviceCollection.BuildServiceProvider();
        
        var main = ServiceProvider.GetRequiredService<MainWindow>();
           
        main.Show();
    }

    private void ConfigureServices(ServiceCollection serviceCollection)
    {
        serviceCollection.AddDbContextFactory<ShopDatabaseContext>(opts => {
            opts.UseSqlServer(Configuration.GetConnectionString("SqlServerExpress"));
        });
        serviceCollection.AddTransient<IUnitOfWork, UnitOfWork>();
        
        serviceCollection.AddSingleton<IViewMediator, ViewMediator>();
        serviceCollection.AddTransient<MainViewModel>();
        serviceCollection.AddTransient<MainWindow>();
        serviceCollection.AddViewModels();
        serviceCollection.AddViewComponents();
    }
}
