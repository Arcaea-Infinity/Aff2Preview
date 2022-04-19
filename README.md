# Aff2Preview

Generate 2D chart preview image by aff file.

Usage:

```csharp
AffRenderer affRenderer = new AffRenderer();
affRenderer.LoadResource("assets/note.png", "assets/note_hold.png", "assets/arc_body.png", "assets/base.jpg");

var image = await affRenderer.Render(
    "assets/2.aff", 0, 2, "assets/base.jpg", "1/16 Toriru Practice", "9+", "InariAimu", "InariAimu");

image?.Save("output.png");
```

Example output: `assets/2.aff`

![example](output.jpg)

## LICENSE

Assets: `CC-BY-NC` [Arcaea-Infinity/OpenArcaeaArts](https://github.com/Arcaea-Infinity/OpenArcaeaArts)

Aff & cover `616 SB License` InariAimu from [Arcaea-Infinity/FanmadeCharts](https://github.com/Arcaea-Infinity/FanmadeCharts)

Licensed under `616 SB License`.
