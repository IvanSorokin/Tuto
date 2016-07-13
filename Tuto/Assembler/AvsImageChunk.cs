using System;
using System.Collections.Generic;
using System.IO;
using System.Web.UI.WebControls;

namespace Tuto.TutoServices.Assembler
{
    class AvsImageChunk : AvsNode
    {
        public FileInfo ChunkFile { get; set; }
        public int FPS { get; set; }
        public int Length { get; set; }

        public override void SerializeToContext(AvsContext context)
        {
            id = context.Id;
            context.AddData(String.Format(Format, Id, ChunkFile, FPS, Time2Frame(Length)));
        }

        public override IList<AvsNode> ChildNodes
        {
            get { return new AvsNode[] {}; }
        }

        protected override string Format 
        {
            get { return "{0} = ImageSource(\"{1}\", fps={2}, start=1, end={3})"; }
        }
    }
}