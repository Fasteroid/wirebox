﻿using System;
namespace Sandbox.Tools
{
	[Library( "tool_wiregps", Title = "Wire GPS", Description = "Create a Wire GPS for retrieving position data", Group = "construction" )]
	public partial class GPSTool : BaseWireTool
	{
		[ConVar.ClientData( "tool_wiregps_model" )]
		public static string _ { get; set; } = "models/citizen_props/icecreamcone01.vmdl";

		protected override Type GetEntityType()
		{
			return typeof( WireGPSEntity );
		}
		protected override ModelEntity SpawnEntity( TraceResult tr )
		{
			return new WireGPSEntity {
				Position = tr.EndPos,
				Rotation = Rotation.LookAt( tr.Normal, tr.Direction ) * Rotation.From( new Angles( 90, 0, 0 ) )
			};
		}
		protected override string[] GetSpawnLists()
		{
			return new string[] { "gps", "controller" };
		}
	}
}
