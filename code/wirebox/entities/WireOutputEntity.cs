﻿using System.Collections.Generic;
using System.Linq;

namespace Sandbox
{
	public class WireOutput
	{
		public object value = 0;
		public Entity entity;
		public string outputName;
		public string type;
		public List<WireInput> connected = new();
		public int executions = 0;
		public int executionsTick = 0;

		public WireOutput( Entity entity, string outputName, string type )
		{
			this.entity = entity;
			this.outputName = outputName;
			this.type = type;
		}
	}

	public readonly struct PortType
	{
		public string Name { get; init; }
		public string Type { get; init; }

		public static PortType Bool( string name ) =>
			new() { Name = name, Type = "bool" };
		public static PortType Int( string name ) =>
			new() { Name = name, Type = "int" };
		public static PortType Float( string name ) =>
			new() { Name = name, Type = "float" };
		public static PortType String( string name ) =>
			new() { Name = name, Type = "string" };
		public static PortType Vector3( string name ) =>
			new() { Name = name, Type = "vector3" };
		public static PortType Angle( string name ) =>
			new() { Name = name, Type = "angle" };
		public static PortType Rotation( string name ) =>
			new() { Name = name, Type = "rotation" };
	}

	public interface WireOutputEntity : IWireEntity
	{
		public void WireTriggerOutput<T>( string outputName, T value )
		{
			var output = GetOutput( outputName );
			output.value = value;

			if (output.executionsTick != Time.Tick ) {
				output.executionsTick = Time.Tick;
				output.executions = 0;
			}
			if ( output.executions >= 4 ) {
				// prevent infinite loops
				return; // todo: queue for next tick?
			}
			output.executions++;

			foreach ( var input in output.connected ) {
				if ( !input.entity.IsValid() ) continue;
				if ( input.entity is WireInputEntity inputEntity ) {
					inputEntity.WireTriggerInput( input.inputName, value );
				}
			}
		}
		public void WireConnect( WireInputEntity inputEnt, string outputName, string inputName )
		{
			var input = inputEnt.GetInput( inputName );
			var output = GetOutput( outputName );
			var connected = output.connected;
			if ( input.connectedOutput != null ) {
				inputEnt.DisconnectInput( inputName );
			}
			input.connectedOutput = output;
			connected.Add( input );
			WireTriggerOutput( outputName, output.value );
		}

		public WireOutput GetOutput( string inputName )
		{
			if ( WirePorts.outputs.Count == 0 ) {
				WireInitializeOutputs();
			}
			return WirePorts.outputs[inputName];
		}
		public string[] GetOutputNames( bool withValues = false )
		{
			if ( WirePorts.outputs.Count == 0 ) {
				WireInitializeOutputs();
			}
			return !withValues
				? WirePorts.outputs.Keys.ToArray()
				: WirePorts.outputs.Keys.Select( ( string key ) => {
					return $"{key} [{WirePorts.outputs[key].type}]: {WirePorts.outputs[key].value}";
				} ).ToArray();
		}

		// A thin wrapper, so classes can replaces this as needed
		public virtual void WireInitializeOutputs()
		{
			InitializeOutputs();
		}
		public void InitializeOutputs()
		{
			foreach ( var type in WireGetOutputs() ) {
				WirePorts.outputs[type.Name] = new WireOutput( (Entity)this, type.Name, type.Type );
			}
		}
		abstract public PortType[] WireGetOutputs();
	}

	// Extension methods to allow calling the interface methods without explicit casting
	public static class WireOutputEntityUtils
	{
		public static void WireTriggerOutput<T>( this WireOutputEntity instance, string outputName, T value )
		{
			instance.WireTriggerOutput( outputName, value );
		}
	}

}
