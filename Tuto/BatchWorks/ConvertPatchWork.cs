using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Tuto.Model;
using System.Threading;

namespace Tuto.BatchWorks
{
    public class ConvertPatchWork : ConvertVideoWork
    {

        private Patch patch;
        public ConvertPatchWork(EditorModel model, bool forced, Patch patch)
        {
            Model = model;
            this.patch = patch;
            Name = "Converting Patch: " + (patch.VideoData as VideoFilePatch).GetFileName(model.Videotheque).Name;
            Source = (patch.VideoData as VideoFilePatch).GetFileName(model.Videotheque);
            Forced = forced;
        }

        public override bool Finished()
        {
            return (patch.VideoData as VideoFilePatch).GetTempName(Model).Exists;
        }

        public override void Clean()
        {
            FinishProcess();
            TryToDelete(TempFile.FullName);
        }
    }
}
