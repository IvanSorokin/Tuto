using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Tuto.Model;
using System.Threading;
using Tuto.TutoServices;

namespace Tuto.BatchWorks
{
    //работа-метка, поэтому метода Work() нет
    public class MakeAll : CompositeWork
    {
        public MakeAll(EditorModel model)
        {
            Name = "Make all for: " + model.RawLocation.Name;
            var videoWork = new AssemblyVideoWork(model);
            Tasks.Add(videoWork);
            var serv = new AssemblerService(true);
            var episodes = model.Montage.Information.Episodes;

            //с эпизодами надо что-то решить, в силу того, что эпизодинфо не знает нужной инфы, другого выхода не вижу
            //мы это уже обсуждали, но так и не пришли ни к чему ((

            for (int episodeNumber=0; episodeNumber < episodes.Count; episodeNumber++)
            {
                if (model.Montage.Information.Episodes[episodeNumber].OutputType == OutputTypes.None)
                    continue;


                var from = model.Locations.GetOutputFile(episodeNumber);
                var to = model.Locations.GetFinalOutputFile(episodeNumber);

                if (model.Montage.Information.Episodes[episodeNumber].OutputType == OutputTypes.Patch)
                    continue;

                var task = new MoveFile(from, to, model);

                videoWork.Tasks.Select(x => x as AssemblyEpisodeWork).First(x => x.EpisodeNumber == episodeNumber).Tasks.Add(task);
            }
        }
    }
}
