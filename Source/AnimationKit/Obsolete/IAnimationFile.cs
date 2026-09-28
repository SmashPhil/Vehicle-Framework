using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools.Animations
{
	public interface IAnimationFile : IXmlExport
	{
		Guid Guid { get; }

		string FilePath { get; set; }

		string FileName { get; set; }

		string FileNameWithExtension { get; }

		void ResolveReferences();
	}
}
