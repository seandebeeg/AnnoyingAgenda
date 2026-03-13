using AnnoyingAgenda.Service;
using AnnoyingAgenda.Shared;

var builder = Host.CreateApplicationBuilder(args);

IHost host = Host.CreateDefaultBuilder(args)
  .ConfigureAppConfiguration((context, config) =>
  {
    config.AddJsonFile( 
      Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Annoying Agenda",
        "Settings.json"),
      optional: false, reloadOnChange: true);

    config.AddJsonFile(
      Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Annoying Agenda",
        "Lists.json"),
      optional: false, reloadOnChange: true);
  })
  .ConfigureServices((context, service) =>
  {
    service.Configure<Settings>(context.Configuration);
    service.Configure<List<ToDoList>>(context.Configuration.GetSection("AllLists"));
    service.AddHostedService<Worker>();
  })
  .Build();

host.Run();
