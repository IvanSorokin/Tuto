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
        public int X;
        public int Y;
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
            var script = string.Format(@"{0} = {1}.Subtitle(""{2}"", x={3}, y={4}, first_frame={5}, last_frame={6}, size={7}, text_color=color_{8}, halo_color=color_{9})", Id, Payload.Id, Content, X, Y, startTime, endTime, (int)double.Parse(FontSize), Foreground, Stroke);
            context.AddData(script);
        }

        public override IList<AvsNode> ChildNodes
        {
            get { return new[] { Payload }; }
        }
    }
}
