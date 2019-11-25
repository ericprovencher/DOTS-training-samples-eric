﻿using System;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Systems {
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(UpdateApproachSpeedSystem))]
    public class UpdateCoordinatesSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return Entities.ForEach((ref CoordinateSystemComponent coords, in LocalIntersectionComponent localIntersection, in CarSpeedComponent speed, in InIntersectionComponent inIntersection, in OnSplineComponent spline, in SplineDataComponent splineData) =>
            {
                float2 extrudePoint;
                if (!inIntersection.Value)
                {
                    extrudePoint = new float2(spline.Side, spline.Side);
                    extrudePoint.x *= spline.Direction;
                }
                else
                {
                    extrudePoint = new float2(localIntersection.Side, localIntersection.Side);
                }

                extrudePoint.x *= -RoadGeneratorDots.trackRadius * .5f;
                extrudePoint.y *= RoadGeneratorDots.trackThickness * .5f;

                float t = math.clamp(speed.SplineTimer, 0, 1);
                if (!inIntersection.Value && spline.Direction == -1)
                    t = 1f - t;
                
                // find our position and orientation
                float3 splinePoint = TrackUtils.Extrude(splineData.Bezier, splineData.Geometry, splineData.TwistMode, extrudePoint, t, out _, out var up, out _);
                up = math.normalize(up) * spline.Side;
                coords.Up = up;
                float3 lastPosition = coords.Position;
                coords.Position = splinePoint + up * .06f;
                var delta = coords.Position - lastPosition;
                var sqLength = math.lengthsq(delta);
                if (sqLength > 0)
                    coords.Forward = delta / math.sqrt(sqLength);
            }).WithoutBurst().Schedule(inputDeps);
        }
    }
}