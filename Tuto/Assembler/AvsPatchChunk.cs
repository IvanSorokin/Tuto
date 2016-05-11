
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
        public int EndTime { get; set; }
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
                var final = string.Format("AddEmptySoundIfNecessary(DirectShowSource(\"{0}\"))", Path);
                var time = Time2Frame(EndTime);
                if (EndTime != 0)
                    final = string.Format("Trim(AddEmptySoundIfNecessary(DirectShowSource(\"{0}\")),0,{1})", Path, time);
                return "{0} = " + final; 
            }
        }
    }
}