using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


#if false

// Use a universal bitfield to categorize Entity.
// System using these flags instead of defining an extra Component will be slower,
// but it avoids making other system queries' chunk occupancy lower.
// Practically, this approach only works if the categorization has no associated data.

// If we can have a component just one bit large, it would be more cache efficient than the current version.
// If enabling / disabling component in DOTS is available, this pattern may be better implemented on top of that.

public struct TagsBitField : IComponentData
{
    byte Flags;

    public enum Tag
    {
        Oscillate = 1,
    }

    public void AddTag(Tag tagToAdd)
    {
        Flags |= (byte)tagToAdd;
    }

    public bool HasTag(Tag tagToTest)
    {
        return ((byte)tagToTest & Flags) != 0;
    }
}

[ConverterVersion("test", 1)]
[UpdateInGroup(typeof(GameObjectAfterConversionGroup))]
public class TagsBitFieldConversionSystem : GameObjectConversionSystem
{
    // Adds this to every entity since it is a universal system
    protected override void OnUpdate()
    {
        Entities.ForEach((Transform gameObject) =>
        {
            var entity = GetPrimaryEntity(gameObject);
            DstEntityManager.AddComponent<TagsBitField>(entity);
        });
    }
}

[ConverterVersion("test", 1)]
[UpdateInGroup(typeof(GameObjectAfterConversionGroup))]
[UpdateAfter(typeof(TagsBitFieldConversionSystem))]
public class OscillateConversionSystem : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Oscillate_Authoring oscillate) =>
        {
            var entity = GetPrimaryEntity(oscillate);
            var bitField = DstEntityManager.GetComponentData<TagsBitField>(entity);
            bitField.AddTag(TagsBitField.Tag.Oscillate);
            DstEntityManager.SetComponentData(entity, bitField);
        });
    }
}

public class OscillateSystem : SystemBase
{
    EntityQuery m_OscillateGroup;

    [BurstCompile]
    struct OscillateJob : IJobChunk
    {
        public float time;

        [ReadOnly]
        public ComponentTypeHandle<TagsBitField> tagsBitFieldTypeHandle;

        public ComponentTypeHandle<Translation> translationTypeHandle;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var tagsBitFieldChunk = chunk.GetNativeArray(tagsBitFieldTypeHandle);
            var translationChunk = chunk.GetNativeArray(translationTypeHandle);

            for (var i = 0; i < translationChunk.Length; i++)
            {
                var tagsBitFieldComp = tagsBitFieldChunk[i];
                if (tagsBitFieldComp.HasTag(TagsBitField.Tag.Oscillate))
                {
                    var translationComp = translationChunk[i];
                    translationChunk[i] = new Translation { Value = new float3(translationComp.Value.x, math.sin(time), translationComp.Value.z) };
                }
            }
        }
    }

    protected override void OnCreate()
    {
        m_OscillateGroup = GetEntityQuery(ComponentType.ReadOnly<TagsBitField>(), ComponentType.ReadWrite<Translation>());
    }

    protected override void OnUpdate()
    {
        var tagsBitFieldTypeHandle = GetComponentTypeHandle<TagsBitField>();
        var translationTypeHandle = GetComponentTypeHandle<Translation>();

        var job = new OscillateJob
        {
            time = (float)Time.ElapsedTime,
            tagsBitFieldTypeHandle = tagsBitFieldTypeHandle,
            translationTypeHandle = translationTypeHandle,
        };

        Dependency = job.ScheduleSingle(m_OscillateGroup, Dependency);
    }
}

public struct Rotator : IComponentData
{
    public float Speed;
}

[ConverterVersion("test", 1)]
[UpdateInGroup(typeof(GameObjectAfterConversionGroup))]
public class RotatorConversionSystem : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Rotator_Authoring rotator) =>
        {
            var entity = GetPrimaryEntity(rotator);
            DstEntityManager.AddComponentData(entity, new Rotator { Speed = rotator.Speed });
        });
    }
}

public class RotationSystem : SystemBase
{
    EntityQuery m_RotationGroup;

    [BurstCompile]
    struct RotationJob : IJobChunk
    {
        public float deltaTime;

        [ReadOnly]
        public ComponentTypeHandle<Rotator> rotatorTypeHandle;

        public ComponentTypeHandle<Rotation> rotationTypeHandle;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var rotatorChunk = chunk.GetNativeArray(rotatorTypeHandle);
            var rotationChunk = chunk.GetNativeArray(rotationTypeHandle);

            for (var i = 0; i < rotationChunk.Length; i++)
            {
                var rotatorComp = rotatorChunk[i];
                var rotationComp = rotationChunk[i];

                rotationChunk[i] = new Rotation { Value = math.mul(math.normalize(rotationComp.Value), quaternion.AxisAngle(math.up(), rotatorComp.Speed * deltaTime)) };
            }
        }
    }

    protected override void OnCreate()
    {
        m_RotationGroup = GetEntityQuery(ComponentType.ReadOnly<Rotator>(), ComponentType.ReadWrite<Rotation>());
    }

    protected override void OnUpdate()
    {
        var rotatorTypeHandle = GetComponentTypeHandle<Rotator>();
        var rotationTypeHandle = GetComponentTypeHandle<Rotation>();

        var job = new RotationJob
        {
            deltaTime = Time.DeltaTime,
            rotatorTypeHandle = rotatorTypeHandle,
            rotationTypeHandle = rotationTypeHandle,
        };

        Dependency = job.ScheduleSingle(m_RotationGroup, Dependency);
    }
}

#endif