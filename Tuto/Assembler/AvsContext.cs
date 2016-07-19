using System;
using System.IO;
using System.Text;
using Tuto.Model;

namespace Tuto.TutoServices.Assembler
{
   public class AvsContext
    {
        public void AddData(string data)
        {
            internalData.Append(data);
            internalData.AppendLine();
        }

        public string Serialize(EditorModel model)
        {
            var faceFormat = "";
            var withoutReduction = string.Format(@"face = DirectShowSource(""{0}"").ChangeFPS(25)", model.Locations.ConvertedFaceVideo.FullName);
            var withReduction = string.Format(
                @"face = audiodub(DirectShowSource(""{0}"").ChangeFPS(25).KillAudio(),  DirectShowSource(""{1}""))",
                model.Locations.ConvertedFaceVideo.FullName,
                model.Locations.ClearedSound);
            if (model.Locations.ClearedSound.Exists)
                faceFormat = withReduction;
            else
                faceFormat = withoutReduction;

            var desktopFormat = string.Format(@"desktop = DirectShowSource(""{0}"").ChangeFPS(25)", model.Locations.ConvertedDesktopVideo.FullName);
            var desktopHeader = model.Locations.ConvertedDesktopVideo.Exists ? desktopFormat : "";

            return string.Format(Format,
                model.Locations.AvsLibrary.FullName,
                model.Locations.VSFilterLibrary.FullName,
                model.Locations.VSFilterLibrary.FullName,
                internalData,
                String.Format(AvsNode.Template, 0),
                faceFormat,
                desktopHeader); 
        }

       public string GetContent()
        {
            return internalData.ToString();
        }

        public int Id { get { id++;
            return id;
        } }

        private int id = -1;
        private const string Format =
@"import(""{0}"")
Loadplugin (""{1}"")
{5}
{6}
{3}
return {4}";

        private readonly StringBuilder internalData = new StringBuilder();
        
    }
}