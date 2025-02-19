﻿using System;
using System.Linq;
using Sandbox.UI;
using System.Text.Json;
using Sandbox.UI.Construct;

namespace Sandbox.Tools
{
	[Library( "tool_wiring", Title = "Wiring", Description = "Wire entities together", Group = "construction" )]
	public partial class WiringTool : BaseTool
	{
		[Net, Local]
		private Entity inputEnt { get; set; }
		[Net, Local]
		private Vector3 inputPos { get; set; }

		private WireGatePanel wireGatePanel;
		private WiringPanel wiringHud;

		// Cache the inputs/outputs here, so we can network them to the client, as only the server knows the current port values
		// These would be tidier over in the HUD class, but [Net] seems buggy over there
		[Net, Local]
		public string NetInputs { get; set; } = "";
		[Net, Local]
		public string NetOutputs { get; set; } = "";

		[Net, Local]
		private int InputPortIndex { get; set; } = 0;
		[Net, Local]
		private int OutputPortIndex { get; set; } = 0;

		[ConVar.ClientData( "tool_wiring_model" )]
		public string _ { get; set; } = "models/wirebox/katlatze/chip_rectangle.vmdl";
		[ConVar.ClientData( "tool_wiring_materialgroup" )]
		public int _2 { get; set; } = 0;

		public override void Simulate()
		{
			using ( Prediction.Off() ) {
				var startPos = Owner.EyePos;
				var dir = Owner.EyeRot.Forward;

				var tr = Trace.Ray( startPos, startPos + dir * MaxTraceDistance )
					.Ignore( Owner )
					.Run();


				if ( inputEnt is WireInputEntity wireInputEnt ) {
					ShowInputs( wireInputEnt, true );
					if ( tr.Entity is WireOutputEntity wireOutputProp1 ) {
						if ( Host.IsServer ) {
							OutputPortIndex = Math.Clamp( OutputPortIndex - Input.MouseWheel, 0, Math.Max( 0, wireOutputProp1.GetOutputNames().Length - 1 ) );
						}
						ShowOutputs( wireOutputProp1, true );
					}
					else {
						ShowOutputs( null );
					}
				}
				else {
					if ( tr.Entity is WireInputEntity wireInputEnt2 ) {
						if ( Host.IsServer ) {
							InputPortIndex = Math.Clamp( InputPortIndex - Input.MouseWheel, 0, Math.Max( 0, wireInputEnt2.GetInputNames().Length - 1 ) );
						}
						ShowInputs( wireInputEnt2, false );
					}
					else {
						ShowInputs( null, false );
					}
					if ( tr.Entity is WireOutputEntity wireOutputProp2 ) {
						ShowOutputs( wireOutputProp2 );
					}
					else {
						ShowOutputs( null );
					}
				}



				if ( Input.Pressed( InputButton.Attack1 ) ) {

					if ( !tr.Hit || !tr.Body.IsValid() || !tr.Entity.IsValid() || tr.Entity.IsWorld )
						return;

					if ( !inputEnt.IsValid() ) {
						// stage 1

						if ( tr.Entity is not WireInputEntity wireProp || wireProp.GetInputNames().Length == 0 )
							return;
						if ( Host.IsClient ) {
							CreateHitEffects( tr.EndPos, tr.Normal );
							return;
						}
						inputEnt = tr.Entity;
						inputPos = tr.EndPos;
					}
					else {
						// stage 2
						if ( inputEnt is not WireInputEntity wireInputProp )
							return;
						if ( tr.Entity is not WireOutputEntity wireOutputProp || wireOutputProp.GetOutputNames().Length == 0 )
							return;

						if ( Host.IsServer ) {
							var outputName = wireOutputProp.GetOutputNames()[OutputPortIndex];
							var inputName = wireInputProp.GetInputNames()[InputPortIndex];

							// Log.Info("Wiring " + wireInputProp + "'s " + inputName + " to " + wireOutputProp + "'s " + outputName);
							wireOutputProp.WireConnect( wireInputProp, outputName, inputName );

							var attachEnt = tr.Body.IsValid() ? tr.Body.Entity : tr.Entity;
							var rope = new WireCable( "particles/wirebox/wire.vpcf", inputEnt, attachEnt );
							rope.Particle.SetEntity( 0, inputEnt, inputEnt.Transform.PointToLocal( inputPos ) );

							var attachLocalPos = tr.Body.Transform.PointToLocal( tr.EndPos );
							if ( attachEnt.IsWorld ) {
								rope.Particle.SetPosition( 1, attachLocalPos );
							}
							else {
								rope.Particle.SetEntityBone( 1, attachEnt, tr.Bone, new Transform( attachLocalPos ) );
							}
							wireInputProp.WirePorts.inputs[inputName].AttachRope = rope;
						}
						Reset();
					}
				}
				else if ( Input.Pressed( InputButton.Attack2 ) ) {
					if ( Host.IsClient ) {
						return;
					}
					var portDirection = Input.Down( InputButton.Run ) ? -1 : 1;

					if ( inputEnt is WireInputEntity ) {
						OutputPortIndex += portDirection;
					}
					else {
						InputPortIndex += portDirection;
					}

					return;
				}
				else if ( Input.Pressed( InputButton.Reload ) ) {
					if ( tr.Entity is WireInputEntity wireEntity && Host.IsServer ) {
						wireEntity.DisconnectInput( wireEntity.GetInputNames()[InputPortIndex] );
					}
					else {
						Reset();
					}
				}
				else {
					return;
				}
				if ( Host.IsClient ) {
					CreateHitEffects( tr.EndPos, tr.Normal );
				}
			}
		}

		private void Reset()
		{
			inputEnt = null;
			InputPortIndex = 0;
			OutputPortIndex = 0;
			ShowInputs( null );
			ShowOutputs( null );
		}


		public override void Activate()
		{
			base.Activate();

			if ( Host.IsClient ) {
				Local.Hud.StyleSheet.Load( "/wirebox/ui/wiringhud.scss" );
				wiringHud = Local.Hud.AddChild<WiringPanel>();
				wireGatePanel = Local.Hud.AddChild<WireGatePanel>( "wire-gate-menu" );

				var modelSelector = new ModelSelector( new string[] { "gate", "controller" } );
				SpawnMenu.Instance?.ToolPanel?.AddChild( modelSelector );
			}
			Reset();
		}

		public override void Deactivate()
		{
			base.Deactivate();
			if ( Host.IsClient ) {
				wireGatePanel?.Delete( true );
				wiringHud?.Delete();
			}
			Reset();
		}

		[ServerCmd( "wire_spawn_gate" )]
		public static void SpawnGate( string gateType )
		{
			var owner = ConsoleSystem.Caller?.Pawn;

			if ( ConsoleSystem.Caller == null )
				return;

			var tr = Trace.Ray( owner.EyePos, owner.EyePos + owner.EyeRot.Forward * 500 )
			  .UseHitboxes()
			  .Ignore( owner )
			  .Size( 2 )
			  .Run();

			if ( tr.Entity is WireGateEntity wireGateEntity ) {
				wireGateEntity.Update(gateType);
				if ( owner.Inventory.Active is Tool toolgun && toolgun.CurrentTool is WiringTool wiringTool) {
					wiringTool.CreateHitEffects( tr.EndPos, tr.Normal );
				}
				return;
			}

			var ent = new WireGateEntity {
				Position = tr.EndPos,
				Rotation = Rotation.LookAt( tr.Normal, tr.Direction ) * Rotation.From( new Angles( 90, 0, 0 ) ),
				GateType = gateType,
			};
			ent.SetModel( ConsoleSystem.Caller.GetUserString( "tool_wiring_model" ) );
			int.TryParse(ConsoleSystem.Caller.GetUserString("tool_wiring_materialgroup"), out int matGroup);
			ent.SetMaterialGroup(matGroup);

			var attachEnt = tr.Body.IsValid() ? tr.Body.Entity : tr.Entity;
			if ( attachEnt.IsValid() ) {
				ent.SetParent( tr.Body.Entity, tr.Body.PhysicsGroup.GetBodyBoneName( tr.Body ) );
			}
			if ( owner.Inventory.Active is Tool toolgun2 && toolgun2.CurrentTool is WiringTool wiringTool2) {
				wiringTool2.CreateHitEffects( tr.EndPos, tr.Normal );
			}
			Sandbox.Hooks.Entities.TriggerOnSpawned( ent, owner );
		}


		// A wrapper around wiringHud.SetInputs that helps sync the server port state to the client for display
		private void ShowInputs( WireInputEntity ent, bool entSelected = false )
		{
			string[] names = Array.Empty<string>();
			if ( ent != null ) {
				if ( Host.IsServer ) {
					names = ent.GetInputNames( true );
					NetInputs = JsonSerializer.Serialize( names ); // serialize em, as [Net] errors on string[]'s
				}
				else {
					names = NetInputs?.Length > 0 ? JsonSerializer.Deserialize<string[]>( NetInputs ) : ent.GetInputNames();
				}
			}
			if ( Host.IsClient ) {
				wiringHud?.SetInputs( names, entSelected, InputPortIndex );
			}
		}
		private void ShowOutputs( WireOutputEntity ent, bool selectingOutput = false )
		{
			string[] names = Array.Empty<string>();
			if ( ent != null ) {
				if ( Host.IsServer ) {
					names = ent.GetOutputNames( true );
					NetOutputs = JsonSerializer.Serialize( names );
				}
				else {
					names = NetOutputs?.Length > 0 ? JsonSerializer.Deserialize<string[]>( NetOutputs ) : ent.GetOutputNames();
				}
			}

			if ( Host.IsClient ) {
				wiringHud?.SetOutputs( names, selectingOutput, OutputPortIndex );
			}
		}
	}

	public partial class WiringPanel : Panel
	{
		private string[] lastInputs = System.Array.Empty<string>();
		private string[] lastOutputs = System.Array.Empty<string>();
		public Panel InputsPanel { get; set; }
		public Panel OutputsPanel { get; set; }

		public WiringPanel()
		{
			SetTemplate( "/wirebox/ui/wiringhud.html" );
			InputsPanel = GetChild( 0 ).GetChild( 0 );
			OutputsPanel = GetChild( 0 ).GetChild( 1 );
		}

		public void SetInputs( string[] names, bool selected = false, int portIndex = 0 )
		{
			if ( Local.Pawn is SandboxPlayer sandboxPlayer ) {
				sandboxPlayer.SuppressScrollWheelInventory = names.Length != 0;
			}
			foreach ( var lineItem in InputsPanel.GetChild( 1 ).Children ) {
				lineItem.SetClass( "active", InputsPanel.GetChild( 1 ).GetChildIndex( lineItem ) == portIndex );
			}
			InputsPanel.SetClass( "selected", selected );
			if ( Enumerable.SequenceEqual( lastInputs, names ) ) {
				return;
			}
			lastInputs = names;
			InputsPanel.GetChild( 1 ).DeleteChildren( true );

			foreach ( var name in names ) {
				InputsPanel.GetChild( 1 ).AddChild<Label>( "port" ).SetText( name );
			}
		}
		public void SetOutputs( string[] names, bool selectingOutput = false, int portIndex = 0 )
		{
			foreach ( var lineItem in OutputsPanel.GetChild( 1 ).Children ) {
				lineItem.SetClass( "active", selectingOutput && OutputsPanel.GetChild( 1 ).GetChildIndex( lineItem ) == portIndex );
			}
			OutputsPanel.SetClass( "selected", selectingOutput );
			if ( Enumerable.SequenceEqual( lastOutputs, names ) ) {
				return;
			}
			lastOutputs = names;
			OutputsPanel.GetChild( 1 ).DeleteChildren( true );
			OutputsPanel.SetClass( "visible", names.Length != 0 );

			foreach ( var name in names ) {
				OutputsPanel.GetChild( 1 ).AddChild<Label>( "port" ).SetText( name );
			}
		}
	}

	public partial class WireGatePanel : Panel
	{
		public WireGatePanel()
		{
			var container = Add.Panel( "wire-gate-container" );
			foreach ( var kvp in WireGateEntity.GetGates() ) {
				var category = kvp.Key;
				var gates = kvp.Value;

				var categoryRow = container.Add.Panel( "wire-gate-category" );
				var categoryText = categoryRow.Add.TextEntry( category );
				categoryText.AddClass( "wire-gate-category-label" );
				var categoryList = categoryRow.Add.Panel( "wire-gate-category-list" );
				foreach ( var gateName in gates ) {
					categoryList.Add.Button( gateName, () => {
						ConsoleSystem.Run( "wire_spawn_gate", gateName );
					} );
				}
			}
		}
		public override void Tick()
		{
			base.Tick();
			SetClass( "visible", Input.Down( InputButton.Drop ) );
		}
	}
}
