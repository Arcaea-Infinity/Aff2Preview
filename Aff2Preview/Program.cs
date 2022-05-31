using AffTools.Aff2Preview;

AffRenderer affRenderer = new("assets/2.aff")
{
    Title = "Gift",
    Artist = "Notorious(Nota & TRIAL)",
    Charter = "Misaka12456",
    Side = 0,
    Difficulty = 2,
    Rating = 10.3f,
    Notes = 0,
    ChartBpm = 200,
};

affRenderer.LoadResource(
    "assets/note.png",
    "assets/note_hold.png",
    "assets/arc_body.png",
    "assets/base.jpg",
    "assets/base.jpg");

var image = affRenderer.Draw();

image?.SaveToPng("output.png");
