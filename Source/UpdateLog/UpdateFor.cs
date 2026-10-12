using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateLogTool
{
	public enum UpdateFor 
	{ 
		Startup = 0,
		GameInit = 1,
		LoadedGame = 2,
		NewGame = 3
	}
}
