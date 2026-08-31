using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using AffTools.AffAnalyzer;

using AffTools.AffReader;

namespace AffTools.AffTools.AffAnalyzer;

public enum NoteType
{
    Tap,
    ArcTap,
    Hold,
    Arc,
    Flick,
}

public enum NoteSide
{
    None,
    Left,
    Right,
    Middle,
}

public class NoteObject
{
    public NoteType Type { get; set; }
    public Vector3 Location { get; init; }
    public Vector3 EndLocation { get; init; }
    public string? Property { get; init; }
    public int BaseTrack { get; init; } = 4;
    public NoteSide Side { get; init; } = NoteSide.None;
    public TimingNote? TimingNote { get; set; }
}

public class NoteAnalyzer
{
    public ArcaeaAffReader AffReader { get; set; } = new();

    public List<NoteObject> NoteObjects { get; set; } = new();

    public void Aff2TappableObjects()
    {
        foreach (var ev in AffReader.Events)
        {
            //if (IsGroupNoInput(ev.TimingGroup))
            //    continue;

            switch (ev)
            {
                case ArcaeaAffTap tap:
                    {
                        var no = new NoteObject()
                        {
                            Location = new Vector3(0, 0, 0),
                        };
                        NoteObjects.Add(no);
                    }
                    break;
            }
        }
    }
}
