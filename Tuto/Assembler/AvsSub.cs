using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tuto.TutoServices.Assembler
{
    class AvsSub : AvsNode
    {
        public AvsNode Payload;
        public string Content;
        public string FontSize;
        public double Start;
        public double End;
        public string Foreground;
        public string Stroke;

        public override void SerializeToContext(AvsContext context)
        {
            base.id = context.Id;
            Payload.SerializeToContext(context);
            var startTime = Time2Frame(Start);
            var endTime = Time2Frame(End);
            var script = string.Format(@"{0} = {1}.Subtitle(""{2}"", first_frame={3}, align=2, last_frame={4}, size={5}, text_color=color_{6}, halo_color=color_{7})", Id, Payload.Id, Content, startTime, endTime, (int)double.Parse(FontSize), Foreground, Stroke);
            context.AddData(script);
        }

        public override IList<AvsNode> ChildNodes
        {
            get { return new[] { Payload }; }
        }
    }
}
