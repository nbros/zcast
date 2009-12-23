using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

namespace GDIScreenshot
{
    public class FrameInfo
    {
        public int FrameNumber { get; set; }
        public Point Offset { get; set; }
        public string FileName { get; set; }
    }

    public class ScreencastData
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int DelayBetweenFrames { get; set; }

        public List<FrameInfo> FrameInfos = new List<FrameInfo>();

        public void SerializeTo(Stream stream)
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(GetType());
            serializer.Serialize(stream, this);
        }

        public static ScreencastData DeSerializeFrom(Stream stream)
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(ScreencastData));
            return (ScreencastData)serializer.Deserialize(stream);
        }
    }
}
