using Cocona.Builder;
using Cocona;
using MsixStoreTool.Commands;

CoconaAppBuilder? builder = CoconaApp.CreateBuilder();

//builder.Services.TryAddSingleton<IHtmlRenderer, HtmlRenderer>();

CoconaApp? app = builder.Build();

app.AddCommands<MsixCommand>();

app.Run();