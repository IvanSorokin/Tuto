
using System;
using System.Collections.Generic;
using System.IO;
using System.Web.UI.WebControls;
using Tuto.Model;

namespace Tuto.TutoServices.Assembler
{
    public class AvsPatchChunk : AvsNode
    {
        public string Path { get; set; }
        public int ConvertToFps { get; set; }

        public void Load(string path)
        {
            Path = path;
        }
        public override void SerializeToContext(AvsContext context)
        {
            id = context.Id;
            context.AddData(String.Format(Format, Id));
        }

        public override IList<AvsNode> ChildNodes
        {
            get { return new AvsNode[] {}; }
        }

        protected override string Format 
        {
            get 
            { 
                var clip = string.Format("DirectShowSource(\"{0}\")", Path);
                var frameRate = 25;
                var conv = "Time2Frame({0}, {1})";
                var final = string.Format("AddEmptySoundIfNecessary(DirectShowSource(\"{0}\"))", Path);
                return "{0} = " + final; 
            }
        }
    }
}