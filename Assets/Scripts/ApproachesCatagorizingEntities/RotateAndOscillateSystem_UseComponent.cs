using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

#if true

public struct Oscillate : IComponentData
{
}

[ConverterVersion("test", 1)]
[UpdateInGroup(typeof(GameObjectAfterConversionGroup))]
public class OscillateConversionSystem : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((Oscillate_Authoring oscillate) =>
        {
            var entity = GetPrimaryEntity(oscillate);
            DstEntityManager.AddComponent<Oscillate>(entity);
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

        public ComponentTypeHandle<Translation> translationTypeHandle;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var translationChunk = chunk.GetNativeArray(translationTypeHandle);

            for (var i = 0; i < translationChunk.Length; i++)
            {
                var translationComp = translationChunk[i];

                translationChunk[i] = new Translation { Value = new float3(translationComp.Value.x, math.sin(time), translationComp.Value.z) };
            }
        }
    }

    protected override void OnCreate()
    {
        m_OscillateGroup = GetEntityQuery(ComponentType.ReadOnly<Oscillate>(), ComponentType.ReadWrite<Translation>());
    }

    protected override void OnUpdate()
    {
        var translationTypeHandle = GetComponentTypeHandle<Translation>();

        var job = new OscillateJob
        {
            time = (float)Time.ElapsedTime,
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