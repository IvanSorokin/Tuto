﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Tuto.Model
{
    public enum PatchType
    {
        Empty,
        Subtitles,
        Video
    }

    [DataContract]
    public class Patch
    {
        [DataMember]
        public int Begin { get; set; }
        [DataMember]
        public int End { get; set; }
        [DataMember]
        public SubtitlePatch Subtitles { get; set; }
        [DataMember]
        public VideoPatch Video { get; set; }

        public PatchType Type
        {
            get
            {
                if (Subtitles != null) return PatchType.Subtitles;
                if (Video != null) return PatchType.Video;
                return PatchType.Empty;
            }
        }
    }
}
