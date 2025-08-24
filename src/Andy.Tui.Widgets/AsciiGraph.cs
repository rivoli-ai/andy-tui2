using System;
using System.Collections.Generic;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    // Simple network/graph: draws nodes and lines using ASCII approximations
    public sealed class AsciiGraph
    {
        public readonly struct Node { public readonly int X,Y; public readonly string Label; public Node(int x,int y,string label){X=x;Y=y;Label=label;} }
        private readonly List<Node> _nodes = new();
        private readonly List<(int A,int B)> _edges = new();
        private DL.Rgb24 _fg = new DL.Rgb24(220,220,220);
        private DL.Rgb24 _bg = new DL.Rgb24(0,0,0);
        public void SetNodes(IEnumerable<Node> nodes){ _nodes.Clear(); if (nodes!=null) _nodes.AddRange(nodes); }
        public void SetEdges(IEnumerable<(int A,int B)> edges){ _edges.Clear(); if (edges!=null) _edges.AddRange(edges); }
        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X,y=(int)rect.Y,w=(int)rect.Width,h=(int)rect.Height; if (w<=0||h<=0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h)); b.DrawRect(new DL.Rect(x,y,w,h,_bg));
            foreach (var (a,bn) in _edges)
            {
                if (a<0||a>=_nodes.Count||bn<0||bn>=_nodes.Count) continue;
                var n1=_nodes[a]; var n2=_nodes[bn];
                int x1=x+n1.X, y1=y+n1.Y, x2=x+n2.X, y2=y+n2.Y;
                int dx = System.Math.Sign(x2-x1), dy = System.Math.Sign(y2-y1);
                int cx = x1, cy = y1;
                while (cx!=x2 || cy!=y2)
                {
                    b.DrawText(new DL.TextRun(cx, cy, ".", _fg, _bg, DL.CellAttrFlags.None));
                    if (cx!=x2) cx += dx; if (cy!=y2) cy += dy;
                }
            }
            for (int i=0;i<_nodes.Count;i++)
            {
                var n=_nodes[i]; int nx=x+n.X, ny=y+n.Y; string cap = $"({n.Label})";
                b.DrawText(new DL.TextRun(nx, ny, cap, _fg, _bg, DL.CellAttrFlags.Bold));
            }
            b.Pop();
        }
    }
}
