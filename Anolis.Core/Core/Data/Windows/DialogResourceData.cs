﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Anolis.Core.Data {
	
	public class DialogResourceDataFactory : ResourceDataFactory {
		
		public override Compatibility HandlesType(ResourceTypeIdentifier typeId) {
			throw new NotImplementedException();
		}
		
		public override Compatibility HandlesExtension(String filenameExtension) {
			throw new NotImplementedException();
		}
		
		public override ResourceData FromResource(ResourceLang lang, Byte[] data) {
			
			return DialogResourceData.TryCreate(lang, data);
			
		}
		
		public override ResourceData FromFile(Stream stream, String extension, ResourceSource currentSource) {
			throw new NotImplementedException();
		}
		
		public override String Name {
			get { throw new NotImplementedException(); }
		}
		
		public override String OpenFileFilter {
			get { throw new NotImplementedException(); }
		}
	}
	
	public class DialogResourceData : ResourceData {
		
		private DialogResourceData(ResourceLang lang, Byte[] rawData) : base(lang, rawData) {
			
		}
		
		internal static DialogResourceData TryCreate(ResourceLang lang, Byte[] rawData) {
			
			
			return null;
			
		}
		
		protected override void SaveAs(System.IO.Stream stream, String extension) {
			throw new NotImplementedException();
		}
		
		protected override String[] SupportedFilters {
			get { return new String[] {}; }
		}
		
		protected override ResourceTypeIdentifier GetRecommendedTypeId() {
			return new ResourceTypeIdentifier(Win32ResourceType.Dialog);
		}
	}
	
	
}
