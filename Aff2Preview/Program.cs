using AimuBotCS.Modules.Arcaea.Aff2Preview;

AffRenderer affRenderer = new();
affRenderer.LoadResource("assets/note.png", "assets/note_hold.png", "assets/arc_body.png", "assets/base.jpg");

var image = await affRenderer.Render(
    "assets/2.aff", 0, 2, "assets/base.jpg", "1/16 Toriru Practice", "9+", "InariAimu", "InariAimu");

image?.Save("output.png");
