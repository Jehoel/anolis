﻿using System;
using System.Xml;
using System.Collections.Generic;
using System.IO;
using System.Collections.ObjectModel;

using P = System.IO.Path;

namespace Anolis.Core.Packages.Operations {
	
	public abstract class ExtraOperation : Operation {
		
		protected ExtraOperation(ExtraType type, Package package, XmlElement operationElement) : base(package, operationElement) {
			
			Files = new Collection<String>();
			
			ExtraType = type;
			
			String src = operationElement.GetAttribute("src");
			
			PackageUtility.ResolvePath( src, package.RootDirectory.FullName );
			
			Files.Add( src );
			
		}
		
		protected override String OperationName {
			get { return "X " + ExtraType; }
		}
		
		public String             Attribution { get; private set; }
		public ExtraType          ExtraType   { get; private set; }
		public Collection<String> Files       { get; private set; }
		
		public static ExtraOperation Create(Package package, XmlElement operationElement) {
			
			String typeStr = operationElement.GetAttribute("type");
			ExtraType type = (ExtraType)Enum.Parse( typeof(ExtraType), typeStr, true );
			
			switch(type) {
				case ExtraType.Wallpaper:
					return new WallpaperExtraOperation(package, operationElement);
				case ExtraType.BootScreen:
					return new BootScreenExtraOperation(package, operationElement);
				case ExtraType.CursorScheme:
					return new CursorSetExtraOperation(package, operationElement);
				case ExtraType.VisualStyle:
					return new VisualStyleExtraOperation(package, operationElement);
				case ExtraType.Screensaver:
					return new ScreensaverExtraOperation(package, operationElement);
				case ExtraType.Program:
					return new ProgramExtraOperation(package, operationElement);
				case ExtraType.Custom:
					return new CustomExtraOperation(package, operationElement);
				default:
					return null;
			}
		}
		
		public override Boolean Merge(Operation operation) {
			
			ExtraOperation other = operation as ExtraOperation;
			if(other == null) return false;
			if(other.ExtraType != this.ExtraType) return false;
			
			foreach(String file in other.Files) {
				
				if( !this.Files.Contains( file ) ) this.Files.Add( file );
				
			}
			
			return true;
			
		}
		
	}
	
	public enum ExtraType {
		None,
		Wallpaper,
		BootScreen,
		CursorScheme,
		VisualStyle,
		Screensaver,
		Program,
		Custom
	}
	
	
	
}
