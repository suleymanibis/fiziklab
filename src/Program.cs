using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PhysicsLab.Safari;

var app = WebAssemblyHostBuilder.CreateDefault(args);
app.RootComponents.Add<App>("#app");
await app.Build().RunAsync();
