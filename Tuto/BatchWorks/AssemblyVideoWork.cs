using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Tuto.Model;
using Tuto.TutoServices;
using Tuto.TutoServices.Assembler;

namespace Tuto.BatchWorks
{
    public class AssemblyVideoWork : CompositeWork
    {
        public AssemblyVideoWork (EditorModel model)
        {
            Model = model;
            Name = "Assembling project: " + model.RawLocation.Name;
            var service = new AssemblerService(model.Videotheque.Data.EditorSettings.CrossFadesEnabled);
            var episodes = model.Montage.Information.Episodes;

            var tutoPatches = model.Montage.Patches
                .Where(x => x.Data is TutoPatch)
                .Select(x => x.Data as TutoPatch)
                .ToList();
            if (tutoPatches.Count != 0)
            {
                tutoPatches.ForEach(patch =>
                {
                    var episodeWithModel = Model.Videotheque.EditorModels
                    .Select(x => Tuple.Create(x, x.Montage.Information.Episodes))
                    .Where(x => x.Item2.Any(z => (z.Guid == patch.Guid)))
                    .Select(x => Tuple.Create(x.Item1, x.Item2.First(z => z.Guid == patch.Guid)))
                    .First();

                    var work = new AssemblyEpisodeWork(episodeWithModel.Item1, episodeWithModel.Item2);
                    if (!work.IsAssembled())
                        Tasks.Add(work);
                });
            }

            Model.Montage.Patches.Where(x => x.IsVideoPatch && !(x.Data is TutoPatch)).ToList().ForEach(x => {
            var work = new ConvertPatchWork(model, false, x);
            if (!work.Finished())
                Tasks.Add(work);
            });

            for (var episodeNumber = 0; episodeNumber < episodes.Count; episodeNumber++)
            {
                if (Model.Montage.Information.Episodes[episodeNumber].OutputType == OutputTypes.None)
                    continue;
                var episodeInfo = model.Montage.Information.Episodes[episodeNumber];
                Tasks.Add(new AssemblyEpisodeWork(model,episodeInfo));
            }
        }
    }
}
