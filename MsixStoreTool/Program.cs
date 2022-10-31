using Cocona.Builder;
using Cocona;
using MsixStoreTool.Commands;

CoconaAppBuilder? builder = CoconaApp.CreateBuilder();

CoconaApp? app = builder.Build();

app.AddCommands<MsixCommand>();

app.Run();