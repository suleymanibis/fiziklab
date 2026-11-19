using System;
using System.Collections.Generic;
using System.Linq;

namespace PhysicsLab
{
    internal static partial class Mechanics
    {
        private const double G = 9.8, Gcgs = 980, Tau = 2 * Math.PI;
        private static readonly int[] NoColumns = new int[0];

        public static Experiment[] Create()
        {
            var springs = Enumerable.Range(1, 3).Select(i => new Group(
                "Tablo 1.4." + i + " · Yay " + i,
                "x₁ konumunu cetvelden oku. Cetvel koordinatı aşağı doğru artmalı.",
                "m (g)|x₁ (cm)", 5, true).Seed(0, "10", "20", "30", "40", "50")).ToList();
            springs.Add(new Group("Yay enerjisi ve salınım",
                "Δx ilk = x bırakma − x denge. Yukarı kaldırdıysan negatif, aşağı çektiysen pozitif gir. x son, yayın durduğu cetvel konumu. İlk 10T zorunlu.",
                "m (g)|x denge (cm)|Δx ilk (cm)|x son (cm)|10T ① (s)|10T ② (s)|10T ③ (s)", 3, true)
                .Seed(0, "10", "10", "10").Seed(2, "-1,5", "-2", "-2,5"));

            const string forceHeaders = "F₁ (N veya g)|φ₁ (°)|F₂ (N veya g)|φ₂ (°)|F₃ (N veya g)|φ₃ (°)";
            const string angleHint = "φ açılarını masanın +x yönünden saat yönünün tersine oku. 0° sağ, 90° yukarı. Gram seçtiysen askı dahil toplam kütleyi gir.";
            var force120 = new Group("I · Makaralar 120° aralıklı", angleHint, forceHeaders, 1)
                .Seed(1, "30").Seed(3, "150").Seed(5, "270");
            var reverse = new Group("Tablo 4.2 · Tersinir sarkaç",
                "h, deliğin kütle merkezine pozitif uzaklığı. Her iki taraf için 5 salınım süresini saniye cinsinden gir. Ölçülmeyen taraf boş kalabilir.",
                "h (cm)|Sol 5τ (s)|Sağ 5τ (s)", 18, true);
            const string newtonHeaders = "m (g)|mₐ (g)|t₁ (s)|t₂ (s)|t₃ (s)";
            return new[] {
                new Experiment("Basit harmonik hareket", "10–12",
                    "Üç yayın kuvvet–uzama eğiminden yay sabitlerini bul. Bırakma ve durma konumlarıyla enerji değişimlerini ve ölçülen salınım periyodunu hesapla.",
                    new[] { new Option("Yay 1 · yüksüz x₀ (cm)"), new Option("Yay 2 · yüksüz x₀ (cm)"),
                        new Option("Yay 3 · yüksüz x₀ (cm)"), new Option("Enerji deneyi · yay no (1–3)", "1") }, Spring, springs.ToArray()),
                new Experiment("Kuvvetlerin bileşkesi", "13–16",
                    "Üç makara düzeninde kuvvetleri bileşenlerine ayır, bileşkeyi ve denge sapmasını bul. Her ölçüm için vektörleri ve sinüs bağıntısını incele.",
                    new[] { new Option("Girdi birimi · 1 = N, 2 = gram", "1"), new Option("Kuvvet belirsizliği ΔF (N) · isteğe bağlı"),
                        new Option("Açı belirsizliği Δφ (°) · isteğe bağlı") }, Forces,
                    force120, new Group("Tablo 2.2.1 · Sabit açılar", angleHint, forceHeaders, 3),
                    new Group("Tablo 2.3.1 · Sabit kuvvetler", angleHint, forceHeaders, 3)),
                new Experiment("Serbest düşme", "17–18",
                    "Her yükseklikte üç düşme süresini gir. Ortalama süre, h–t ve h–t² grafikleri ile eğimden yerçekimi ivmesi hesaplanır.",
                    new[] { new Option("Referans g (m/s²)", "9,8") }, FreeFall,
                    new Group("Tablo 3.1 · Düşme süreleri",
                        "Sayaç milisaniye gösteriyorsa 1000’e böl. Örneğin 202 ms için 0,202 s yaz.",
                        "h (cm)|t₁ (s)|t₂ (s)|t₃ (s)", 8, true).Seed(0, "40", "35", "30", "25", "20", "15", "10", "5")),
                new Experiment("Basit ve tersinir sarkaç", "19–22",
                    "Basit sarkaçta L–τ² eğimini ve ölçüm belirsizliğini, tersinir sarkaçta ortak periyot çizgisinden eşdeğer uzunluğu ve g’yi bul.",
                    new[] { new Option("Referans g (m/s²)", "9,8"), new Option("Yarıçap belirsizliği Δr (cm) · isteğe bağlı"),
                        new Option("Ortak periyot τ (s) · boşsa otomatik") }, Pendulum,
                    new Group("Tablo 4.1 · Basit sarkaç",
                        "ℓ değerlerini askıdan kürenin başlangıcına kadar ölç. r kürenin yarıçapı. A ve B karşı yönleri gösterir. Dört süre zorunlu. B ölçüldüyse ayrıca gir.",
                        "ℓ₁ (cm)|ℓ₂ (cm)|ℓ₃ (cm)|ℓ₄ (cm)|r (cm)|A₁ 5τ (s)|A₂ 5τ (s)|A₃ 5τ (s)|A₄ 5τ (s)|B₁ 5τ (s)|B₂ 5τ (s)|B₃ 5τ (s)|B₄ 5τ (s)", 5, true),
                    reverse),
                new Experiment("Newton’un ikinci yasası", "23–25",
                    "Işık kapılarındaki hızları ve ortalama ivmeyi hesapla. Sabit toplam kütlede kuvvet etkisini, sabit asılı kütlede ise kızak kütlesinin etkisini incele.",
                    new[] { new Option("Kızağın etkin uzunluğu L (cm)") }, Newton,
                    new Group("Tablo 5.1 · Toplam kütle sabit",
                        "m kızak, mₐ askı dahil asılı kütle. t₁ ve t₂ kapıda kalma, t₃ iki kapı arasındaki geçiş süresi. Beş farklı mₐ kullan.",
                        newtonHeaders, 5),
                    new Group("Tablo 5.2 · Asılı kütle sabit",
                        "mₐ sabitken en az beş farklı kızak kütlesi gir. t₃, iki kapı arasındaki geçiş süresidir.",
                        newtonHeaders, 5))
            };
        }

        private static int[] Cols(int length) { return Enumerable.Range(0, length).ToArray(); }
        private static double Error(double experimental, double reference)
        {
            var difference = Math.Abs(experimental - reference);
            return difference <= Math.Abs(reference) * 2e-15 ? 0 : 100 * difference / Math.Abs(reference);
        }
        private static string N(double value) { return Format.Number(value); }
        private static Point[] Curve(double begin, double end, Func<double, double> f)
        {
            return Enumerable.Range(0, 81).Select(i => {
                double x = begin + (end - begin) * i / 80;
                return new Point(x, f(x));
            }).ToArray();
        }

        private static void Spring(Input input, Report report)
        {
            var zero = new double?[3];
            var constants = new double?[3];
            for (int i = 0; i < 3; i++)
            {
                var rows = input.Rows(i, Cols(2), new[] { 0 }).ToArray();
                zero[i] = input.Setting(i, v => true, rows.Length > 0);
                if (rows.Length == 0 || !zero[i].HasValue) continue;

                int spring = i + 1;
                double x0 = zero[i].Value;
                var table = report.Table("Tablo 1.4." + spring + " · Yay " + spring, "m (g)|F (dyn)|x₁ (cm)|x₁ − x₀ (cm)");
                foreach (var row in rows) table.Add(row[0], row[0] * Gcgs, row[1], row[1] - x0);
                var points = rows.Select(r => new Point(r[1] - x0, r[0] * Gcgs)).ToArray();
                var fit = Fit.Linear(points);
                var chart = report.Chart("Yay " + spring + " · F = f(x₁ − x₀)", "x₁ − x₀ (cm)", "F (dyn)",
                    fit == null ? "Eğim için iki farklı uzama gerekir." : fit.Equation);
                chart.Add("Ölçüm", TraceStyle.Points, points);
                if (fit == null)
                {
                    report.Warn("Yay " + spring + " için eğim hesaplamak üzere iki farklı uzama gir.");
                    continue;
                }
                chart.Add("Doğrusal uyum", TraceStyle.Line, fit.Domain(points));
                if (fit.Slope > 0) constants[i] = fit.Slope;
                else report.Warn("Yay " + spring + " eğimi pozitif çıkmadı. Cetvel yönünü ve kayıt sırasını kontrol et.");
                string estimate = fit.SlopeError.HasValue ? Format.WithUncertainty(fit.Slope, fit.SlopeError.Value) : N(fit.Slope);
                report.Note("Yay " + spring + " · k",
                    estimate + " dyn/cm (" + N(fit.Slope * .001) + " N/m). " + fit.Equation +
                    ". Buradaki ±, uygun olduğunda eğimin standart hatasıdır. Hooke yasasında uygulanan kuvvetin büyüklüğü uzamayla artar.");
            }
            var energy = input.Rows(3, new[] { 0, 1, 2, 3, 4 }, new[] { 0, 4, 5, 6 }).ToArray();
            if (energy.Length == 0) return;
            var springNumber = input.Setting(3, x => x >= 1 && x <= 3 && x == Math.Truncate(x), true);
            double? k = springNumber.HasValue ? constants[(int)springNumber.Value - 1] : null;
            double? unloaded = springNumber.HasValue ? zero[(int)springNumber.Value - 1] : null;
            if (!k.HasValue) report.Warn("Yay enerjisi ve teorik periyot için seçilen yayın pozitif k eğimi gerekli. Diğer sonuçlar yine hesaplanır.");

            var energyTable = report.Table("Enerji karşılaştırması",
                "Deneme|m (g)|Yön|x ilk (cm)|x son (cm)|ΔE yay (erg)|E yer kaybı (erg)|Kayıp − artış (erg)|Fark (%)");
            var timeTable = report.Table("Salınım sonuçları",
                "Deneme|Ort. 10T (s)|T (s)|ω (rad/s)|A (cm)|f (Hz)|T teorik (s)|T farkı (%)");
            foreach (var row in energy)
            {
                double start = row[1] + row[2], end = row[3];
                double loss = row[0] * Gcgs * (end - start);
                double? gain = k.HasValue ? 0.5 * k * (Fit.Square(end - unloaded.Value) - Fit.Square(start - unloaded.Value)) : null;
                double? difference = loss - gain;
                double? percent = gain.HasValue && loss != 0 ? (double?)Error(gain.Value, loss) : null;
                energyTable.Add(row.Number, row[0], row[2] < 0 ? "Yukarı" : row[2] > 0 ? "Aşağı" : "Dengede",
                    start, end, gain, loss, difference, percent);
                var times = row.Values.Skip(4).Where(x => x.HasValue).Select(x => x.Value).ToArray();
                double time = times.Average(), period = time / 10;
                double? theory = k.HasValue ? (double?) (Tau * Math.Sqrt(row[0] / k.Value)) : null;
                timeTable.Add(row.Number, time, period, Tau / period, Math.Abs(row[2]), 1 / period, theory,
                    theory.HasValue ? (double?)Error(period, theory.Value) : null);
            }
            report.Note("Enerji ve konum işareti",
                "Aşağı yön pozitiftir. x ilk = x denge + Δx ilk. Yay enerjisi yüksüz x₀’a göre ½k(x − x₀)², yerçekimi enerjisi kaybı mg(x son − x ilk). " +
                "Pozitif fark, yaydaki artışın yerçekimi enerjisi kaybından küçük olduğunu gösterir. Sönüm, cetvel okuması ve son konumun seçimi farkı etkileyebilir.");
            report.Note("Periyot ve genlik",
                "10 salınım sürelerinin ortalaması 10’a bölündü. ω = 2π/T, A = |Δx ilk|. Teorik periyotta askı kütlesi kullanılır ve yayın kendi kütlesi ihmal edilir.");
        }

        private static void Forces(Input input, Report report)
        {
            var unit = input.Setting(0, v => v == 1 || v == 2, true);
            var dF = input.Setting(1, v => v >= 0);
            var dPhi = input.Setting(2, v => v >= 0 && v <= 180);
            if (!unit.HasValue) return;
            double scale = unit.Value == 1 ? 1 : G / 1000;
            string[] names = { "120° aralık", "Sabit açılar", "Sabit kuvvetler" };
            for (int group = 0; group < names.Length; group++)
            {
                var rows = input.Rows(group, Cols(6), NoColumns, 0, 2, 4).ToArray();
                if (rows.Length == 0) continue;
                var table = report.Table(names[group] + " · Kuvvet bileşenleri (N)",
                    "Deneme|F₁x|F₁y|F₂x|F₂y|F₃x|F₃y|ΣFₓ|ΣFᵧ|‖R‖|R yönü (°)|Sapma (%)|ΔΣFₓ|ΔΣFᵧ|Denge kontrolü");
                var sine = report.Table(names[group] + " · Sinüs bağıntısı (2.12)",
                    "Deneme|α (°)|β (°)|θ (°)|F₁/sin α (N)|F₂/sin β (N)|F₃/sin θ (N)|F₂ teo. (N)|F₃ teo. (N)|F₂ sapma (%)|F₃ sapma (%)");
                foreach (var row in rows)
                {
                    var f = new[] { row[0] * scale, row[2] * scale, row[4] * scale };
                    var ph = new[] { row[1], row[3], row[5] }.Select(NormalizeAngle).ToArray();
                    var radians = ph.Select(x => x * Math.PI / 180).ToArray();
                    var fx = radians.Select((a, i) => f[i] * Math.Cos(a)).ToArray();
                    var fy = radians.Select((a, i) => f[i] * Math.Sin(a)).ToArray();
                    double sx = fx.Sum(), sy = fy.Sum(), r = Math.Sqrt(sx * sx + sy * sy), total = f.Sum();
                    if (total == 0) report.Warn(names[group] + ", satır " + row.Number + " · Üç kuvvet de sıfır.");
                    if (r <= total * 1e-12) { sx = 0; sy = 0; r = 0; }
                    double? dir = r == 0 ? (double?)null : NormalizeAngle(Math.Atan2(sy, sx) * 180 / Math.PI);
                    double? errX = null, errY = null;
                    if (dF.HasValue || dPhi.HasValue)
                    {
                        double delta = (dPhi ?? 0) * Math.PI / 180;
                        errX = radians.Select((a, i) => Math.Abs(Math.Cos(a)) * (dF ?? 0) + Math.Abs(fy[i]) * delta).Sum();
                        errY = radians.Select((a, i) => Math.Abs(Math.Sin(a)) * (dF ?? 0) + Math.Abs(fx[i]) * delta).Sum();
                    }
                    string balance = r == 0 ? "ΣF ≈ 0" : errX.HasValue
                        ? (Math.Abs(sx) <= errX.Value && Math.Abs(sy) <= errY.Value ? "Belirsizlik içinde" : "Belirsizlik dışında")
                        : "ΣF sıfırdan farklı";
                    table.Add(row.Number, Clean(fx[0], total), Clean(fy[0], total), Clean(fx[1], total), Clean(fy[1], total),
                        Clean(fx[2], total), Clean(fy[2], total), sx, sy, r, dir, total > 0 ? (double?)(100 * r / total) : null, errX, errY, balance);
                    double[] angle = { Separation(ph[1], ph[2]), Separation(ph[0], ph[2]), Separation(ph[0], ph[1]) };
                    var opposite = angle.Select(x => Math.Sin(x * Math.PI / 180)).ToArray();
                    bool closes = Math.Abs(angle.Sum() - 360) < 1e-7 && opposite.All(v => v > 1e-9);
                    if (!closes)
                        report.Warn(names[group] + ", satır " + row.Number + " · Bu yönlerle üç pozitif kuvvet için sinüs bağıntısından denge çözümü çıkmıyor.");
                    double? theory2 = closes ? (double?)(f[0] * opposite[1] / opposite[0]) : null;
                    double? theory3 = closes ? (double?)(f[0] * opposite[2] / opposite[0]) : null;
                    sine.Add(row.Number, angle[0], angle[1], angle[2],
                        SineRatio(f[0], opposite[0]), SineRatio(f[1], opposite[1]), SineRatio(f[2], opposite[2]),
                        theory2, theory3, Relative(f[1], theory2), Relative(f[2], theory3));

                    var plot = report.Chart(names[group] + " · Deneme " + row.Number, "Fₓ (N)", "Fᵧ (N)",
                        "‖R‖ = " + N(r) + " N. Vektör başlangıçları (0, 0).");
                    plot.EqualAxes = true;
                    var origin = new Point(0, 0);
                    for (int i = 0; i < 3; i++)
                        plot.Add("F" + (i + 1) + " · " + N(ph[i]) + "°", TraceStyle.Vector, new[] { origin, new Point(fx[i], fy[i]) });
                    if (r > 0) plot.Add("Bileşke R", TraceStyle.Vector, new[] { origin, new Point(sx, sy) });
                }
            }
            report.Note("Denge kontrolü",
                "Fₓ = F cos φ, Fᵧ = F sin φ. İdeal dengede ΣFₓ ve ΣFᵧ sıfırdır. Sapma yüzdesi = 100 ‖R‖/Σ|Fᵢ|. " +
                "Gram girdisi kg’ye çevrilerek g = 9,8 m/s² ile çarpılır. Girilen belirsizlikler, bileşenlere birinci dereceden mutlak türev toplamıyla yayılır. " +
                "Denge uyumu, iki bileşenin de kendi belirsizlik aralığında sıfırı içerip içermediğine göre değerlendirilir.");
            report.Note("Karşı açılar ve sinüs bağıntısı",
                "α, F₂ ile F₃ arasındaki açı. β, F₁ ile F₃ arasındaki açı. θ, F₁ ile F₂ arasındaki açı. " +
                "Dengede F₁/sin α = F₂/sin β = F₃/sin θ. Teorik F₂ ve F₃ için F₁ referans alınır. Makara sürtünmesi ve açı okumaları dengeyi etkileyebilir.");
        }

        private static double Clean(double value, double scale) { return Math.Abs(value) <= scale * 1e-12 ? 0 : value; }
        private static double NormalizeAngle(double degrees) { return (degrees % 360 + 360) % 360; }
        private static double Separation(double a, double b) { double d = Math.Abs(a - b); return Math.Min(d, 360 - d); }
        private static double? SineRatio(double f, double sine) { return sine > 1e-9 ? (double?) (f / sine) : null; }
        private static double? Relative(double measured, double? reference)
        { return reference.HasValue && reference.Value > 0 ? (double?)Error(measured, reference.Value) : null; }

        private static void FreeFall(Input input, Report report)
        {
            var rows = input.Rows(0, Cols(4), Cols(4)).ToArray();
            if (rows.Length == 0) return;
            double? reference = input.Setting(0, x => x > 0, true);
            var table = report.Table("Tablo 3.1 · Süre ortalamaları", "h (cm)|t₁ (s)|t₂ (s)|t₃ (s)|Ort. t (s)|t² (s²)|Δt (s)|Noktasal g (m/s²)");
            var points = new List<Point>();
            foreach (var row in rows)
            {
                var times = new[] { row[1], row[2], row[3] };
                var mean = times.Average();
                table.Add(row[0], row[1], row[2], row[3], mean, mean * mean, Fit.StandardError(times), 2 * row[0] / 100 / (mean * mean));
                points.Add(new Point(mean, row[0]));
            }
            var squared = points.Select(p => new Point(p.X * p.X, p.Y)).ToArray();
            var fit = Fit.Linear(squared);
            var time = report.Chart("Serbest düşme · h = f(t)", "t (s)", "h (cm)", "Eğri, h = at² + b modelidir.");
            time.Add("Ölçüm", TraceStyle.Points, points);
            var linear = report.Chart("Serbest düşme · h = f(t²)", "t² (s²)", "h (cm)", fit == null ? "Eğim için farklı ortalama süreler gerekir." : fit.Equation);
            linear.Add("Ölçüm", TraceStyle.Points, squared);
            if (fit != null)
            {
                time.Add("İkinci dereceden model", TraceStyle.Line, Curve(points.Min(p => p.X), points.Max(p => p.X),
                    x => fit.Slope * x * x + fit.Intercept));
                linear.Add("Doğrusal uyum", TraceStyle.Line, fit.Domain(squared));
                double g = 2 * fit.Slope / 100;
                report.Note("Eğimden yerçekimi ivmesi", "g = 2 × eğim (birim dönüşümüyle) = " +
                    (fit.SlopeError.HasValue ? Format.WithUncertainty(g, 2 * fit.SlopeError.Value / 100) : N(g)) +
                    " m/s². Eğim " + N(fit.Slope) + " cm/s². " + fit.Equation + "." +
                    (reference.HasValue ? " " + N(reference.Value) + " m/s² referansına göre sapma %" + N(Error(g, reference.Value)) + "." : ""));
                if (g <= 0) report.Warn("g pozitif çıkmadı. Yükseklik, süre ve saniye birimini yeniden kontrol et.");
            }
            else report.Warn("g eğimi için farklı ortalama süreye sahip en az iki satır gerekli.");
            report.Note("Grafikler ve föy soruları",
                "İdeal serbest düşmede h, t² ile orantılıdır. Bu yüzden h–t grafiği parabol, h–t² grafiği doğrudur. " +
                "Hava direnci yoksa topun boyutu, kütlesi ve malzemesi süreyi değiştirmez. Havada küçük ya da düşük yoğunluklu bir topun süre farkı, sürükleme kuvvetinin ağırlığına oranına bağlıdır.");
            report.Note("Olası hata kaynakları",
                "Elektromıknatısın bırakma gecikmesi, fotosensörün tetikleme noktası, cetvelin sıfırı ve hava direnci etkili olabilir. " +
                "Kesim b, ölçüm ofseti için bir göstergedir. Δt, üç tekrarın ortalama standart hatasıdır.");
        }
    }
}
