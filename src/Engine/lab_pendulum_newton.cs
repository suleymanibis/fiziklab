using System;
using System.Collections.Generic;
using System.Linq;

namespace PhysicsLab
{
    internal static partial class Mechanics
    {
        private static void Pendulum(Input input, Report report)
        {
            var simple = input.Rows(0, Cols(9), Cols(13)).ToArray();
            var reverse = input.Rows(1, new[] { 0 }, Cols(3)).ToArray();
            if (simple.Length == 0 && reverse.Length == 0) return;
            var reference = input.Setting(0, x => x > 0, true);
            if (simple.Length != 0) SimplePendulum(input, report, simple, reference);
            if (reverse.Length != 0) ReversiblePendulum(input, report, reverse, reference);
        }

        private static void SimplePendulum(Input input, Report report, Datum[] rows, double? reference)
        {
            double dr = input.Setting(1, x => x >= 0) ?? 0;
            var lengths = report.Table("Tablo 4.1 · Basit sarkaç", "Düzenek|Ort. ℓ (cm)|r (cm)|L (cm)|Ort. 5τ (s)|τ (s)|τ² (s²)|g (m/s²)|Sapma (%)");
            var errors = report.Table("Basit sarkaç · Belirsizlik", "Düzenek|Süre sayısı|Δℓ (cm)|ΔL (cm)|Δτ (s)|Δg/g (%)|g ± Δg (m/s²)");
            var points = new List<Point>();
            foreach (var row in rows)
            {
                var repetitions = row.Values.Take(4).Select(v => v.Value).ToArray();
                var laps = row.Values.Skip(5).Where(v => v.HasValue).Select(v => v.Value).ToArray();
                if (laps.Length > 4 && laps.Length < 8)
                    report.Warn("Tablo 4.1, satır " + row.Number + " · İkinci yön için dört süreden yalnızca " + (laps.Length - 4) + " tanesi girildi. Girilen süreler ortalamaya katıldı.");
                double ell = repetitions.Average(), length = ell + row[4];
                double period = laps.Average() / 5, t2 = Fit.Square(period);
                double dEll = Fit.StandardError(repetitions), dLength = dEll + dr, dPeriod = Fit.StandardError(laps) / 5;
                double g = Fit.Square(Tau) * length / 100 / t2;
                double relative = dLength / length + 2 * dPeriod / period;
                lengths.Add(row.Number, ell, row[4], length, laps.Average(), period, t2, g, Relative(g, reference));
                errors.Add(row.Number, laps.Length, dEll, dLength, dPeriod, relative * 100, Format.WithUncertainty(g, g * relative));
                points.Add(new Point(t2, length));
            }
            var measured = points.ToArray();
            var fit = Fit.Linear(measured);
            var plot = report.Chart("Basit sarkaç · L = f(τ²)", "τ² (s²)", "L (cm)", fit == null ? "Eğim için iki farklı τ² gerekiyor." : fit.Equation);
            plot.Add("Ölçüm", TraceStyle.Points, measured);
            if (fit != null)
            {
                plot.Add("Doğrusal uyum", TraceStyle.Line, fit.Domain(measured));
                double factor = Fit.Square(Tau) / 100, g = factor * fit.Slope;
                report.Note("Basit sarkaç · Grafikten g",
                    "Eğim = " + N(fit.Slope) + " cm/s². g = 4π² × eğim (birim dönüşümüyle) = " +
                    (fit.SlopeError.HasValue ? Format.WithUncertainty(g, factor * fit.SlopeError.Value) : N(g)) + " m/s²." +
                    (reference.HasValue ? " " + N(reference.Value) + " m/s² referansına göre sapma %" + N(Error(g, reference.Value)) + "." : ""));
                if (g <= 0) report.Warn("Basit sarkacın grafik eğimi pozitif çıkmadı. Uzunluk ve süre satırlarını kontrol et.");
            }
            else report.Warn("Basit sarkacın grafik eğimi için iki farklı τ² değeri gerekli.");
            if (rows.Length < 5) report.Warn("Tablo 4.1 · Föy beş uzunluk istiyor. Hesap girilen satırlara dayanıyor.");
            report.Note("Basit sarkaç · Ölçüm yöntemi",
                "L = ortalama ℓ + r. τ, girilen beş salınım sürelerinin ortalamasının beşte biridir. " +
                "Δℓ ve Δτ, ortalamanın standart hatasıdır. ΔL = Δℓ + Δr. Δg/g = ΔL/L + 2Δτ/τ. " +
                "Δr boşsa yalnızca uzunluk tekrarlarının saçılımı hesaba katılır. Grafikten bulunan ± ise eğimin standart hatasından gelir.");
        }

        private sealed class Branches
        {
            public readonly Point[] Points;
            private readonly int bottom;
            public Branches(Point[] points)
            {
                Points = points.GroupBy(p => p.X).OrderBy(group => group.Key)
                    .Select(group => new Point(group.Key, group.Average(p => p.Y))).ToArray();
                if (Points.Length < 3) { bottom = -1; return; }
                double minimum = Points.Min(p => p.Y);
                var low = Enumerable.Range(0, Points.Length).Where(i => Points[i].Y == minimum).ToArray();
                bottom = low.Any(i => i == 0 || i == Points.Length - 1) ? -1 : low[low.Length / 2];
            }
            public bool HasMinimum { get { return bottom > 0; } }
            public double Low { get { return Points[bottom].Y; } }
            public double High
            {
                get { return Math.Min(Points.Take(bottom).Max(p => p.Y), Points.Skip(bottom + 1).Max(p => p.Y)); }
            }

            public double[] Crossings(double y)
            {
                return new[] { Cross(y, -1), Cross(y, 1) };
            }

            private double Cross(double y, int direction)
            {
                for (int i = bottom + direction; i >= 0 && i < Points.Length; i += direction)
                {
                    Point a = Points[i - direction], b = Points[i];
                    if (b.Y < y || b.Y == a.Y) continue;
                    return a.X + (b.X - a.X) * (y - a.Y) / (b.Y - a.Y);
                }
                return double.NaN;
            }
        }

        private static void ReversiblePendulum(Input input, Report report, Datum[] rows, double? reference)
        {
            var table = report.Table("Tablo 4.2 · Tersinir sarkaç", "Delik / satır|h (cm)|Sol 5τ (s)|Sol τ (s)|Sağ 5τ (s)|Sağ τ (s)");
            var leftPoints = new List<Point>();
            var rightPoints = new List<Point>();
            foreach (var row in rows)
            {
                if (!row.Values[1].HasValue && !row.Values[2].HasValue)
                {
                    report.Warn("Tablo 4.2, satır " + row.Number + " · Sol veya sağ süreyi gir.");
                    continue;
                }
                var l = row.Values[1] / 5; var r = row.Values[2] / 5;
                table.Add(row.Number, row[0], row.Values[1], l, row.Values[2], r);
                if (l.HasValue) leftPoints.Add(new Point(row[0], l.Value));
                if (r.HasValue) rightPoints.Add(new Point(row[0], r.Value));
            }
            if (table.Rows.Count == 0) return;
            var left = new Branches(leftPoints.ToArray());
            var right = new Branches(rightPoints.ToArray());
            var plot = report.Chart("Tersinir sarkaç · τ = f(h)", "Merkeze göre konum h (cm)", "τ (s)",
                "Sol delikler negatif, sağ delikler pozitif eksende. Birleşik noktalar arasında doğrusal ara değer kullanılır.");
            plot.Add("Sol ölçüm", TraceStyle.Points, leftPoints.Select(p => new Point(-p.X, p.Y)));
            plot.Add("Sağ ölçüm", TraceStyle.Points, rightPoints);
            plot.Add("Sol ara değer", TraceStyle.Line, left.Points.Reverse().Select(p => new Point(-p.X, p.Y)));
            plot.Add("Sağ ara değer", TraceStyle.Line, right.Points);
            if (left.Points.Length != leftPoints.Count || right.Points.Length != rightPoints.Count)
                report.Note("Aynı uzaklıktaki tekrarlar", "Kesişim hesabı ve ara değer çizgisi için aynı tarafta ve aynı h’deki sürelerin ortalaması alındı.");

            if (!left.HasMinimum || !right.HasMinimum)
            {
                report.Warn("Tersinir sarkaç · Her tarafta bir iç minimum ve minimumun iki yanında ölçüm gerekli. Grafik mevcut verilerle çizildi.");
                return;
            }
            var low = Math.Max(left.Low, right.Low);
            var high = Math.Min(left.High, right.High);
            if (low >= high)
            {
                report.Warn("Tersinir sarkaç · İki tarafın dört farklı noktada kesiştiği ortak bir periyot aralığı bulunamadı.");
                return;
            }
            var requested = input.Setting(2, value => value > 0);
            double period = requested ?? (low + high) / 2;
            if (period <= low || period > high)
            {
                report.Warn("Ortak periyot " + N(low) + " s’den büyük, " + N(high) + " s’den küçük veya eşit olmalı.");
                return;
            }
            double[] lCross = left.Crossings(period), rCross = right.Crossings(period);
            plot.Add("Ortak periyot", TraceStyle.Guide,
                new[] { new Point(-left.Points.Last().X, period), new Point(right.Points.Last().X, period) });
            plot.Add("Kesişimler", TraceStyle.Points, lCross.Select(h => new Point(-h, period)).Concat(rCross.Select(h => new Point(h, period))));
            double length = lCross[0] + rCross[1];
            double other = lCross[1] + rCross[0];
            double g = Fit.Square(Tau / period) * length / 100;
            double otherG = Fit.Square(Tau / period) * other / 100;
            var equivalent = report.Table("Tersinir sarkaç · Eşdeğer uzunluk", "Kesişim çifti|h₁ (cm)|h₂ (cm)|L (cm)|τ (s)|g (m/s²)|Sapma (%)");
            equivalent.Add("Sol iç + sağ dış", lCross[0], rCross[1], length, period, g, Relative(g, reference));
            equivalent.Add("Sol dış + sağ iç", lCross[1], rCross[0], other, period, otherG, Relative(otherG, reference));
            report.Note("Tersinir sarkaç · Dört kesişim",
                "Şekil 4.3’teki sol iç ve sağ dış kesişimleri ana sonuç olarak kullanılır. " +
                "Diğer çapraz çift, karşılaştırma için verilir. İki g sonucu arasındaki fark " + N(Math.Abs(g - otherG)) + " m/s². " +
                "Ortak periyot " + (requested.HasValue ? "girilen değerdir. " : "geçerli aralığın orta noktasıdır. ") +
                "Hesap g = 4π²(h₁ + h₂)/τ² bağıntısını kullanır.");
        }

        private static void Newton(Input input, Report report)
        {
            var sets = Enumerable.Range(0, 2).Select(group => input.Rows(group, Cols(5), new[] { 0, 2, 3, 4 }, 1).ToArray()).ToArray();
            if (sets.All(set => set.Length == 0)) return;
            double? lengthCm = input.Setting(0, x => x > 0, true);
            if (!lengthCm.HasValue) return;
            double length = lengthCm.Value / 100;
            for (int group = 0; group < 2; group++)
            {
                var rows = sets[group];
                if (rows.Length == 0) continue;
                bool forceVaries = group == 0;
                string title = "Tablo 5." + (group + 1);
                var table = report.Table(title + " · Işık kapısı hesapları",
                    "Deneme|m (g)|mₐ (g)|t₁ (s)|t₂ (s)|t₃ (s)|v₁ (m/s)|v₂ (m/s)|a (m/s²)|Fₐ (N)|a teorik (m/s²)|Sapma (%)");
                double[] constant = rows.Select(r => forceVaries ? r[0] + r[1] : r[1]).ToArray();
                bool follows = constant.Max() - constant.Min() <= Math.Max(1e-9, constant.Max() * 1e-9);
                if (!follows) report.Warn(title + " · " + (forceVaries ? "m + mₐ" : "mₐ") + " girilen satırlarda sabit değil. Teorik karşılaştırma her satırın kendi kütlesini kullanır.");
                var points = new List<Point>();
                var models = new List<Point>();
                foreach (var row in rows)
                {
                    double transit = row[4], v1 = length / row[2], v2 = length / row[3];
                    double average = (v2 - v1) / transit, force = row[1] * G / 1000;
                    double model = force / ((row[0] + row[1]) / 1000);
                    table.Add(row.Number, row[0], row[1], row[2], row[3], transit,
                        v1, v2, average, force, model, model > 0 ? (double?)Error(average, model) : null);
                    double x = forceVaries ? force : row[0] / 1000;
                    points.Add(new Point(x, average));
                    models.Add(new Point(x, model));
                }
                if (points.Select(p => p.X).Distinct().Count() < 5)
                    report.Warn(title + " · Föy, yatay eksen için en az beş farklı ölçüm değeri istiyor.");
                var measured = points.ToArray();
                var fit = forceVaries ? Fit.Linear(measured) : null;
                var plot = report.Chart(forceVaries ? "İvme · a = f(Fₐ)" : "İvme · a = f(m)",
                    forceVaries ? "Fₐ (N)" : "Kızak kütlesi m (kg)", "Ortalama a (m/s²)",
                    forceVaries ? (fit == null ? "Beklenen ilişki a = Fₐ/(m + mₐ)." : fit.Equation) :
                    "Sabit mₐ için beklenen ilişki a = mₐg/(m + mₐ).");
                plot.Add("Ölçüm", TraceStyle.Points, points);
                if (fit != null) plot.Add("Doğrusal uyum", TraceStyle.Line, fit.Domain(measured));
                if (follows)
                {
                    double mass = constant.Average() / 1000;
                    plot.Add("İdeal model", TraceStyle.Guide, Curve(measured.Min(p => p.X), measured.Max(p => p.X),
                        x => forceVaries ? x / mass : mass * G / (x + mass)));
                }
                else plot.Add("Satırın ideal değeri", TraceStyle.Points, models);
                if (forceVaries && fit != null)
                {
                    string text = fit.Equation + ". Eğim " + N(fit.Slope) + " kg⁻¹.";
                    if (follows) text += " Sabit toplam kütleye göre ideal eğim " + N(1000 / constant[0]) + " kg⁻¹.";
                    if (fit.Slope > 0) text += " Eğimden bulunan etkin kütle " + N(1 / fit.Slope) + " kg.";
                    report.Note("Kuvvet arttığında", text);
                }
                if (!forceVaries)
                {
                    var min = measured.OrderBy(p => p.X).First();
                    var max = measured.OrderByDescending(p => p.X).First();
                    if (max.X > min.X)
                        report.Note("Kızak kütlesi arttığında", "En düşük ve en yüksek m için ölçülen ivmeler sırasıyla " +
                            N(min.Y) + " ve " + N(max.Y) + " m/s². Sabit mₐ altında ideal model ivmenin kütle arttıkça azalmasını öngörür.");
                }
            }
            report.Note("Hızlar ve ortalama ivme",
                "v₁ = L/t₁, v₂ = L/t₂, a = (v₂ − v₁)/t₃, Fₐ = mₐg. İdeal ivme Fₐ/(m + mₐ). " +
                "Ölçülen ivme iki kapı arasındaki ortalamadır. Anlık ivmeyi temsil etmesi, aralıkta net kuvvet ve ivmenin yaklaşık sabit olmasına bağlıdır.");
            report.Note("Olası hata kaynakları",
                "Ray eğimi, hava akışı, makarada sürtünme veya dönme eylemsizliği ve etkin uzunluk ölçümü ideal modelden sapma yaratabilir.");
        }
    }
}
