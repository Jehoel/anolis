﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using Anolis.Core.Packages;
using W3b.Wizards.WindowsForms;

namespace Anolis.Installer.Pages {
	
	public partial class ExtractingPage : BaseInteriorPage {
		
		public ExtractingPage() {
			InitializeComponent();
			
			this.PageLoad += new EventHandler(ExtractingPage_PageLoad);
			
			Localize();
		}
		
		protected override String LocalizePrefix { get { return "C_B"; } }
		
		private void ExtractingPage_PageLoad(object sender, EventArgs e) {
			
			WizardForm.EnablePrev = false;
			WizardForm.EnableNext = false;
			
			// Begin extraction
			switch(PackageInfo.Source) {
				
				case PackageSource.File:
					
					// don't call InstantiatePackage from Load (use PageLoad) because it loads the next wizard page before this one finishes loading
					InstantiatePackage( PackageInfo.SourcePath );
					
					break;
					
				case PackageSource.Archive:
				case PackageSource.Embedded:
					
					PackageInfo.Archive.PackageProgressEvent += new EventHandler<PackageProgressEventArgs>(Archive_PackageProgressEvent);
					PackageInfo.Archive.BeginPackageExtract( new Action<String>( Archive_Completed ) );
					
					break;
			}
		}
		
		private void Archive_PackageProgressEvent(object sender, PackageProgressEventArgs e) {
			
			// only fire it if the percentage has moved more than 1%
			float oldPerc = __progress.Value;
			float newPerc = e.Percentage;
			
			if( oldPerc == newPerc ) return; // if percs are the same
			// if newperc is less than 1% different than oldperc, but only if newperc is less than 95%
			if( newPerc < 95 && ( newPerc < oldPerc + 1 ) ) return;
			
			BeginInvoke( new MethodInvoker( delegate() {
				
				__statusLbl.Text = e.Message;
				__progress.Value = e.Percentage;
				
			} ) );
			
		}
		
		private void Archive_Completed(String destDir) {
			
			this.Invoke( new MethodInvoker( delegate() {
				
				if( destDir != null ) {
					
					__statusLbl.Text = InstallerResources.GetString("C_B_instantiating");
					
					InstantiatePackage( destDir );
					
				} else {
					
					// the previous PackageProgressEvent method call will contain the error string, so don't set anything and display a message to the user
					MessageBox.Show(this, InstallerResources.GetString("C_B_error"), "Anolis", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
					
				}
				
			} ) );
			
		}
		
		private void InstantiatePackage(String path) {
			
			try {
				
				if( File.Exists( path ) ) PackageInfo.Package = Package.FromFile( path );
				else                      PackageInfo.Package = Package.FromDirectory( path );
				
			} catch( PackageValidationException pve ) {
				
				__packageMessages.Visible = true;
				
				StringBuilder sb = new StringBuilder();
				sb.AppendLine( pve.Message );
				foreach(System.Xml.Schema.ValidationEventArgs ve in pve.ValidationErrors) {
					sb.Append( ve.Severity );
					sb.Append(" ");
					sb.Append( ve.Message );
					sb.Append(" ");
					if( ve.Exception != null ) {
						sb.Append( ve.Exception.Message );
						sb.Append(" (");
						sb.Append( ve.Exception.LineNumber );
						sb.Append(", ");
						sb.Append( ve.Exception.LinePosition );
						sb.Append(")");
					}
					
					sb.AppendLine();
				}
				
				__packageMessages.Text = sb.ToString();
				
				return;
				
				
			} catch(PackageException pe ) {
				
				__packageMessages.Visible = true;
				
				__packageMessages.Text = pe.GetType().Name + " " + pe.Message;
				
				return;
				
			}
			
			//////////////////////////////////////////////////
			
			EvaluationInfo info = PackageInfo.Package.EvaluateCondition();
			if( info.Result == EvaluationResult.False ) {
				
				MessageBox.Show(this, info.Message, "Anolis Installer", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
				WizardForm.LoadPage( Program.PageCASelectPackage );
				return;
				
			} else if( info.Result == EvaluationResult.Error ) {
				
				MessageBox.Show(this, "There was an error whilst attempting to evaluate the package's suitability for your computer. Contact the package's author.", "Anolis Installer", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
				WizardForm.LoadPage( Program.PageCASelectPackage );
				return;
				
			}
			
			WizardForm.LoadPage( Program.PageCCUpdatePackage );
			
		}
		
		public override BaseWizardPage PrevPage {
			get { return null; }
		}
		
		public override BaseWizardPage NextPage {
			get { return null; }
		}
		
	}
}
