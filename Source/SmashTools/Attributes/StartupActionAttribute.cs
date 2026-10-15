using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmashTools
{
	[AttributeUsage(AttributeTargets.Method)]
	public class StartupActionAttribute : Attribute
	{
		/// <summary>
		/// Name assigned to checkbox label in Testing dialog. Overrides method name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Category for splitting up TestFixture cases between different mods
		/// </summary>
		public string Category { get; set; }

		/// <summary>
		/// GameState to initiate TestFixture
		/// </summary>
		/// <remarks>
		/// <para><see cref="GameState.OnStartup"/> executes after the main menu has loaded</para>
		/// <para><see cref="GameState.NewGame"/> executes after a new game has been initialized</para>
		/// <para><see cref="GameState.LoadedSave"/> executes after a loaded save has finished initializing</para>
		/// <para><see cref="GameState.Playing"/> executes for both new games and loaded saves</para>
		/// </remarks>
		public GameState GameState { get; set; } = GameState.Playing;
	}
}
