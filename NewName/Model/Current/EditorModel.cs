﻿using Editor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;


namespace Tuto.Model
{
    public class EditorModel
    {
        public readonly DirectoryInfo RootFolder;
        public readonly DirectoryInfo VideoFolder;
        public readonly DirectoryInfo ProgramFolder;
        public readonly DirectoryInfo ChunkFolder;

        public Locations Locations { get; private set; }
        public GlobalData Global { get; set; }
        public MontageModel Montage { get; set; }
        public WindowState WindowState { get; set; }

        public event EventHandler MontageModelChanged;

        public void OnMontageModelChanged()
        {
            if (MontageModelChanged != null)
                MontageModelChanged(this, EventArgs.Empty);
        }


        public EditorModel(DirectoryInfo local, DirectoryInfo global, DirectoryInfo program)
        {
            this.VideoFolder=local;
            this.RootFolder=global;
            this.ProgramFolder=program;
            ChunkFolder = VideoFolder.CreateSubdirectory("chunks");
            Montage = new MontageModel(360000);
            Locations = new Locations(this);
            WindowState = new WindowState();
            Global = new GlobalData();
        }


        #region Basic algorithms

        private StreamTokenArray Tokens { get { return Montage.Tokens; } }

        public int FindChunkIndex(int time)
        {
            return Tokens.FindIndex(time);
        }

        public void MoveLeftChunkBorder(int index, int newTime)
        {
            if (index == 0) return;
            Tokens.MoveToken(index, newTime);
            OnMontageModelChanged();
        }

        public void MoveRightChunkBorder(int index, int newTime)
        {
            if (index == Tokens.Count - 1) return;
            Tokens.MoveToken(index + 1, newTime);
            OnMontageModelChanged();

        }

        public void ShiftLeftChunkBorder(int index, int deltaTime)
        {
            if (index == 0) return;
            Tokens.MoveToken(index, Tokens[index].StartTime + deltaTime);
            OnMontageModelChanged();

        }

        public void ShiftRightChunkBorder(int index, int deltaTime)
        {
            if (index == Tokens.Count - 1) return;
            Tokens.MoveToken(index + 1, Tokens[index].EndTime + deltaTime);
            OnMontageModelChanged();
        }
        
        #endregion
        #region Algorithms using WindowState properies

        static public bool[] ModeToBools(Mode mode)
        {
            if (mode == Mode.Drop) return new bool[] { false, false };
            if (mode == Mode.Face) return new bool[] { true, false };
            if (mode == Mode.Screen) return new bool[] { false, true};
            throw new ArgumentException();

        }

        public void MarkHere(Mode mode, bool ctrl)
        {
            var time=WindowState.CurrentPosition;
            Tokens.Mark(time, ModeToBools(mode), !ctrl);
            var index = Tokens.FindIndex(time);
            CorrectBorderBetweenChunksBySound(index - 1);
            CorrectBorderBetweenChunksBySound(index);
            OnMontageModelChanged();
        }

        public void RemoveChunkHere()
        {
            var position = WindowState.CurrentPosition;
            var index = Tokens.FindIndex(position);
            Tokens.Clear(index);
            OnMontageModelChanged();
        }

        
        public void NewEpisodeHere()
        {
            var index = Tokens.FindIndex(WindowState.CurrentPosition);
            if (index != -1)
            {
                Tokens.NewEpisode(index);
                OnMontageModelChanged();
            }
        }
        #endregion
        #region Correction by sound
        public void CorrectBorderBetweenChunksBySound(int leftChunkIndex)
        {
            if (leftChunkIndex < 0) return;
            var rightChunkIndex = leftChunkIndex + 1;
            if (rightChunkIndex >= Tokens.Count) return;
            var leftChunk = Tokens[leftChunkIndex];
            var rightChunk = Tokens[rightChunkIndex];
            if (leftChunk.Mode== Mode.Undefined || rightChunk.Mode == Mode.Undefined) return;

            var interval = Montage.SoundIntervals
                .Where(z => !z.HasVoice && z.DistanceTo(rightChunk.StartTime) < Global.VoiceSettings.MaxDistanceToSilence)
                .FirstOrDefault();
            if (interval == null) return;

            var leftDistance = Math.Abs(interval.StartTime - rightChunk.StartTime);
            var rightDistance = Math.Abs(interval.EndTime - rightChunk.StartTime);
            var distance = interval.DistanceTo(rightChunk.StartTime);
            bool LeftIn = leftDistance < Global.VoiceSettings.MaxDistanceToSilence;
            bool RightIn = rightDistance < Global.VoiceSettings.MaxDistanceToSilence;

            if (!LeftIn && !RightIn) return;

            int NewStart = rightChunk.StartTime;
            if (LeftIn && RightIn)
            {
                //значит, оба конца интервала - близко от точки сечения, и точку нужно передвинуть на середину интервада
                NewStart = interval.MiddleTime;
            }
            else if (LeftIn && !RightIn)
            {
                //значит, только левая граница где-то недалеко. 
                NewStart = interval.StartTime + Global.VoiceSettings.SilenceMargin;
            }
            else if (!LeftIn && RightIn)
            {
                NewStart = interval.EndTime - Global.VoiceSettings.SilenceMargin;
            }

            //не вылезли за границы интервала при перемещении
            if (interval.DistanceTo(NewStart) > 0) return;

            //не выскочили за границы чанков при перемещении
            if (!rightChunk.Contains(NewStart) && !leftChunk.Contains(NewStart)) return;

            Tokens.MoveToken(rightChunkIndex, NewStart);
        }
        #endregion
        #region Creation of FileChunks
        public void CreateFileChunks()
        {
            // Collapse adjacent chunks of same type into one FileChunk
            Montage.FileChunks = new List<FileChunk>();
            var activeChunks = Tokens.Chunks.Where(c => c.IsActive).ToList();
            activeChunks.Add(new StreamChunk(activeChunks.Last().EndTime,activeChunks.Last().EndTime,Mode.Undefined,true));
            var oldChunk = activeChunks[0];
            for (var i = 1; i < activeChunks.Count; i++)
            {
                var currentChunk = activeChunks[i];
                var prevChunk = activeChunks[i - 1];
                // collect adjacent chunks starting with oldChunk
                if (!currentChunk.StartsNewEpisode && currentChunk.Mode == oldChunk.Mode)
                    continue;
                // or flush adjacent chunks into one and start new sequence
                Montage.FileChunks.Add(new FileChunk
                {
                    Mode = oldChunk.Mode,
                    StartTime = oldChunk.StartTime,
                    Length = prevChunk.EndTime - oldChunk.StartTime,
                    //SourceFilename = oldChunk.Mode == Mode.Face ? Locations.FaceVideo : Locations.DesktopVideo,
                    StartsNewEpisode = oldChunk.StartsNewEpisode
                });
                oldChunk = currentChunk;
            }
        }
        #endregion
        #region Saving and loading

        public static EditorModel Load(string subdirectory)
        {
            var localDirectory = new DirectoryInfo(subdirectory);
            if (!localDirectory.Exists) throw new Exception("Local directory '" + subdirectory + "' is not found");
            var rootDirectory = localDirectory;
            while (true)
            {
                try
                {
                    rootDirectory = rootDirectory.Parent;
                }
                catch
                {
                    throw new Exception("Root directory is not found. Root directory must be a parent of '" + localDirectory.FullName + "' and contain global data file '" + Locations.GlobalFileName + "'");
                }
                if (rootDirectory.GetFiles(Locations.GlobalFileName).Length != 0)
                    break;
            }

            var programFolder = new FileInfo(Assembly.GetExecutingAssembly().FullName).Directory;

            EditorModel model = new EditorModel(localDirectory, rootDirectory, programFolder);

            var file = localDirectory.GetFiles(Locations.LocalFileName).FirstOrDefault();
            if (file == null)
            {
                var oldModel = ObsoleteModelIO.LoadAndConvert(subdirectory); //try to recover model from obsolete file formats
                if (oldModel != null) model = oldModel;
                else //no files at all. Create an empty model
                {
                    model.Montage = new MontageModel(60 * 60 * 1000); //this is very bad. Need to analyze the video file
                    model.WindowState = new WindowState();
                }
            }
            else
            {

                FileContainer container = null;
                using (var stream = File.Open(file.FullName, FileMode.Open, FileAccess.Read))
                {
                    container = (FileContainer)new DataContractJsonSerializer(typeof(FileContainer)).ReadObject(stream);
                }
                model.Montage = container.MontageModel;
                model.WindowState = container.WindowState;
            }

            file = rootDirectory.GetFiles(Locations.GlobalFileName).FirstOrDefault();
            if (file == null)
            {
                model.Global = new GlobalData();
            }
            else
            {
                using (var stream = File.Open(file.FullName, FileMode.Open, FileAccess.Read))
                {
                    model.Global = (GlobalData)new DataContractJsonSerializer(typeof(GlobalData)).ReadObject(stream);
                }
            }
            return model;
        }

        public void Save()
        {
            var container = new FileContainer();
            container.MontageModel = Montage;
            container.WindowState = WindowState;
            using(var stream=File.Open(Locations.LocalFilePath.FullName,FileMode.OpenOrCreate,FileAccess.Write))
            {
                new DataContractJsonSerializer(typeof(FileContainer)).WriteObject(stream, container);
            }
        }

        #endregion
    }
}

