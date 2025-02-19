﻿using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

[Library( "ent_wirekeyboard", Title = "Wire Keyboard" )]
public partial class KeyboardEntity : Prop, IUse, WireOutputEntity
{
	public bool IsToggle { get; set; } = false;
	WirePortData IWireEntity.WirePorts { get; } = new WirePortData();

	private static readonly Dictionary<string, InputButton> inputButtons = new Dictionary<string, InputButton> {
		["Forward"] = InputButton.Forward, //  W
		["Back"] = InputButton.Back, //  S
		["Left"] = InputButton.Left, //  A
		["Right"] = InputButton.Right, //  D
		["Attack1"] = InputButton.Attack1, //  Mouse0
		["Attack2"] = InputButton.Attack2, //  Mouse1
		["Reload"] = InputButton.Reload, //  R
		["Drop"] = InputButton.Drop, //  G
		["Jump"] = InputButton.Jump, //  Spacebar
		["Run"] = InputButton.Run, //  LShift and RShift
		["Walk"] = InputButton.Walk, //  LAlt
		["Duck"] = InputButton.Duck, //  LCtrl and RCtrl
		["Score"] = InputButton.Score, // Tab
		["Menu"] = InputButton.Menu, // Q
		["Flashlight"] = InputButton.Flashlight, // F
		["View"] = InputButton.View, // C
		["Voice"] = InputButton.Voice, // V
		["Slot1"] = InputButton.Slot1,
		["Slot2"] = InputButton.Slot2,
		["Slot3"] = InputButton.Slot3,
		["Slot4"] = InputButton.Slot4,
		["Slot5"] = InputButton.Slot5,
		["Slot6"] = InputButton.Slot6,
		["Slot7"] = InputButton.Slot7,
		["Slot8"] = InputButton.Slot8,
		["Slot9"] = InputButton.Slot9,
	};

	public bool IsUsable( Entity user )
	{
		return user is SandboxPlayer;
	}
	public bool OnUse( Entity user )
	{
		if ( user is SandboxPlayer player ) {
			if ( player.Controller.GetType() != typeof( LockedPositionController ) ) {
				player.OnSimulate -= OnSimulatePlayer;
				player.OnSimulate += OnSimulatePlayer;
				player.EnableSolidCollisions = false;
				player.PhysicsBody.CollisionEnabled = false;
				player.Controller = new LockedPositionController();

				this.WireTriggerOutput( "Active", true );
			}
			else {
				player.OnSimulate -= OnSimulatePlayer;
				player.EnableSolidCollisions = true;
				player.PhysicsBody.CollisionEnabled = true;
				player.Controller = new WalkController();

				this.WireTriggerOutput( "Active", false );
			}
		}
		return false;
	}

	private void OnSimulatePlayer( SandboxPlayer player )
	{
		foreach ( var name in inputButtons.Keys ) {
			var inputButton = inputButtons[name];
			var newState = Input.Down( inputButton );
			if ( newState != (bool)((WireOutputEntity)this).GetOutput( name ).value ) {
				this.WireTriggerOutput( name, newState );
			}
		}
	}
	public PortType[] WireGetOutputs()
	{
		return inputButtons.Keys
			.Concat(new string[]{"Active"})
			.Select(x => PortType.Bool(x))
			.ToArray();
	}
}
