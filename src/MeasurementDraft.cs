using System.Text.Json.Serialization;

namespace PhysicsLab.Safari;

public sealed class MeasurementDraft
{
    public string[] Options { get; set; }
    public List<string[][]> Measurements { get; set; }

    internal static MeasurementDraft From(Experiment experiment) => new()
    {
        Options = experiment.Options.Select(option => option.Initial).ToArray(),
        Measurements = experiment.Groups.Select(group => group.Initial.Select(row => row.ToArray()).ToArray()).ToList()
    };

    internal bool Matches(Experiment experiment) =>
        Options?.Length == experiment.Options.Length && Measurements?.Count == experiment.Groups.Length &&
        Measurements.Select((rows, i) => rows != null && rows.Length == Math.Clamp(rows.Length, 1, 150) &&
            rows.All(row => row != null && row.Length == experiment.Groups[i].Columns.Length)).All(valid => valid);
}

[JsonSerializable(typeof(MeasurementDraft))]
internal sealed partial class DraftJson : JsonSerializerContext;
