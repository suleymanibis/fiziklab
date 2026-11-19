# FizikLab

Fizik I mekanik laboratuvarındaki beş deney için ölçüm, hesaplama ve grafik uygulaması. Ölçümler tarayıcıda saklanır ve grafikler PNG olarak indirilebilir.

Yayımlanan dosyalar docs/, Blazor WebAssembly kaynakları src/ dizinindedir. .NET 10 SDK ile dotnet publish src/FizikLab.Safari.csproj -c Release komutunu kullanarak derleyebilirsin. Çevrimdışı paketi hazırlamak için yayımlanan wwwroot içeriğini src/dist/ içine al ve node src/write-offline-manifest.mjs çalıştır.
