﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Tuto.Model;
using Tuto.TutoServices.Assembler;


namespace Tuto.TutoServices
{
	public class AssemblerService
	{
		private bool CrossFadesEnabled;
		public AssemblerService(bool crossFadesEnabled)
		{
			CrossFadesEnabled = crossFadesEnabled;
		}
		

		public List<AvsNode> GetEpisodesNodes(EditorModel model)
		{
			model.FormPreparedChunks();
            var episodes = ListEpisodes(model.Montage.PreparedChunks).Select(e => MakeEpisode(model, e)).ToList();
			return episodes;
		}

		private bool IsDifferentMode(Mode mode1, Mode mode2)
		{
			var firstVar = mode1 == Mode.Desktop && mode2 == Mode.Face;
			var secondVar = mode1 == Mode.Face && mode2 == Mode.Desktop;
			return firstVar || secondVar;
		}

        private bool IntersectedWithPatch(EditorModel m, StreamChunk chunk)
        {
            var patches = m.Montage.Patches.Where(x => x.IsVideoPatch);
            foreach (var e in patches)
                if (e.Begin >= chunk.StartTime && e.Begin <= chunk.EndTime)
                    return true;
            return false;
        }

		private Tuple<AvsNode, int> GetChainNodeAndNewIndex(int index, List<StreamChunk> initialChunks, int shift, int fps, EditorModel m)
		{
            //ADD PATCH CHECK INTERSECTION
			var finalChunks = new List<StreamChunk>();
			finalChunks.Add(initialChunks[index]);
			for (var i = index; i < initialChunks.Count - 1; i++)
			{
				var currentChunk = initialChunks[i];
				var nextChunk = initialChunks[i + 1];
				if (currentChunk.IsActive && IsDifferentMode(currentChunk.Mode, nextChunk.Mode) && !IntersectedWithPatch(m, nextChunk))
				{
					finalChunks.Add(nextChunk);
				}
				else if (finalChunks.Count > 1)
				{
                    if (finalChunks.Any(x => x.IsNotActive))
                        throw new NotImplementedException();

					AvsConcatList videoChunk = new AvsConcatList();
					videoChunk.Items = new List<AvsNode>() { };
					foreach (var e in finalChunks)
						videoChunk.Items.Add(AvsNode.NormalizedNode(e, fps, currentChunk.Mode == Mode.Face, shift));
					var startTime = finalChunks[0].StartTime;
					var endTime = startTime + finalChunks.Select(x => x.Length).Sum();
					var audioChunk = new StreamChunk(startTime, endTime, Mode.Face, currentChunk.Mode == Mode.Face);
					AvsNode audioAvsChunk = AvsNode.NormalizedNode(audioChunk, fps, currentChunk.Mode == Mode.Face, shift);
					AvsMix mix = new AvsMix();
					mix.First = videoChunk;
					mix.Second = audioAvsChunk;
					return new Tuple<AvsNode, int>(mix, i);
				}
				else return null;
			}
			return null;
		}

        // to insert in currentAvsChunk, new index
        private Tuple<AvsNode, int> ApplyPatch(int index, Patch patch, List<StreamChunk> chunks, int syncShift, EditorModel m)
        {
            var firstPart = new StreamChunk(chunks[index].StartTime, patch.Begin, chunks[index].Mode, false);
            StreamChunk endPart = new StreamChunk(0,0, Mode.Desktop, false);
            AvsNode resultNode = new AvsChunk();

            var tempLayer = new List<StreamChunk>(); //layer for patch
            var newIndex = index;
            for (; newIndex < chunks.Count; newIndex++)
            {
                if (!(chunks[newIndex].EndTime < patch.End))
                {
                    endPart = new StreamChunk(patch.End, chunks[newIndex].EndTime, chunks[newIndex].Mode, false);
                    break;
                }
            }

            var lastActiveEndTime = chunks[chunks.Count - 2].EndTime;
            var layer = new AvsChunk() { Chunk = new StreamChunk(patch.Begin, Math.Min(patch.End, lastActiveEndTime), Mode.Face, false) };

            var patchNode = new AvsPatchChunk();
            var tempNode = new AvsConcatList() { Items = new List<AvsNode>() };
            patchNode.Load((patch.VideoData as VideoFilePatch).GetTempName(m).FullName);

            if (patch.VideoData.OverlayType == VideoPatchOverlayType.Replace)
            {              
                tempNode.Items.Add(new AvsChunk() { Chunk = firstPart});
                tempNode.Items.Add(patchNode);
                if (endPart.IsActive)
                    tempNode.Items.Add(new AvsChunk() { Chunk = endPart });
                resultNode = tempNode;
            }

            if (patch.VideoData.OverlayType == VideoPatchOverlayType.KeepSoundAddSilence)
            {
                tempNode.Items.Add(new AvsChunk() { Chunk = firstPart });

                var mix = new AvsMix() { First = patchNode, Second = layer, SyncShift = syncShift };
                tempNode.Items.Add(mix);
                if (endPart.IsActive)
                    tempNode.Items.Add(new AvsChunk() { Chunk = endPart });
                resultNode = tempNode;
            }
            return Tuple.Create(resultNode, newIndex);
        }

        private bool UseChainProcessing = true;

		private AvsNode MakeEpisode(EditorModel model, EpisodesChunks episode)
		{
			var chunks = episode.chunks;
			var avsChunks = new AvsConcatList { Items = new List<AvsNode>() };
			var fps = 25;
			var shift = model.Montage.SynchronizationShift;
            var patches = model.Montage.Patches.OrderBy(x => x.Begin).ToList();
			var currentChunk = chunks[0];
			//making cross-fades and merging
			for (int i = 0; i < chunks.Count; i++)
			{
                //PATCHES ARE NOT READY YET! WORK IS IN PROGRESS!
                if (patches.Count != 0 && patches[0].Begin >= chunks[i].StartTime && patches[0].Begin <= chunks[i].EndTime )
                {
                    var res = ApplyPatch(i, patches[0], chunks, shift, model);
                    avsChunks.Items.Add(res.Item1);
                    i = res.Item2;
                    patches.RemoveAt(0);
                    continue;

                }

				if (chunks[i].IsNotActive)
					continue;
				var prevChunk = i >= 1 ? currentChunk : null;
				currentChunk = chunks[i];
				AvsNode currentAvsChunk = AvsNode.NormalizedNode(currentChunk, fps, currentChunk.Mode == Mode.Face, shift);
				AvsNode prevAvsChunk = avsChunks.Items.Count >= 1 ? avsChunks.Items[avsChunks.Items.Count - 1] : AvsNode.NormalizedNode(chunks[0], fps, false, shift);
                
                //Оптимизация face-desktop et cetera
                if (UseChainProcessing)
                {
                    var chain = GetChainNodeAndNewIndex(i, chunks, shift, fps, model);
                    if (chain != null)
                    {
                        currentAvsChunk = chain.Item1;
                        i = chain.Item2;
                    }
                }

				if (prevChunk != null && prevChunk.Mode == Mode.Face && currentChunk.Mode == Mode.Face && CrossFadesEnabled)
					avsChunks.Items[avsChunks.Items.Count - 1] = new AvsCrossFade
					{
						FadeFrom = prevAvsChunk,
						FadeTo = currentAvsChunk
					};
				else
					if (currentChunk.IsActive)
						avsChunks.Items.Add(currentAvsChunk);
				currentChunk = chunks[i];
                if (currentChunk.IsNotActive)
                    throw new ArgumentException("!!!");
			}

			// intro with fadein and fadeout
			//var intro = new AvsIntro
			//{
			//    VideoReference = model.Locations.Make(model.ChunkFolder, fileChunks[0].ChunkFilename),
			//    ImageFile = model.Locations.IntroImage
			//};
			//var normalizedIntro = AvsNode.NormalizedNode(intro);
			//var fadedIntro = new AvsFadeIn {Payload = new AvsFadeOut {Payload = normalizedIntro}};
			//avsChunks.Items.Insert(0, fadedIntro);

			// fadeout last item
			avsChunks.Items[avsChunks.Items.Count - 1] = new AvsFadeOut { Payload = avsChunks.Items[avsChunks.Items.Count - 1] };

			//AvsNode resultedAvs = avsChunks;
			//if (!string.IsNullOrEmpty(File.ReadAllText(model.Locations.GetSrtFile(episode.episodeNumber).FullName)))
			//{
			//    resultedAvs = new AvsSubtitle { SrtPath = model.Locations.GetSrtFile(episode.episodeNumber).FullName, Payload = avsChunks };
			//}


			// autolevel
			// ???

			return avsChunks;

			// watermark
			//return new AvsWatermark
			//{
			//    Payload = avsChunks,
			//    ImageFile = model.Locations.WatermarkImage
			//};

			/*
			 * add intro
			 * make crossfades
			 * add fadein/fadeout
			 * add autolevel?
			 * add watermark
			 */
		}


		private class EpisodesChunks
		{
			public List<StreamChunk> chunks = new List<StreamChunk>();
			public int episodeNumber = 0;
		}

		private static List<EpisodesChunks> ListEpisodes(List<StreamChunk> fileChunks)
		{
			var test = fileChunks.Any(z => z.StartsNewEpisode);

			var result = new List<EpisodesChunks>();
			var i = 0;
			while (i < fileChunks.Count)
			{
				var last = new EpisodesChunks();
				last.episodeNumber = result.Count;
				result.Add(last);

				while (i < fileChunks.Count && (!fileChunks[i].StartsNewEpisode || last.chunks.Count == 0))
				{
					last.chunks.Add(fileChunks[i]);
					i++;
				}
			}
			return result;
		}
	}
}