using System.Collections.Generic;

namespace HitboxAssigner
{
    public class HitboxInformation
    {
        public string Spritename { get; set; }
        public List<Box> HitBoxes { get; set; }
        public List<Box> HurtBoxes { get; set; }
    }

    public struct Box
    {
        public Box(int x, int y, int width, int height, int frameNo, int zoomLevel, bool nextMove = false)
        {
            NextMove = nextMove;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            FrameNo = frameNo;
            ZoomLevel = zoomLevel;
        }

        public bool NextMove { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int FrameNo { get; set; }
        public int ZoomLevel { get; set; }
    }
}
