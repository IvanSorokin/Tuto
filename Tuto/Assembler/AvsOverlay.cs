using System.Collections.Generic;

namespace Tuto.TutoServices.Assembler
{
    public class AvsOverlay : AvsNode
    {
        public AvsNode First { get; set; }

        public AvsNode Second { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public override void SerializeToContext(AvsContext context)
        {
            id = context.Id;
            First.SerializeToContext(context);
            Second.SerializeToContext(context);
            var script = string.Format(Format, Id, First.Id, Second.Id, X, Y);
            context.AddData(script);
        }

        public override IList<AvsNode> ChildNodes
        {
            get { return new[] { First, Second }; }
        }

        protected override string Format { get { return "{0} = Overlay({1}, {2}, x={3}, y={4})"; } }
    }
}