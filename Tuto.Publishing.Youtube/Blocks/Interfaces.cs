﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Tuto.Model;
using Tuto.Navigator;

namespace Tuto.Publishing
{
	public interface IMaterialSource
	{
		void Initialize(PublishingSettings settings);
		void Load(Item root);
		void Pull(Item root);
		void Save(Item root);
		ICommandBlockModel ForVideo(VideoWrap wrap);
		ICommandBlockModel ForLecture(LectureWrap wrap);
	}

	public class VisualCommand
	{
		public RelayCommand Command { get; private set; }
		public Uri ImageSource { get; private set; }
		public VisualCommand(RelayCommand command, string imageName)
		{
			Command = command;
			ImageSource = new Uri(@"/Img/" + imageName, UriKind.Relative);
		}
        public VisualCommand(Action action, Func<bool> actionAvailable, string imageName)
            : this(new RelayCommand(action, actionAvailable), imageName)
        { }
	}

	public interface ICommandBlockModel
	{
		List<VisualCommand> Commands { get; }
		Uri ImageSource { get; }
		BlockStatus Status { get;  }
	}

	public interface ICommandBlocksHolder
	{
		List<ICommandBlockModel> CommandBlocks { get; }
	}

	public static class IItemExtension
	{
		public static IEnumerable<TCommandBlock> ChildCommandBlocks<TCommandBlock>(this Item item)
		{
			return item.Subtree().OfType<ICommandBlocksHolder>().SelectMany(z => z.CommandBlocks.OfType<TCommandBlock>());
		}
	}
}
