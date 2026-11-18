// ------------------------------------------------------------------------
//  Copyright (c) 2026, Jiaqi (0x7c13) Liu. All rights reserved.
//  See LICENSE file in the project root for license information.
// ------------------------------------------------------------------------

namespace Pal3.Game.Effect.Runtime
{
    using UnityEngine;

    public interface IEffectActor
    {
        Vector3 Position
        {
            get; set;
        }

        float FacingDegrees
        {
            get; set;
        }

        float Blend
        {
            get; set;
        }

        bool ActionOver
        {
            get;
        }

        void SetExternalTask(bool controlled, object owner);

        void SetAction(string action);

        void SetLongBlade(bool enabled);

        void SetWeapons(float left, float right);
    }

    public interface IEffectCamera
    {
        void StopList();

        bool StartList(int index);

        void SetQuake(float amplitude, float frequency, float decay);
    }

    public interface IEffectMovie : System.IDisposable
    {
        bool Finished
        {
            get;
        }
    }

    public interface IEffectTimeControl
    {
        void SetTimeScale(object owner, float scale);

        void ClearTimeScale(object owner);
    }

    // A scene viewer has no combat role pool, camera controller or modal movie
    // loop. These capabilities must be supplied explicitly by the battle owner.
    public interface IBattleEffectContext
    {
        // GT util::Rand belongs to the effect manager. It is independent of
        // CUtil::Rand used by combat rules and of the engine particle streams.
        int NextEffectRandom();

        IEffectActor Actor(int nativeIndex, uint modelCrc = 0);

        IEffectCamera Camera
        {
            get;
        }

        bool IsModalPresentationRunning
        {
            get;
        }

        IEffectMovie PlayMovie(string originalPath);
    }
}
