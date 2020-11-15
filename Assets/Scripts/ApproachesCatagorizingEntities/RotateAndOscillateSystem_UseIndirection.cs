using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


#if false

// Using indirection to categorize Entity, a bit like in OOP
// System using indirection instead of defining an extra Component will be slower,
// but it avoids making other system queries' chunk occupancy lower.
// This approach works even if the categorization has associated data.

// If both the bitfield method and indirection method is possible,
// the bitfield approach is better if a lot of Entity we iterate over actually has the flag enabled;
// in contrast, this approach is better if there are fewer Entities we iterate over.

public struct Oscillate : IBufferElementData
{
    public Entity Target;
}

[ConverterVersion("test", 1)]
[UpdateInGroup(typeof(GameObjectAfterConversionGroup))]
public class OscillateConversionSystem : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        var oscillateManager = DstEntityManager.CreateEntity(ComponentType.ReadWrite<Oscillate>());
        var oscillateBuffer = DstEntityManager.GetBuffer<Oscillate>(oscillateManager);

        Entities.ForEach((Oscillate_Authoring oscillate) =>
        {
            var entity = GetPrimaryEntity(oscillate);
            oscillateBuffer.Add(new Oscillate { Target = entity });
        });
    }
}

public class OscillateSystem : SystemBase
{
    NativeList<Oscillate> oscillateBufferCache;

    [BurstCompile]
    struct OscillateJob : IJobParallelFor
    {
        public float time;

        [ReadOnly]
        public NativeArray<Oscillate> oscillateBuffer;

        [NativeDisableParallelForRestriction]
        public ComponentDataFromEntity<Translation> translationLookup;

        public void Execute(int index)
        {
            var oscillateEntity = oscillateBuffer[index].Target;
            var translationComp = translationLookup[oscillateEntity];

            translationLookup[oscillateEntity] = new Translation { Value = new float3(translationComp.Value.x, math.sin(time), translationComp.Value.z) };
        }
    }

    protected override void OnCreate()
    {
        oscillateBufferCache = new NativeList<Oscillate>(Allocator.Persistent);        
    }

    protected override void OnDestroy()
    {
        oscillateBufferCache.Dispose();
    }

    protected override void OnUpdate()
    {
        var oscillateManager = GetSingletonEntity<Oscillate>();
        var oscillateBuffer = GetBuffer<Oscillate>(oscillateManager);

        oscillateBufferCache.Clear();
        oscillateBufferCache.CopyFrom(oscillateBuffer.AsNativeArray());

        var translationLookup = GetComponentDataFromEntity<Translation>(false);

        var job = new OscillateJob
        {
            time = (float)Time.ElapsedTime,

            oscillateBuffer = oscillateBufferCache,

            translationLookup = translationLookup
        };

        var innerLoopBatchCount = (int)math.floor(math.sqrt((float)oscillateBufferCache.Length));
        Dependency = job.Schedule(oscillateBufferCache.Length, innerLoopBatchCount, Dependency);
    }
}

public struct Rotator : IBufferElementData
{
    public Entity Target;
    public float Speed;
}

[ConverterVersion("test", 1)]
[UpdateInGroup(typeof(GameObjectAfterConversionGroup))]
public class RotatorConversionSystem : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        var rotatorManager = DstEntityManager.CreateEntity(ComponentType.ReadWrite<Rotator>());
        var rotatorBuffer = DstEntityManager.GetBuffer<Rotator>(rotatorManager);

        Entities.ForEach((Rotator_Authoring rotator) =>
        {
            var entity = GetPrimaryEntity(rotator);
            rotatorBuffer.Add(new Rotator { Target = entity, Speed = rotator.Speed });
        });
    }
}

public class RotationSystem : SystemBase
{
    NativeList<Rotator> rotatorBufferCache;

    [BurstCompile]
    struct RotationJob : IJobParallelFor
    {
        public float deltaTime;

        [ReadOnly]
        public NativeArray<Rotator> rotatorBuffer;

        [NativeDisableParallelForRestriction]
        public ComponentDataFromEntity<Rotation> rotationLookup;

        public void Execute(int index)
        {
            var rotatorData = rotatorBuffer[index];
            var rotatorEntity = rotatorData.Target;
            var rotatorSpeed = rotatorData.Speed;
            var rotationComp = rotationLookup[rotatorEntity];

            rotationLookup[rotatorEntity] = new Rotation { Value = math.mul(math.normalize(rotationComp.Value), quaternion.AxisAngle(math.up(), rotatorSpeed * deltaTime)) };
        }
    }

    protected override void OnCreate()
    {
        rotatorBufferCache = new NativeList<Rotator>(Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        rotatorBufferCache.Dispose();
    }

    protected override void OnUpdate()
    {
        var rotatorManager = GetSingletonEntity<Rotator>();
        var rotatorBuffer = GetBuffer<Rotator>(rotatorManager);

        rotatorBufferCache.Clear();
        rotatorBufferCache.CopyFrom(rotatorBuffer.AsNativeArray());

        var rotationLookup = GetComponentDataFromEntity<Rotation>(false);

        var job = new RotationJob
        {
            deltaTime = Time.DeltaTime,

            rotatorBuffer = rotatorBufferCache,

            rotationLookup = rotationLookup
        };

        var innerLoopBatchCount = (int)math.floor(math.sqrt((float)rotatorBufferCache.Length));
        Dependency = job.Schedule(rotatorBufferCache.Length, innerLoopBatchCount, Dependency);
    }
}

#endif