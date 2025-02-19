﻿using Sandbox;

[Library( "ent_lightbridge", Title = "Light Bridge", Spawnable = true )]
public partial class LightBridgeEntity : Prop, WireInputEntity
{
	private MeshEntity bridgeEntity;
	WirePortData IWireEntity.WirePorts { get; } = new WirePortData();

	public void WireInitialize()
	{
		this.RegisterInputHandler( "Length", ( float length ) => {
			if ( length < 10 ) {
				bridgeEntity?.Delete();
				return;
			}
			if ( !bridgeEntity.IsValid() ) {
				bridgeEntity = VertexMeshBuilder.SpawnEntity( (int)length, 100, 1, 64 );
				bridgeEntity.Position = Transform.PointToWorld( new Vector3( 4, -50, 9.5f ) - bridgeEntity.CollisionBounds.Mins );
				bridgeEntity.Rotation = Rotation;
				bridgeEntity.MaterialOverride = "materials/wirebox/katlatze/metal.vmat";
				bridgeEntity.RenderColorAndAlpha = new Color32( 0, 90, 255, 180 );
				this.Weld( bridgeEntity );
			}
			else {
				bridgeEntity.Model = VertexMeshBuilder.GenerateRectangleServer( (int)length, 100, 1, 64 );
				bridgeEntity.Tick();
				bridgeEntity.Position = Transform.PointToWorld( new Vector3( 4, -50, 9.5f ) - bridgeEntity.CollisionBounds.Mins );
			}
		} );
	}

	protected override void OnDestroy()
	{
		if ( bridgeEntity.IsValid() ) {
			bridgeEntity.Delete();
		}
		base.OnDestroy();
	}

	public static void CreateFromTool( Player owner, TraceResult tr )
	{
		var ent = new LightBridgeEntity {
			Position = tr.EndPos,
			Rotation = Rotation.LookAt( tr.Normal, owner.EyeRot.Forward ) * Rotation.From( new Angles( 90, 0, 0 ) ),
		};

		if ( tr.Body.IsValid() && !tr.Entity.IsWorld ) {
			ent.SetParent( tr.Entity, tr.Body.PhysicsGroup.GetBodyBoneName( tr.Body ) );
		}

		ent.SetModel( "models/wirebox/katlatze/lightbridge.vmdl" );

		Sandbox.Hooks.Entities.TriggerOnSpawned( ent, owner );
	}
	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/wirebox/katlatze/lightbridge.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic, false );
	}
}

