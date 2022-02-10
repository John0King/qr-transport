using KN.SafeCommunicationPlatform.Hubs;
using System.Buffers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddMvc();
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseEndpoints(builder =>
{
    builder.MapDefaultControllerRoute();
    builder.MapHub<SafeCommunicationHub>("xHub");
    //builder.Map("/ws", async context =>
    //{
    //    if (context.WebSockets.IsWebSocketRequest)
    //    {
    //        var ws = await context.WebSockets.AcceptWebSocketAsync();
    //        var mem = MemoryPool<byte>.Shared.Rent(2048);
    //        var result = await ws.ReceiveAsync(mem.Memory, context.RequestAborted);
    //        ws.SendAsync()
    //    }
    //});
});



app.Run();
