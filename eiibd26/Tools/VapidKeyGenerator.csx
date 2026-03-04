// Simple console app to generate VAPID keys using WebPush library
// Run: dotnet script VapidKeyGenerator.csx

#r "nuget: WebPush-NetCore, 1.0.2"

using WebPush;

var vapidKeys = VapidHelper.GenerateVapidKeys();

Console.WriteLine("=== VAPID KEYS GENERATED ===");
Console.WriteLine();
Console.WriteLine("Public Key:");
Console.WriteLine(vapidKeys.PublicKey);
Console.WriteLine();
Console.WriteLine("Private Key:");
Console.WriteLine(vapidKeys.PrivateKey);
Console.WriteLine();
Console.WriteLine("=== ADD TO appsettings.json ===");
Console.WriteLine();
Console.WriteLine(@"{
  ""VapidKeys"": {
    ""PublicKey"": """ + vapidKeys.PublicKey + @""",
    ""PrivateKey"": """ + vapidKeys.PrivateKey + @""",
    ""Subject"": ""mailto:admin@eiibd.com""
  }
}");
