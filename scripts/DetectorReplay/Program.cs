using System.Diagnostics;
using System.Text.Json;
using ChestTracker.Core;

if (args.Length < 2) { Console.Error.WriteLine("Usage: DetectorReplay video output.jsonl [crop=width:height:x:y] [fps=6]"); return 2; }
if (Path.GetExtension(args[0]).Equals(".jsonl", StringComparison.OrdinalIgnoreCase))
{
    var replayAnalysis = new GameplayAnalyzer(); var replayDetector = new ChestDetector("visual-hud-reference-v2", TimeSpan.FromSeconds(2));
    using var replayOutput = File.CreateText(args[1]); GameState? lastState = null;
    foreach (var line in File.ReadLines(args[0]))
    {
        var input = JsonSerializer.Deserialize<CapturedFrame>(line, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        var result = replayAnalysis.Analyze(DateTimeOffset.UnixEpoch.AddSeconds(input.Second), input.Evidence, input.Text ?? new());
        var update = replayDetector.Process(result.Observation);
        replayOutput.WriteLine(JsonSerializer.Serialize(new { input.Second, state = update.State.ToString(), update.Count, detection = update.Event is not null, result.ChestReason }));
        if (lastState != update.State || update.Event is not null) Console.WriteLine($"{input.Second:F3}s: {update.State}, chests={update.Count}, {result.ChestReason}");
        lastState = update.State;
    }
    return 0;
}
var start = new ProcessStartInfo("ffmpeg") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
var offset = double.Parse((args.FirstOrDefault(a => a.StartsWith("start=")) ?? "start=0")[6..], System.Globalization.CultureInfo.InvariantCulture);
if (offset > 0) { start.ArgumentList.Add("-ss"); start.ArgumentList.Add(offset.ToString(System.Globalization.CultureInfo.InvariantCulture)); }
var duration = args.FirstOrDefault(a => a.StartsWith("duration="));
if (duration is not null) { start.ArgumentList.Add("-t"); start.ArgumentList.Add(duration[9..]); }
var fps = args.FirstOrDefault(a => a.StartsWith("fps=")) ?? "fps=6";
var cropArg = args.FirstOrDefault(a => a.StartsWith("crop="));
var filters = cropArg is null ? "" : cropArg + ",";
foreach (var arg in new[] { "-hide_banner", "-loglevel", "error", "-i", args[0], "-vf", filters + fps + ",scale=960:540", "-f", "rawvideo", "-pix_fmt", "bgra", "pipe:1" }) start.ArgumentList.Add(arg);
using var process = Process.Start(start)!;
var errorTask = process.StandardError.ReadToEndAsync();
using var output = File.CreateText(args[1]);
var pixels = new PixelHudDetector();
var ocrFolder = args.FirstOrDefault(a => a.StartsWith("ocr-models="))?[11..];
var ocrCache = args[1] + ".ocr"; Directory.CreateDirectory(ocrCache);
var analysis = new GameplayAnalyzer(); var detector = new ChestDetector("visual-hud-reference-v2", TimeSpan.FromSeconds(2));
var buffer = new byte[960 * 540 * 4];
HudText text = new(); DateTimeOffset textAt = default; var pendingText = new Queue<(DateTimeOffset ApplyAt, DateTimeOffset At, HudText Text)>();
GameState? priorState = null;
int index = 0; double rate = double.Parse(fps[4..], System.Globalization.CultureInfo.InvariantCulture);
while (true)
{
    int read = 0, count;
    while (read < buffer.Length && (count = await process.StandardOutput.BaseStream.ReadAsync(buffer.AsMemory(read))) > 0) read += count;
    if (read == 0) break;
    if (read != buffer.Length) throw new IOException("Truncated video frame");
    double second = offset + index++ / rate; var at = DateTimeOffset.UnixEpoch.AddSeconds(second);
    var evidence = pixels.Analyze(at, buffer);
    if (ocrFolder is not null && (index - 1) % (int)rate == 0)
    {
        var cache = Path.Combine(ocrCache, $"{second:F3}.json");
        HudText recognized;
        if (File.Exists(cache)) recognized = JsonSerializer.Deserialize<HudText>(File.ReadAllText(cache))!;
        else
        {
            var values = new List<(string Name, string Text)>(); int region = 0;
            foreach (var input in HudOcrInputs.Create(buffer))
            {
                var pgm = Path.Combine(ocrCache, $"input-{region++}.pgm"); await File.WriteAllBytesAsync(pgm, input.ToPgm());
                var info = new ProcessStartInfo("tesseract") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
                info.Environment["OMP_THREAD_LIMIT"] = "1";
                foreach (var value in new[] { pgm, "stdout", "--tessdata-dir", ocrFolder, "-l", "deu+eng", "--psm", input.SingleBlock ? "6" : "11" }) info.ArgumentList.Add(value);
                using var ocr = Process.Start(info)!; var stdout = ocr.StandardOutput.ReadToEndAsync(); var stderr = ocr.StandardError.ReadToEndAsync(); await ocr.WaitForExitAsync();
                if (ocr.ExitCode != 0) throw new IOException(await stderr);
                values.Add((input.Name, await stdout));
            }
            recognized = HudOcrInputs.Combine(values); await File.WriteAllTextAsync(cache, JsonSerializer.Serialize(recognized));
        }
        // Model asynchronous recognition completing half a second after capture. Never apply future text early.
        pendingText.Enqueue((at.AddMilliseconds(500), at, recognized));
    }
    while (pendingText.TryPeek(out var pending) && pending.ApplyAt <= at) { pendingText.Dequeue(); text = pending.Text; textAt = pending.At; }
    var currentText = at - textAt <= TimeSpan.FromSeconds(2) ? text : new();
    var result = analysis.Analyze(at, evidence, currentText);
    var update = detector.Process(result.Observation);
    output.WriteLine(JsonSerializer.Serialize(new { second, evidence, result.Detail, result.ChestReason, text = currentText, state = update.State.ToString(), update.Count, detection = update.Event is not null }));
    if (update.State != priorState) { Console.WriteLine($"{second:F3}s: {update.State}"); priorState = update.State; }
    if (update.Event is not null) Console.WriteLine($"{second:F3}s: chest {update.Count} ({result.ChestReason})");
}
await process.WaitForExitAsync();
if (process.ExitCode != 0) { Console.Error.WriteLine(await errorTask); return 1; }
Console.WriteLine($"Read {index} frames. Private observations saved locally to {args[1]}");
return 0;

record CapturedFrame(double Second, PixelEvidence Evidence, HudText? Text);
