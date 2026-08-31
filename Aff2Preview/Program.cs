using AffTools.Aff2Preview;
using AffTools.AffAnalyzer;

//Analyzer.OutputAllChartDoubleTapAnalyze(@"E:\gitlab\aimubot\bot_shared_data\Arcaea\assets\songs\");
//return;

//AffRenderer affRenderer = new(@"E:\gitlab\aimubot\bot_shared_data\Arcaea\assets\songs\dl_testify\3.aff")
AffRenderer affRenderer = new(@"E:\github\Aff2Preview\1.aff")
{
    Title = "dl_testify",
    Artist = "",
    Charter = "",
    Side = 2,
    Difficulty = 3,
    Rating = 11f,
    Notes = 0,
    ChartBpm = 222,
    IsMirror = false
};

affRenderer.LoadResource(
    "assets/note.png",
    "assets/note_hold.png",
    "assets/arc_body.png",
    @"E:\gitlab\aimubot\bot_shared_data\Arcaea\assets\img\bg\testify.jpg",
    @"E:\gitlab\aimubot\bot_shared_data\Arcaea\assets\songs\dl_testify\base.jpg");

var image = affRenderer.Draw();

image?.SaveToPng("output.png");
