﻿using System;
using System.IO;

using Cult    = System.Globalization.CultureInfo;
using Marshal = System.Runtime.InteropServices.Marshal;

namespace Anolis.Core.PE {
	
	/// <summary>Encapsulates a Windows Portal Executable resource source.</summary>
	public class PEResourceSource : ResourceSource {
		
		private String   _path;
		private IntPtr   _moduleHandle;
		private ResourceSourceInfo _sourceInfo;
		
		public PEResourceSource(String filename, Boolean readOnly) : base(readOnly) {
			
			FileInfo = new FileInfo( _path = filename );
			if(!FileInfo.Exists) throw new FileNotFoundException("The specified Win32 PE Image was not found", filename);
			
			Reload();
			
		}
		
		public override void CommitChanges(Boolean reload) {
			
			if( IsReadOnly ) throw new InvalidOperationException("Changes cannot be commited because the current ResourceSource is read-only");
			
			// Unload self
			Unload();
			
			IntPtr updateHandle = NativeMethods.BeginUpdateResource( this._path, false );
			
			foreach(ResourceData data in this) {
				
				switch(data.Action) {
					
					case ResourceDataAction.Add:
						
						throw new NotImplementedException();
						
//						break;
						
					case ResourceDataAction.Delete:
						
						throw new NotImplementedException();
						
//						break;
						
					case ResourceDataAction.Update:
						
						IntPtr unmanagedData = Marshal.AllocHGlobal( data.RawData.Length );
						
						Marshal.Copy( data.RawData, 0, unmanagedData, data.RawData.Length );
						
						IntPtr typeId = data.Lang.Name.Type.Identifier.NativeId;
						IntPtr nameId = data.Lang.Name.Identifier.NativeId;
						ushort langId = data.Lang.LanguageId;
						
						NativeMethods.UpdateResource( updateHandle, typeId, nameId, langId, unmanagedData, data.RawData.Length );
						
						Marshal.FreeHGlobal( unmanagedData );
						
						break;
					
				}
				
			}
			
			if(reload) Reload();
			
		}
		
		public override void Reload() {
			
			_moduleHandle = IsReadOnly ?
				NativeMethods.LoadLibraryEx( _path, IntPtr.Zero, NativeMethods.LoadLibraryFlags.LoadLibraryAsDatafile ) :
				NativeMethods.LoadLibrary  ( _path );
			
			if( _moduleHandle == IntPtr.Zero ) {
				
				String win32Message = NativeMethods.GetLastErrorString();
				
				throw new ApplicationException("PE/COFF ResourceSource could not be loaded: " + win32Message);
			}
			
			// Load it up, yo!
			
			GetResources();
			
			
			
		}
		
		private void Unload() {
			
			NativeMethods.FreeLibrary( _moduleHandle );
		}
		
		public override ResourceData GetResourceData(ResourceLang lang) {
			
			if( lang.Name.Type.Source != this ) throw new ArgumentException("Provided resource does not exist in this Image");
			// TODO: Check that ResourceLang refers to a Resource that actually does exist in this resource and is not a pending add operation
			// TODO: Find the TypePtr and NamePtr implementations; they need re-adding to the ResourceIdentifier class
			
			// use FindResourceEx and LoadResource to get a handle to the resource
			// use SizeOfResource to get the length of the byte array
			// then LockResource to get a pointer to it. Use Marshal to get a byte array and take it from there
			
			IntPtr resInfo = NativeMethods.FindResourceEx( _moduleHandle, lang.Name.Type.Identifier.NativeId, lang.Name.Identifier.NativeId, lang.LanguageId );
			IntPtr resData = NativeMethods.LoadResource  ( _moduleHandle, resInfo );
			Int32  size    = NativeMethods.SizeOfResource( _moduleHandle, resInfo );
			
			if(resData == IntPtr.Zero) return null;
			
			IntPtr resPtr  = NativeMethods.LockResource( resData ); // there is no method to unlock resources, but they should be freed anyway
			
			Byte[] data = new Byte[ size ];
			
			Marshal.Copy( resPtr, data, 0, size );
			
			NativeMethods.FreeResource( resData );
			
			ResourceData retval = ResourceData.FromResource(lang, data);
			
			return retval;
			
		}
		
		////////////////////////////
		// Useful implementation-specific bits
		
		public FileInfo FileInfo { get; private set; }
		
		public override ResourceSourceInfo SourceInfo { get { return _sourceInfo; } }
		
		////////////////////////////
		// Destructor / Dispose
		
		~PEResourceSource() {
			Dispose(false);
		}
		
		public override void Dispose() {
			Dispose(true);
			GC.SuppressFinalize( this );
		}
		
		public void Dispose(Boolean disposeManaged) {
			Unload();
		}
		
#region ResourceSourceInfo
		
		private void GetSourceInfo() {
			
			_sourceInfo = new ResourceSourceInfo();
			_sourceInfo.Add("Size"    , FileInfo.Length        .ToString(Cult.InvariantCulture));
			_sourceInfo.Add("Accessed", FileInfo.LastAccessTime.ToString(Cult.InvariantCulture));
			_sourceInfo.Add("Modified", FileInfo.LastWriteTime .ToString(Cult.InvariantCulture));
			_sourceInfo.Add("Created" , FileInfo.CreationTime  .ToString(Cult.InvariantCulture));
			
			using(FileStream stream = FileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read)) {
				
				BinaryReader br = new BinaryReader(stream);
				
				// I CBA to work out how to read a COFF/PE header right now
				
			}
			
			_sourceInfo = null;
			
		}
		
#endregion
		
#region Resource Enumeration
		
		private void GetResources() {
			
			NativeMethods.EnumResTypeProc callback = new NativeMethods.EnumResTypeProc( GetResourceTypesCallback );
			NativeMethods.EnumResourceTypes( _moduleHandle, callback, IntPtr.Zero );
			
		}
		
		private Boolean GetResourceTypesCallback(IntPtr moduleHandle, IntPtr pType, IntPtr userParam) {
			
			ResourceType type = new ResourceType( pType, this );
			
			UnderlyingAdd( _currentType = type );
			
			// enumerate all resources for that type
			NativeMethods.EnumResNameProc callback = new NativeMethods.EnumResNameProc( GetResourceNamesCallback );
			NativeMethods.EnumResourceNames( moduleHandle, pType, callback, IntPtr.Zero );
			
			return true;
			
		}
		
		private ResourceType _currentType;
		
		private Boolean GetResourceNamesCallback(IntPtr moduleHandle, IntPtr pType, IntPtr pName, IntPtr userParam) {
			
			ResourceType type = _currentType;
			
			ResourceName name = new ResourceName( pName, type );
			
			UnderlyingAdd( type, _currentName = name );
			
			NativeMethods.EnumResLangProc callback = new NativeMethods.EnumResLangProc( GetResourceLanguagesCallback );
			NativeMethods.EnumResourceLanguages(moduleHandle, pType, pName, callback, IntPtr.Zero );
			
			return true;
		}
		
		private ResourceName _currentName;
		
		private Boolean GetResourceLanguagesCallback(IntPtr moduleHandle, IntPtr pType, IntPtr pName, UInt16 langId, IntPtr userParam) {
			
			ResourceName name = _currentName;
			
			ResourceLang lang = new ResourceLang( langId, name );
			
			UnderlyingAdd( name, lang );
			
			return true;
		}
		
#endregion
		
#region Resource Updates
		
		
		
#endregion
		
	}
}
